using System.Reflection;
using UnityEngine;

namespace UnityMapper;

/// <summary>
/// An object encapsulating a property of a class that can be mapped to a signal.
/// </summary>
public interface IMappedProperty
{
    /// <summary>
    /// The vector size of this mapped property.
    /// If > 1, T should be an array of a supported primitive;
    /// </summary>
    /// <returns>An integer >= 1</returns>
    int GetVectorLength()
    {
        return 1;
    }

    /// <summary>
    /// Get the type that this property is mapped to.
    /// </summary>
    /// <remarks>
    /// This doesn't have to be the underlying type of the property, but has to be either a float, int,
    /// double, or an array of one of those.
    /// </remarks>
    /// <returns>Either float, double, int, float[], double[], or int[]</returns>
    Type GetMappedType();
    
    /// <summary>
    /// Called by libmapper to update the value of the property.
    /// </summary>
    /// <param name="value">An object, guaranteed to be the same type that is returned by <see cref="GetMappedType"/> </param>
    void SetObject(object value);
    
    /// <summary>
    /// Called by libmapper to get the current value of this property.
    /// </summary>
    /// <returns>An object of the same type returned by <see cref="GetMappedType"/>. If the type is different, an exception will be thrown.</returns>
    object GetValue();

    /// <summary>
    /// Used to name the corresponding signal in the libmapper graph. 
    /// </summary>
    /// <remarks>
    /// The name should not contain spaces, and should be properly namespaced to be unique.
    /// You can use `/` to create a nested heirarchy.
    /// </remarks>
    /// <returns>A human readable name without spaces</returns>
    string GetName();
}

/// <summary>
/// Simple implementation of <see cref="IMappedProperty"/> for fields.
///
/// Uses reflection to get and set values. For more complex properties a custom implementation should be used.
/// </summary>
/// <param name="info">The target field</param>
/// <param name="target">The component the field belongs to</param>
public class MappedClassField(FieldInfo info, Component target) : IMappedProperty
{
    
    // TODO: Add check to ensure info belongs to target
    
    public Type GetMappedType()
    {
        return info.FieldType;
    }

    public void SetObject(object value)
    {
        info.SetValue(target, value);
    }

    public object GetValue()
    {
        return info.GetValue(target);
    }

    public string GetName()
    {
        return info.DeclaringType.Name + "/" + info.Name;
    }
}