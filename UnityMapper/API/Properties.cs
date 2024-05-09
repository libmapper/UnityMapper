using System.Reflection;
using UnityEngine;

namespace UnityMapper;

/// <summary>
/// An object encapsulating a property of a class that can be exposed as a signal.
/// </summary>
public interface IAccessibleProperty
{
    /// <summary>
    /// The vector size of this property.
    /// If > 1, T should be an array of a supported primitive;
    /// </summary>
    /// <returns>An integer >= 1</returns>
    int GetVectorLength()
    {
        return 1;
    }

    /// <summary>
    /// Get the type that this property is mapped to. This type will be passed to SetObject and returned from GetValue.
    /// </summary>
    /// <remarks>
    /// This doesn't have to be the underlying type of the property, but if it is not a float, int,
    /// double, or an array of one of those a type mapper will be used which may be less efficient.
    /// </remarks>
    Type BackingType { get;}
    
    /// <summary>
    /// Called by libmapper to update the value of the property.
    /// </summary>
    /// <param name="target">The object to set the property on</param>
    /// <param name="value">An object, guaranteed to be the same type that is returned by <see cref="GetMappedType"/> </param>
    void SetObject(object target, object value);
    
    /// <summary>
    /// Called by libmapper to get the current value of this property.
    /// </summary>
    /// <param name="target">Component to get the property from</param>
    /// <returns>An object of the same type returned by <see cref="GetMappedType"/>. If the type is different, an exception will be thrown.</returns>
    object GetValue(object target);

    /// <summary>
    /// Used to name the corresponding signal in the libmapper graph. 
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Names should be unique within a device.</description></item>
    /// <item><description>Names should be human-readable and descriptive.</description></item>
    /// </list>
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// The units that will be displayed in applications like webmapper.
    /// Using null will omit that metadata.
    /// </summary>
    string? Units { get; }
    
    /// <summary>
    /// The numerical bounds of the property. Use null if the property is unbounded.
    /// </summary>
    (float min, float max)? Bounds { get; }
}

public interface IAccessibleProperty<TComponent, TProperty> : IAccessibleProperty where TProperty : notnull
{

    void IAccessibleProperty.SetObject(object target, object value)
    {
        if (!(target is TComponent))
        {
            throw new ArgumentException("Target is not of the correct type", nameof(target));
        }
        if (!(value is TProperty))
        {
            throw new ArgumentException("Value is not of the correct type", nameof(value));
        }
        Set((TComponent) target, (TProperty) value);
    }
    
    object IAccessibleProperty.GetValue(object target)
    {
        if (target is not TComponent)
        {
            throw new ArgumentException("Target is not of the correct type", nameof(target));
        }
        return Get((TComponent) target);
    }
    
    void Set(TComponent target, TProperty value);
    TProperty Get(TComponent target);
    
    Type IAccessibleProperty.BackingType => typeof(TProperty);
}

/// <summary>
/// Simple implementation of <see cref="IAccessibleProperty"/> for fields.
///
/// Uses reflection to get and set values. For more complex properties a custom implementation should be used.
/// </summary>
/// <param name="info">The target field</param>
/// <param name="target">The component the field belongs to</param>
public class AccessibleClassField(FieldInfo info) : IAccessibleProperty
{
    public void SetObject(object target, object value)
    {
        info.SetValue(target, value);
    }

    public object GetValue(object target)
    {
        return info.GetValue(target);
    }
    
    public string Name => info.DeclaringType.Name + "/" + info.Name;
    
    public Type BackingType => info.FieldType;

    public string? Units
    {
        get {
            var attr = info.GetCustomAttribute<SignalUnitAttribute>();
            return attr?.Units;
        }
    }
    
    public (float min, float max)? Bounds
    {
        get
        {
            var attr = info.GetCustomAttribute<SignalBoundsAttribute>();
            if (attr == null)
            {
                return null;
            }
            return (attr.Min, attr.Max);
        }
    }
}

/// <summary>
/// Indicates to UnityMapper the units to be associated with a signal.
/// Only used by the reflection-based property extractor.
/// </summary>
/// <param name="units">Human-readable unit for this signal</param>
public class SignalUnitAttribute(string units) : Attribute
{
    public string Units { get; } = units;
}

/// <summary>
/// Indicates to UnityMapper the bounds to be associated with a signal.
/// Only used by the reflection-based property extractor.
/// </summary>
/// <param name="min">Lower bound</param>
/// <param name="max">Upper bound</param>
public class SignalBoundsAttribute(float min, float max) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
}