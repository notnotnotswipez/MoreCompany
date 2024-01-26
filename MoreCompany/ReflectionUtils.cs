using System;
using System.Reflection;

namespace MoreCompany;

public class ReflectionUtils
{
    public static void InvokeMethod(object obj, string methodName, object[] parameters)
    {
        Type type = obj.GetType();
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(obj, parameters);
    }
        
    public static void InvokeMethod(object obj, Type forceType, string methodName, object[] parameters)
    {
        MethodInfo method = forceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(obj, parameters);
    }
        
    public static void SetPropertyValue(object obj, string propertyName, object value)
    {
        Type type = obj.GetType();
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property.SetValue(obj, value);
    }

    public static T InvokeMethod<T>(object obj, string methodName, object[] parameters)
    {
        Type type = obj.GetType();
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (T) method.Invoke(obj, parameters);
    }

    public static T GetFieldValue<T>(object obj, string fieldName)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (T) field.GetValue(obj);
    }
        
    public static void SetFieldValue(object obj, string fieldName, object value)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        field.SetValue(obj, value);
    }
}