using System;
using System.Reflection;
using System.Linq;
using UnityEngine;

public static class InternalComponentHelper {
    
    /// <summary>
    /// Gets all components of a specific type by name, even if they're internal
    /// </summary>
    public static Component[] GetComponentsByTypeName(GameObject root, string typeName, bool includeInactive = true) {
        var allComponents = root.GetComponentsInChildren<Component>(includeInactive);
        return allComponents.Where(c => c != null && c.GetType().Name == typeName).ToArray();
    }
    
    /// <summary>
    /// Gets a field value from a component using reflection
    /// </summary>
    public static T GetFieldValue<T>(Component component, string fieldName) {
        var type = component.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field == null) {
            Debug.LogWarning($"Field '{fieldName}' not found on type {type.Name}");
            return default(T);
        }
        
        return (T)field.GetValue(component);
    }
    
    /// <summary>
    /// Gets a property value from a component using reflection
    /// </summary>
    public static T GetPropertyValue<T>(Component component, string propertyName) {
        var type = component.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (property == null) {
            Debug.LogWarning($"Property '{propertyName}' not found on type {type.Name}");
            return default(T);
        }
        
        return (T)property.GetValue(component);
    }
    
    /// <summary>
    /// Sets a field value on a component using reflection
    /// </summary>
    public static void SetFieldValue<T>(Component component, string fieldName, T value) {
        var type = component.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field != null) {
            field.SetValue(component, value);
        }
    }
}
