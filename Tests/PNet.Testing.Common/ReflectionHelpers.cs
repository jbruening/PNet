using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PNet.Testing.Common
{
    /// <summary>
    /// Methods for accessing private properties and methods of class instances under test.
    /// </summary>
    public static class ReflectionHelper
    {
        private const BindingFlags AllBindings =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        private const BindingFlags StaticBindings =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        #region Method operations
        /// <summary>
        /// Get method information.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo<TClass>(string methodName)
        {
            MethodInfo result = typeof(TClass).GetMethod(methodName, AllBindings);
            return result;
        }

        /// <summary>
        /// Call method with specified parameters
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object InvokeMethod<TClass>(this TClass instance, string methodName, params object[] parameters) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            return instance.GetType().GetMethod(methodName, AllBindings).Invoke(instance, parameters);
        }

        /// <summary>
        /// Call a static method with specified parameters
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object InvokeMethod<TClass>(string methodName, params object[] parameters)
        {
            return typeof(TClass).GetMethod(methodName, StaticBindings).Invoke(null, parameters);
        }
        #endregion

        #region Field operations
        /// <summary>
        /// Get field information.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FieldInfo GetFieldInfo<TClass>(string name)
        {
            FieldInfo fieldInfo = typeof(TClass).GetField(name, AllBindings);
            return fieldInfo;
        }

        /// <summary>
        /// Get field value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object GetFieldValue<TClass>(this TClass instance, string fieldName) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            return GetFieldInfo<TClass>(fieldName).GetValue(instance);
        }

        /// <summary>
        /// Set field value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public static void SetFieldValue<TClass>(this TClass instance, string fieldName, object value) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            instance.GetType().GetField(fieldName, AllBindings).SetValue(instance, value);
        }

        public static void SetFieldValue<TClass>(string fieldName, object value)
        {
            typeof(TClass).GetField(fieldName, StaticBindings).SetValue(null, value);
        }
        #endregion

        #region Property operations
        /// <summary>
        /// Get property information.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfo<TClass>(string name)
        {
            PropertyInfo result = typeof(TClass).GetProperty(name, AllBindings);
            return result;
        }

        /// <summary>
        /// Get property value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetPropertyValue<TClass>(this TClass instance, string propertyName) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            return GetPropertyInfo<TClass>(propertyName).GetValue(instance, null);
        }

        /// <summary>
        /// Get property value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="propertyName"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static object GetPropertyValue<TClass>(this TClass instance, string propertyName, object[] index) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            return GetPropertyInfo<TClass>(propertyName).GetValue(instance, index);
        }

        /// <summary>
        /// Set property value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void SetPropertyValue<TClass>(this TClass instance, string propertyName, object value) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            instance.GetType().GetProperty(propertyName, AllBindings).SetValue(instance, value, null);
        }

        /// <summary>
        /// Set property value.
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="instance"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        public static void SetPropertyValue<TClass>(this TClass instance, string propertyName, object value, object[] index) where TClass : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            instance.GetType().GetProperty(propertyName, AllBindings).SetValue(instance, value, index);
        }
        #endregion
    }
}
