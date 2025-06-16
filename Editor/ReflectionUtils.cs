using System;
using System.Collections.Generic;
using System.Reflection;

namespace nz.alle.SimpleHierarchy
{
    /// <summary>
    /// 反射工具类
    /// </summary>
    internal static class ReflectionUtils
    {
        private const BindingFlags BindingFlagsAll = BindingFlags.Instance
                                                     | BindingFlags.Static
                                                     | BindingFlags.Public
                                                     | BindingFlags.NonPublic;

        /// <summary>
        /// 判断一个值是否是该类型的默认值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">值</param>
        /// <returns>是否是默认值</returns>
        public static bool IsDefaultValue<T>(T value)
        {
            if (typeof(T) == typeof(string))
            {
                return string.IsNullOrEmpty(value as string);
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return comparer.Equals(value, default);
        }

        /// <summary>
        /// 判断一个类型是否继承自一个泛型类型
        /// </summary>
        /// <param name="current">当前类型</param>
        /// <param name="genericBase">泛型类型</param>
        /// <returns>是否继承自该泛型类型</returns>
        public static bool IsSubTypeOfGeneric(this Type current, Type genericBase)
        {
            do
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
                {
                    return true;
                }
                // ReSharper disable once AssignNullToNotNullAttribute
            } while ((current = current.BaseType) != null);

            return false;
        }

        /// <summary>
        /// 判断一个类型是否继承自另一个类型
        /// </summary>
        /// <param name="type">子类型</param>
        /// <param name="baseType">父类型</param>
        /// <returns>是否继承自该类型</returns>
        public static bool IsSubType(this Type type, Type baseType)
        {
            if (baseType.IsGenericType)
            {
                return IsSubTypeOfGeneric(type, baseType);
            }
            else
            {
                return baseType.IsAssignableFrom(type);
            }
        }

        /// <summary>
        /// 判断类型是否具有特定属性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherit">是否搜索继承链</param>
        /// <returns>是否具有该属性</returns>
        public static bool HasAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return HasAttribute(type, typeof(T), inherit);
        }

        /// <summary>
        /// 判断类型是否具有特定属性
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="attributeType">属性类型</param>
        /// <param name="inherit">是否搜索继承链</param>
        /// <returns>是否具有该属性</returns>
        public static bool HasAttribute(this Type type, Type attributeType, bool inherit = false)
        {
            return type.GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        /// <summary>
        /// 获取类型上的特定属性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherit">是否搜索继承链</param>
        /// <returns>属性实例</returns>
        public static T GetAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttribute(typeof(T), inherit) as T;
        }

        /// <summary>
        /// 获取类型上的所有特定属性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherit">是否搜索继承链</param>
        /// <returns>属性实例数组</returns>
        public static T[] GetAttributes<T>(Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), inherit) as T[];
        }

        /// <summary>
        /// 获取泛型类型的参数类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="index">参数索引</param>
        /// <returns>参数类型</returns>
        public static Type GetGenericArgumentType(Type type, int index)
        {
            Type t = type;
            while (t != null)
            {
                if (t.IsGenericType)
                {
                    Type[] arguments = t.GetGenericArguments();
                    return index <= arguments.Length - 1 ? arguments[index] : null;
                }

                // ReSharper disable once AssignNullToNotNullAttribute
                t = t.BaseType;
            }
            return null;
        }

        /// <summary>
        /// 获取类型的所有字段
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>字段信息数组</returns>
        public static FieldInfo[] GetAllFields(Type type)
        {
            return type.GetFields(BindingFlagsAll);
        }

        /// <summary>
        /// 判断类型是否具有特定字段
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>是否具有该字段</returns>
        public static bool HasField(Type type, string fieldName)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlagsAll);
            return fieldInfo != null;
        }

        /// <summary>
        /// 获取对象字段的值
        /// </summary>
        /// <typeparam name="TValue">字段值类型</typeparam>
        /// <typeparam name="TType">对象类型</typeparam>
        /// <param name="obj">对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段值</returns>
        public static TValue GetFieldValue<TValue, TType>(TType obj, string fieldName)
        {
            return GetFieldValue<TValue>(typeof(TType), obj, fieldName);
        }

        /// <summary>
        /// 获取对象字段的值
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="obj">对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段值</returns>
        public static object GetFieldValue(Type type, object obj, string fieldName)
        {
            return GetFieldValue<object>(type, obj, fieldName);
        }

        /// <summary>
        /// 获取静态字段的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段值</returns>
        public static object GetStaticFieldValue(Type type, string fieldName)
        {
            return GetFieldValue<object>(type, null, fieldName);
        }

        /// <summary>
        /// 获取静态字段的值
        /// </summary>
        /// <typeparam name="TValue">字段值类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段值</returns>
        public static TValue GetStaticFieldValue<TValue>(Type type, string fieldName)
        {
            return GetFieldValue<TValue>(type, null, fieldName);
        }

        /// <summary>
        /// 获取对象字段的值
        /// </summary>
        /// <typeparam name="TValue">字段值类型</typeparam>
        /// <param name="type">对象类型</param>
        /// <param name="obj">对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段值</returns>
        public static TValue GetFieldValue<TValue>(Type type, object obj, string fieldName)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlagsAll)!;
            return (TValue)fieldInfo.GetValue(obj);
        }

        /// <summary>
        /// 设置对象字段的值
        /// </summary>
        /// <typeparam name="TValue">字段值类型</typeparam>
        /// <param name="type">对象类型</param>
        /// <param name="obj">对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="value">字段值</param>
        public static void SetFieldValue<TValue>(Type type, object obj, string fieldName, TValue value)
        {
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlagsAll)!;
            fieldInfo.SetValue(obj, value);
        }
        
        /// <summary>
        /// 获取属性的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性值</returns>
        public static object GetPropertyValue(Type type, object obj, string propertyName)
        {
            return GetPropertyValue<object>(type, obj, propertyName); 
        }

        /// <summary>
        /// 获取属性的值
        /// </summary>
        /// <typeparam name="TValue">属性值类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性值</returns>
        public static TValue GetPropertyValue<TValue>(Type type, object obj, string propertyName)
        {
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlagsAll)!;
            return (TValue)propertyInfo.GetValue(obj);
        }

        /// <summary>
        /// 设置属性的值
        /// </summary>
        /// <typeparam name="TValue">属性值类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="value">属性值</param>
        public static void SetPropertyValue<TValue>(Type type,
                                                    object obj,
                                                    string propertyName,
                                                    TValue value)
        {
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlagsAll)!;
            propertyInfo.SetValue(obj, value);
        }

        /// <summary>
        /// 调用方法
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="methodName">方法名称</param>
        public static void InvokeMethod(Type type, object obj, string methodName)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            MethodInfo methodInfo = type.GetMethod(
                methodName,
                BindingFlagsAll,
                null,
                Array.Empty<Type>(),
                null
            );
            // ReSharper disable once PossibleNullReferenceException
            methodInfo.Invoke(obj, Array.Empty<object>());
        }

        /// <summary>
        /// 调用方法
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="signatures">方法签名</param>
        /// <param name="parameters">参数</param>
        public static void InvokeMethod(Type type,
                                        object obj,
                                        string methodName,
                                        Type[] signatures,
                                        object[] parameters)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlagsAll, null, signatures, null);
            // ReSharper disable once PossibleNullReferenceException
            methodInfo.Invoke(obj, parameters);
        }

        /// <summary>
        /// 调用方法并返回结果
        /// </summary>
        /// <typeparam name="TReturn">返回值类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="methodName">方法名称</param>
        /// <returns>方法返回值</returns>
        public static TReturn InvokeMethod<TReturn>(Type type, object obj, string methodName)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            MethodInfo methodInfo = type.GetMethod(
                methodName,
                BindingFlagsAll,
                null,
                Array.Empty<Type>(),
                null
            );
            // ReSharper disable once PossibleNullReferenceException
            return (TReturn)methodInfo.Invoke(obj, Array.Empty<object>());
        }

        /// <summary>
        /// 调用方法并返回结果
        /// </summary>
        /// <typeparam name="TReturn">返回值类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="obj">对象</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="signatures">方法签名</param>
        /// <param name="parameters">参数</param>
        /// <returns>方法返回值</returns>
        public static TReturn InvokeMethod<TReturn>(Type type,
                                                    object obj,
                                                    string methodName,
                                                    Type[] signatures,
                                                    object[] parameters)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlagsAll, null, signatures, null);
            // ReSharper disable once PossibleNullReferenceException
            return (TReturn)methodInfo.Invoke(obj, parameters);
        }

        /// <summary>
        /// 在类型的继承层次结构中查找直接子类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="baseType">基类型</param>
        /// <returns>基类型的直接子类型</returns>
        public static Type FindBaseTypeDirectSubTypeInHierarchy(Type type, Type baseType)
        {
            Type t = type;
            while (t != null)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == baseType)
                {
                    return t;
                }
                else if (t.BaseType == baseType)
                {
                    return t;
                }

                // ReSharper disable once AssignNullToNotNullAttribute
                t = t.BaseType;
            }

            return null;
        }
    }
}