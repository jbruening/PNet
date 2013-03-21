using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Yaml;
using System.Yaml.Serialization;

namespace PNetS
{
    public static class Resources
    {
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
        /// <returns></returns>
        public static GameObject Load(string filePath, Room roomToInstantiateIn)
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

            var componentType = typeof(Component);
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in a.GetTypes())
                {
                    if (t.IsSubclassOf(componentType))
                    {
                        Type t1 = t;
                        if (t1 == typeof(NetworkView))
                        {
                            Debug.LogWarning("[Resources.Load] file {0} has a NetworkView component on it. This will not make it networked. Use Room.NetworkLoad", actualFilePath);
                        }
                        GameObject dser1 = dser;
                        config.AddActivator(t, () =>
                        {
                            Action awake;
                            var ntrack = trackers.Pop();
                            var ret = dser1.DeserializeAddComponent(t1, out awake, ntrack);
                            awakes.Add(awake);
                            return ret;
                        });
                    }
                }
            }

            var serializer = new YamlSerializer(config);
            serializer.DeserializeFromFile(actualFilePath, typeof(GameObject));

            if (dser.Resource == null)
                dser.Resource = filePath;

            foreach (var awake in awakes)
                if (awake != null) awake();
            
            dser.OnComponentAfterDeserialization();

            return dser;
        }
    }
}
