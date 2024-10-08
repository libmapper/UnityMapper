using System.Reflection;
using UnityEngine;

namespace UnityMapper.API;

/// <summary>
/// An object encapsulating a property of a class that can be bound to a signal.
/// </summary>
public interface IBoundProperty
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
    /// This doesn't have to be the underlying type of the property, but if it is not a float, int,
    /// double, or an array of one of those a type converter will be used which may be less efficient.
    /// </remarks>
    /// <returns>The type that should be passed to SetObject and returned from GetValue</returns>
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
    /// Called by UnityMapper to reset the property to a default state.
    ///
    /// This is called usually when an instance is released.
    /// </summary>
    void Reset()
    {
        
    }

    /// <summary>
    /// Used to name the corresponding signal in the libmapper graph. 
    /// </summary>
    /// <remarks>
    /// The name should not contain spaces, and should be properly namespaced to be unique.
    /// You can use `/` to create a nested heirarchy.
    /// </remarks>
    /// <returns>A human readable name without spaces</returns>
    string GetName();

    /// <summary>
    /// The units that will be displayed in applications like webmapper.
    /// Using null will omit that metadata.
    /// </summary>
    string? Units { get; }
    
    /// <summary>
    /// The numerical bounds of the property. Use null if the property is unbounded.
    /// </summary>
    (float min, float max)? Bounds { get; }
    
    /// <summary>
    /// If this signal is currently "Active", meaning it's value is useful and updating it will cause a visible effect.
    ///
    /// For example, if the backing component in Unity is disabled this should return false.
    /// </summary>
    bool IsActive => true;
}

/// <summary>
/// Simple implementation of <see cref="IBoundProperty"/> for fields.
///
/// Uses reflection to get and set values. For more complex properties a custom implementation should be used.
/// </summary>
/// <param name="info">The target field</param>
/// <param name="target">The component the field belongs to</param>
public class BoundClassField(FieldInfo info, Component target) : IBoundProperty
{
    
    // TODO: Add check to ensure info belongs to target
    
    public Type GetMappedType()
    {
        return info.FieldType;
    }

    public void SetObject(object value)
    {
        if (Bounds != null && EnforceBounds)
        {
            value = value switch
            {
                // clamp value in bounds if it is a float double or int
                float f => Mathf.Clamp(f, Bounds.Value.min, Bounds.Value.max),
                double d => Math.Clamp(d, Bounds.Value.min, Bounds.Value.max),
                int i => Mathf.Clamp(i, (int)Bounds.Value.min, (int)Bounds.Value.max),
                _ => value
            };
        }
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
    
    public bool EnforceBounds { get; private set; } = info.GetCustomAttribute<SignalBoundsAttribute>()?.Enforced ?? false;
    public string? Units { get; private set; } = info.GetCustomAttribute<SignalUnitAttribute>()?.Units;

    public (float min, float max)? Bounds { get; private set; } =
        info.GetCustomAttribute<SignalBoundsAttribute>()?.Bounds;
}

/// <summary>
/// Indicates to UnityMapper the units to be associated with a signal.
/// Only used by the reflection-based property extractor.
/// </summary>
/// <param name="units">Human-readable unit for this signal</param>
[AttributeUsage(AttributeTargets.Field)]
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
/// <param name="enforced">If Unitymapper should clamp values between these bounds</param>
[AttributeUsage(AttributeTargets.Field)]
public class SignalBoundsAttribute(float min, float max, bool enforced = false) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
    
    public (float, float) Bounds => (Min, Max);

    public bool Enforced { get; } = enforced;
}

/// <summary>
/// When applied to a field, UnityMapper will ignore it when extracting properties.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MapperIgnoreAttribute : Attribute
{
    public MapperIgnoreAttribute() {}
}