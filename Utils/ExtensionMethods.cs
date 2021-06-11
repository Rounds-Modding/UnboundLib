using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnboundLib
{
    public static class ExtensionMethods
    {
        #region GameObject

        public static T GetOrAddComponent<T>(this GameObject go, bool searchChildren = false) where T : Component
        {
            var component = searchChildren == true ? go.GetComponentInChildren<T>() : go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        #endregion

        #region MonoBehaviour

        public static void ExecuteAfterFrames(this MonoBehaviour mb, int delay, Action action)
        {
            mb.StartCoroutine(ExecuteAfterFramesCoroutine(delay, action));
        }
        public static void ExecuteAfterSeconds(this MonoBehaviour mb, float delay, Action action)
        {
            mb.StartCoroutine(ExecuteAfterSecondsCoroutine(delay, action));
        }

        private static IEnumerator ExecuteAfterFramesCoroutine(int delay, Action action)
        {
            for (int i = 0; i < delay; i++)
                yield return null;

            action();
        }
        private static IEnumerator ExecuteAfterSecondsCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        #endregion

        #region Image

        public static void SetAlpha(this Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        #endregion

        #region Bool

        public static int AsMultiplier(this bool value)
        {
            return value == true ? 1 : -1;
        }

        #endregion

        #region Array/List

        public static T GetRandom<T>(this IList array)
        {
            return (T)array[Random.Range(0, array.Count)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #endregion

        #region Dictionary

        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            V result;
            dictionary.TryGetValue(key, out result);
            return result;
        }

        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue)
        {
            V result;
            if (dictionary.TryGetValue(key, out result) == false)
            {
                result = defaultValue;
            }
            return result;
        }

        #endregion

        #region Transform

        /// <summary>
        /// Recursively search for children with a given name.
        /// WARNING: Is this really the best way to do what you want?
        /// </summary>
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }

        // Increment the x, y, or z position of a transform
        public static void AddXPosition(this Transform transform, float x)
        {
            Vector3 position = transform.position;
            position.x += x;
            transform.position = position;
        }
        public static void AddYPosition(this Transform transform, float y)
        {
            Vector3 position = transform.position;
            position.y += y;
            transform.position = position;
        }
        public static void AddZPosition(this Transform transform, float z)
        {
            Vector3 position = transform.position;
            position.z += z;
            transform.position = position;
        }

        // Transform
        // Set the x, y, or z position of a transform
        public static void SetXPosition(this Transform transform, float x)
        {
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }
        public static void SetYPosition(this Transform transform, float y)
        {
            Vector3 position = transform.position;
            position.y = y;
            transform.position = position;
        }
        public static void SetZPosition(this Transform transform, float z)
        {
            Vector3 position = transform.position;
            position.z = z;
            transform.position = position;
        }

        #endregion

        #region LayerMask

        public static bool IsLayerInMask(this LayerMask layerMask, int layer)
        {
            return layerMask.value == (layerMask.value | 1 << layer);
        }

        #endregion

        #region Reflection

        // methods
        public static MethodInfo GetMethodInfo(Type type, string methodName)
        {
            MethodInfo methodInfo = null;
            do
            {
                methodInfo = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }
            while (methodInfo == null && type != null);
            return methodInfo;
        }
        public static object InvokeMethod(this object obj, string methodName, params object[] arguments)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            MethodInfo propInfo = GetMethodInfo(objType, methodName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", methodName, objType.FullName));
            return propInfo.Invoke(obj, arguments);
        }
        public static MethodInfo GetMethodInfo(Type type, string methodName, Type[] parameters)
        {
            MethodInfo methodInfo = null;
            do
            {
                methodInfo = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
                    null,
                    parameters,
                    null);
                type = type.BaseType;
            }
            while (methodInfo == null && type != null);
            return methodInfo;
        }
        public static object InvokeMethod(this object obj, string methodName, Type[] argumentOrder, params object[] arguments)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            MethodInfo propInfo = GetMethodInfo(objType, methodName, argumentOrder);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", methodName, objType.FullName));
            return propInfo.Invoke(obj, arguments);
        }

        // fields
        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            FieldInfo fieldInfo = null;
            do
            {
                fieldInfo = type.GetField(fieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);
            return fieldInfo;
        }
        public static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            FieldInfo propInfo = GetFieldInfo(objType, fieldName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", fieldName, objType.FullName));
            return propInfo.GetValue(obj);
        }
        public static void SetFieldValue(this object obj, string fieldName, object val)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            FieldInfo propInfo = GetFieldInfo(objType, fieldName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", fieldName, objType.FullName));
            propInfo.SetValue(obj, val);
        }

        // properties
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propInfo = null;
            do
            {
                propInfo = type.GetProperty(propertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (propInfo == null && type != null);
            return propInfo;
        }
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            return propInfo.GetValue(obj, null);
        }
        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type objType = obj.GetType();
            PropertyInfo propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            propInfo.SetValue(obj, val, null);
        }

        // casting
        public static object Cast(this Type Type, object data)
        {
            var DataParam = Expression.Parameter(typeof(object), "data");
            var Body = Expression.Block(Expression.Convert(Expression.Convert(DataParam, data.GetType()), Type));

            var Run = Expression.Lambda(Body, DataParam).Compile();
            var ret = Run.DynamicInvoke(data);
            return ret;
        }

        #endregion
    }
}