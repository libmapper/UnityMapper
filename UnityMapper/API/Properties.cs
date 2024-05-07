using System.Reflection;
using System.Reflection.Emit;
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
    /// This doesn't have to be the underlying type of the property, but if it is not a float, int,
    /// double, or an array of one of those a type mapper will be used which may be less efficient.
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

/// <summary>
/// Implementation of <see cref="IMappedProperty"/> that uses runtime code generation to improve performance.
///
/// Roughly 5x faster than <see cref="MappedClassField"/>, but has potential issues on iOS.
/// </summary>
public class RCGClassField : IMappedProperty
{
    
    private delegate void SetObjectDelegate(object target, object value);
    private delegate object GetValueDelegate(object target);
    
    private object _target;
    private FieldInfo _info;
    private SetObjectDelegate _setter;
    private GetValueDelegate _getter;

    public RCGClassField(FieldInfo info, Component target)
    {
        _target = target;
        _info = info;
        _setter = CreateSetter();
        _getter = CreateGetter();
    }

    public Type GetMappedType()
    {
        return _info.FieldType;
    }

    public void SetObject(object value)
    {
        _setter(_target, value);
    }

    public object GetValue()
    {
        return _getter(_target);
    }

    public string GetName()
    {
        return _info.DeclaringType.Name + "/" + _info.Name;
    }

    private GetValueDelegate CreateGetter()
    {
        var d = new DynamicMethod($"Get{_info.Name}_Generated", typeof(object), new[] {typeof(object)}, _info.DeclaringType);
        var il = d.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); // "this"
        il.Emit(OpCodes.Castclass, _info.DeclaringType); // cast to the declaring type
        il.Emit(OpCodes.Ldfld, _info); // "this".{_info}
        if (_info.FieldType.IsValueType) // box it if it's a value type
        {
            il.Emit(OpCodes.Box, _info.FieldType);
        }
        il.Emit(OpCodes.Ret); // return the value
        return (GetValueDelegate) d.CreateDelegate(typeof(GetValueDelegate));
    }
    
    private SetObjectDelegate CreateSetter()
    {
        var d = new DynamicMethod($"Set{_info.Name}_Generated", typeof(void), new[] {typeof(object), typeof(object)}, _info.DeclaringType);
        var il = d.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); // "this"
        il.Emit(OpCodes.Castclass, _info.DeclaringType); // cast to the declaring type
        il.Emit(OpCodes.Ldarg_1); // value
        if (_info.FieldType.IsValueType) // unbox it if it's a value type
        {
            il.Emit(OpCodes.Unbox_Any, _info.FieldType);
        }
        il.Emit(OpCodes.Stfld, _info); // "this".{_info} = value
        il.Emit(OpCodes.Ret); // return
        return (SetObjectDelegate) d.CreateDelegate(typeof(SetObjectDelegate));
    }
    public string? Units
    {
        get {
            var attr = _info.GetCustomAttribute<SignalUnitAttribute>();
            return attr?.Units;
        }
    }
    
    public (float min, float max)? Bounds
    {
        get
        {
            var attr = _info.GetCustomAttribute<SignalBoundsAttribute>();
            if (attr == null)
            {
                return null;
            }
            return (attr.Min, attr.Max);
        }
    }
}