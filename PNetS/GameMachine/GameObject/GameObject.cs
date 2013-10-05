using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Yaml;
using System.Yaml.Serialization;
using SlimMath;

namespace PNetS
{
    public sealed partial class GameObject : IDisposable
    {
        /// <summary>
        /// create a new game object
        /// </summary>
        public GameObject()
        {
            Id = -1;
            GameState.InvokeIfRequired(() => Id = GameState.AddGameObject(this));
        }
        /// <summary>
        /// Unique identifier
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public int Id { get; private set; }

        [YamlSerialize(YamlSerializeMethod.Assign)]
        private string _resource;

        /// <summary>
        /// Name of this gameobject. Not necessarily unique, just an identifier
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// resource path this gameobject came from
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public string Resource
        {
            get { return _resource; }
            internal set { _resource = value; }
        }

        /// <summary>
        /// room this gameobject is in
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public Room Room { get; internal set; }
        /// <summary>
        /// world position
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// world rotation
        /// </summary>
        public Quaternion Rotation { get; set; }
        /// <summary>
        /// localized forward
        /// </summary>
        public Vector3 Forward { get { return  Rotation.Multiply(Vector3.UnitZ); } }

        /// <summary>
        /// localized right
        /// </summary>
        public Vector3 Right { get { return Rotation.Multiply(Vector3.UnitX); } }

        /// <summary>
        /// the owner of this gameobject. set during Network Instantiate. Null if no owner.
        /// </summary>
        public Player Owner { get; internal set; }


        private bool _markedForDestruction;
        /// <summary>
        /// Destroy this gameObject. THIS IS NOT NETWORKED SYNCED.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void Destroy(GameObject gameObject)
        {
            //prevent destroy from being called on a gameobject multiple times enqueing
            if (gameObject._markedForDestruction) return;
            gameObject._markedForDestruction = true;
            GameState.DestroyDelays += () => { DestroyNow(gameObject); };
        }

        internal static void DestroyNow(GameObject gameObject)
        {
            gameObject._markedForDestruction = true; //just in case
            gameObject.Owner = null;
            gameObject.OnDestroy();
            gameObject.components.ForEach(g => g.component.Dispose());
            gameObject.components = null;
            GameState.RemoveObject(gameObject);
            (gameObject as IDisposable).Dispose();
        }

        /// <summary>
        /// serialize this gameobject
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            var serializer = new YamlSerializer();
            return serializer.Serialize(this);
        }

        /// <summary>
        /// tostring
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "PnetS.GameObject";
            return "GameObject " + Name;
        }

        /// <summary>
        /// whether or not IDisposable.Dispose has been run.
        /// </summary>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// Dispose the gameobject
        /// </summary>
        void IDisposable.Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Extensions for slimmath
    /// </summary>
    public static class SlimMathExtensions
    {
        /// <summary>
        /// Rotate the vector by the quaternion
        /// </summary>
        /// <param name="vec3"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        [Obsolete("Multiply(this quaternion, vector3) is faster")]
        public static Vector3 Rotate(this Vector3 vec3, Quaternion rotation)
        {
            Quaternion r = Quaternion.Normalize(rotation);
            Vector3 q = new Vector3(r.X, r.Y, r.Z);
            Vector3 t = 2 * Vector3.Cross(q, vec3);
            return vec3 + r.W * t + Vector3.Cross(q, t);
        }

        /// <summary>
        /// Rotate the vector by the quaternion.
        /// </summary>
        /// <remarks>Ogre and Unity use this as well, from the nvidia sdk
        ///  Fuck if I know how this magic works</remarks>
        /// <param name="quaternion"></param>
        /// <param name="vector3"></param>
        /// <returns></returns>
        public static Vector3 Multiply(this Quaternion quaternion, Vector3 vector3)
        {
            Vector3 uv, uuv;
            Vector3 qvec = new Vector3(quaternion.X, quaternion.Y, quaternion.Z);
            Vector3.Cross(ref qvec, ref vector3, out uv);
            Vector3.Cross(ref qvec, ref uv, out uuv);
            uv *= (2.0f * quaternion.W);
            uuv *= 2.0f;

            return vector3 + uv + uuv;

        }

        /// <summary>
        /// Get the 3 bytes from the Color3
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] ColorBytes(this Color3 color)
        {
            var intColor = color.ToRgb();

            byte[] bytes = new byte[3];
            unchecked
            {
                bytes[0] = (byte)(intColor >> 16);
                bytes[1] = (byte)(intColor >> 8);
                bytes[2] = (byte)(intColor);
            }

            return bytes;
        }

        /// <summary>
        /// Get the 4 bytes from the Color4
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] ColorBytes(this Color4 color)
        {
            var intColor = color.ToArgb();

            byte[] bytes = new byte[4];
            unchecked
            {
                bytes[0] = (byte)(intColor >> 24);
                bytes[1] = (byte)(intColor >> 16);
                bytes[2] = (byte)(intColor >> 8);
                bytes[3] = (byte)(intColor);
            }

            return bytes;
        }

        /// <summary>
        /// Get a new color3 from the incoming message
        /// </summary>
        /// <param name="color"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Color3 Deserialize(this Color3 color, Lidgren.Network.NetIncomingMessage msg)
        {
            var cr = msg.ReadByte();
            var cg = msg.ReadByte();
            var cb = msg.ReadByte();
            return new Color3(BitConverter.ToInt32(new byte[] { cb, cg, cr, 0 }, 0));
        }
    }
}
