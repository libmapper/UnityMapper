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

public class SignalVectorSizeAttribute(int size) : Attribute
{
    public int Size { get; } = size;
}

public class AccessorGenerator
{
    public static Type CreateAccessor(FieldInfo info, ModuleBuilder builder)
    {
        var vectorLength = info.GetCustomAttribute<SignalVectorSizeAttribute>()?.Size ?? 1;
        
        var typeName = "Accessor_" + info.Name + "_" + info.DeclaringType.Name;
        var type = builder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, null, [typeof(IMappedProperty)]);
        var refField = type.DefineField("_ref", info.DeclaringType, FieldAttributes.Private);
        
        // implement constructor new(Component type) -> store in _ref
        var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] {info.DeclaringType});
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, refField);
        
        // implement GetVectorLength
        var getVectorLength = type.DefineMethod("GetVectorLength", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(int), null)!;
        il = getVectorLength.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, vectorLength);
        il.Emit(OpCodes.Ret);
        
        // implement GetMappedType
        var getMappedType = type.DefineMethod("GetMappedType", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(Type), null)!;
        il = getMappedType.GetILGenerator();
        il.Emit(OpCodes.Ldtoken, info.FieldType);
        il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
        il.Emit(OpCodes.Ret);
        
        // implement SetObject
        var setObject = type.DefineMethod("SetObject", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(void), new Type[] {typeof(object)})!;
        il = setObject.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, refField);
        il.Emit(OpCodes.Ldarg_1);
        if (info.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, info.FieldType);
        }
        il.Emit(OpCodes.Stfld, info);
        
        // implement GetValue
        var getValue = type.DefineMethod("GetValue", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(object), null)!;
        il = getValue.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, refField);
        if (info.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Box, info.FieldType);
        }
        il.Emit(OpCodes.Ret);
        
        
        // implement GetName
        var getName = type.DefineMethod("GetName", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(string), null)!;
        il = getName.GetILGenerator();
        il.Emit(OpCodes.Ldstr, info.DeclaringType.Name + "/" + info.Name);
        il.Emit(OpCodes.Ret);
        
        // TODO: implement Units and Bounds as something other than null
        // implement Units and Bounds as null constants
        var units = type.DefineProperty("Units", PropertyAttributes.None, typeof(string), null);
        var bounds = type.DefineProperty("Bounds", PropertyAttributes.None, typeof((float, float)?), null);
        var unitsGetter = type.DefineMethod("get_Units", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(string), null)!;
        il = unitsGetter.GetILGenerator();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);
        
        var boundsGetter = type.DefineMethod("get_Bounds", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof((float, float)?), null)!;
        il = boundsGetter.GetILGenerator();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);
        
        
        units.SetGetMethod(unitsGetter);
        bounds.SetGetMethod(boundsGetter);
        
        return type.CreateType();
    }
}
