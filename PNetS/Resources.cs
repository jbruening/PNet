using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Yaml;
using System.Yaml.Serialization;
using SlimMath;

namespace PNetS
{
    /// <summary>
    /// class for loading prefabs
    /// </summary>
    public static class Resources
    {
        private static List<Type> componentTypes = new List<Type>();
        private static IEnumerable<Type> GetComponentTypes()
        {
            if (componentTypes.Count == 0)
            {
                Type componentType = typeof(Component);
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var assemblyComponentTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(componentType));
                        componentTypes.AddRange(assemblyComponentTypes);
                    }catch(ReflectionTypeLoadException exception)
                    {
                        var sb = new StringBuilder();
                        foreach(var type in exception.LoaderExceptions)
                        {
                            sb.AppendLine(type.ToString());
                        }
                        Debug.LogError("Resources GetComponentTypes failed for the following types: {0}", sb.ToString());
                    }
                }
            }
            return componentTypes;
        }

        /// <summary>
        /// the folder that Resources.Load pulls from. by default, it is a folder next to PNetS.dll called Resources
        /// </summary>
        public static string ResourceFolder;

        static Resources()
        {
            ResourceFolder = Path.Combine(Assembly.GetAssembly(typeof (Resources)).Location, "Resources");
        }

        /// <summary>
        /// Load a gameobject from a file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="roomToInstantiateIn"></param>
        /// <param name="position"> </param>
        /// <param name="rotation"> </param>
        /// <param name="allowNetworkInstantiateIfHasNetworkView"></param>
        /// <param name="visibleToAll">makes all players in the room subscribed to the object</param>
        /// <param name="owner">owner of the loaded object if network instantiated.  By default, it is the server</param>
        /// <returns></returns>
        public static GameObject Load(string filePath, Room roomToInstantiateIn, bool allowNetworkInstantiateIfHasNetworkView = false, Vector3? position = null, Quaternion? rotation = null, bool visibleToAll = true, Player owner = null)
        {
            var dser = new GameObject();
            dser.Room = roomToInstantiateIn;
            var awakes = new List<Action>();
            var config = new YamlConfig();
            var actualFilePath = Path.Combine(ResourceFolder, filePath + ".prefab");

            config.AddActivator<GameObject>(() =>
            {
                //be returning an object we've already created, the AddComponent will work properly
                return dser;
            });

            //config.AddActivator < List<ComponentTracker>>(() =>
            //{
            //    return dser.components;
            //});

            var trackers = new Stack<GameObject.ComponentTracker>();

            config.AddActivator<GameObject.ComponentTracker>(() =>
            {
                var ntrack = new GameObject.ComponentTracker();
                trackers.Push(ntrack);
                return ntrack;
            });

            foreach (Type t in GetComponentTypes())
            {
                Type tLocal = t;
                if (tLocal == typeof(NetworkView) && !allowNetworkInstantiateIfHasNetworkView)
                {
                    Debug.LogWarning("[Resources.Load] file {0} has a NetworkView component on it, but was run as to not network instantiate. It will not be networked.", actualFilePath);
                }
                GameObject dser1 = dser;
                config.AddActivator(tLocal, () =>
                {
                    Action awake;
                    var ntrack = trackers.Pop();
                    var ret = dser1.DeserializeAddComponent(tLocal, out awake, ntrack);
                    awakes.Add(awake);
                    return ret;
                });
            }

            var serializer = new YamlSerializer(config);
            serializer.DeserializeFromFile(actualFilePath, typeof(GameObject));

            if (dser.Resource == null)
                dser.Resource = filePath;

            foreach (var awake in awakes)
                if (awake != null) awake();
            
            dser.OnComponentAfterDeserialization();

            if (position.HasValue)
                dser.Position = position.Value;
            if (rotation.HasValue)
                dser.Rotation = rotation.Value;

            if (allowNetworkInstantiateIfHasNetworkView && dser.GetComponent<NetworkView>() != null)
                roomToInstantiateIn.ResourceNetworkInstantiate(dser, visibleToAll, owner);

            return dser;
        }
    }
}
