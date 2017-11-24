//Author: Sergey Lavrinenko
//Date:   28Nov2010

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RequestEngine
{
    /// <summary>
    /// This class provides several useful functions for runtime type conversions and extraction of class members information
    /// </summary>
    static class TypeUtils
    {
        public static bool IsNullableType(Type type)
        {
            return (type.IsGenericType &&
                type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
        }

        public static bool IsConvertable(string str, Type type)
        {
            if (str == null)
                return type.IsClass || IsNullableType(type);

            object value;
            return ConvertFromString(str, type, out value);
        }

        public static bool ConvertFromString(string str, Type type, out object value)
        {
            value = null;
            try
            {
                MethodInfo mi = type.GetMethod("FromString", BindingFlags.Public | BindingFlags.Static);
                if (mi != null)
                    value = mi.Invoke(null, new object[] { str });
                else if (type == typeof(DateTime))
                    value = DateTime.Parse(str);
                else if (type.BaseType == typeof(Enum))
                    value = Enum.Parse(type, str);
                else if (IsNullableType(type) /*|| type.IsClass*/)
                {
                    if (str == string.Empty || string.Compare(str, "null", true) == 0)
                        value = null;
                    else
                    {
                        ConstructorInfo[] ci = type.GetConstructors();
                        ParameterInfo[] pi = ci[0].GetParameters();
                        object val = Convert.ChangeType(str, pi[0].ParameterType);
                        value = ci[0].Invoke(new object[] { val });
                    }
                }
                else
                    value = Convert.ChangeType(str, type);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            PropertyInfo info = type.GetProperty(propertyName, bf);
            if (info != null) return info;
            
            foreach (Type interfaceType in type.GetInterfaces())
            {
                info = interfaceType.GetProperty(propertyName, bf);
                if (info != null) return info;
            }

            return null;
        }

        public static bool MethodExists(Type type, string methodName)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            MethodInfo[] infos = type.GetMethods(bf);
            foreach (MethodInfo mi in infos)
                if (mi.Name == methodName) return true;
            return false;
        }

        public static MethodInfo GetMethodInfo(Type type, string methodName, int argsCount)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            MethodInfo[] infos = type.GetMethods(bf);
            foreach (MethodInfo mi in infos)
                if (mi.Name == methodName && mi.GetParameters().Length == argsCount) return mi;
            return null;
        }
    }
}
