using System.Reflection;
using UnityEngine;

namespace UnityMapper.API;


/// <summary>
/// Extracts a list of mapped properties from a component.
/// </summary>
/// <typeparam name="T">Component type that properties will be extracted from</typeparam>
public interface IPropertyExtractor<T> : IPropertyExtractor where T : Component 
{
    /// <summary>
    /// Create a list of mapped properties from a component.
    ///
    /// Each MappedProperty should contain a reference to the component, so it can independently update or get values.
    /// </summary>
    /// <param name="component">Provided component</param>
    /// <returns></returns>
    List<IMappedProperty> ExtractProperties(T component);
    
    List<IMappedProperty> IPropertyExtractor.ExtractProperties(Component component)
    {
        if (!(component is T))
        {
            throw new ArgumentException($"Expected type {typeof(T)}, got {component.GetType()}");
        }
        return ExtractProperties((T) component);
    }
}

public interface IPropertyExtractor
{
    List<IMappedProperty> ExtractProperties(Component component);
}

/// <summary>
/// Reflection-based property extractor that maps fields to libmapper signals.
/// </summary>
/// <param name="_converters">Available type converters to primitivize types</param>
public class DefaultPropertyExtractor(Dictionary<Type, ITypeConverter> _converters) : IPropertyExtractor
{
    public List<IMappedProperty> ExtractProperties(Component target)
    {
        var candidates = target.GetType().GetFields(BindingFlags.Instance)
            .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) // unity rules
            .ToList();
        
        Debug.Log("Extracting properties from " + target.GetType());
        var l = new List<IMappedProperty>();
        foreach (var prop in candidates)
        {
            var baseType = LibmapperDevice.CreateLibmapperTypeFromPrimitive(prop.FieldType);
            if (baseType == Mapper.Type.Null && !_converters.ContainsKey(prop.FieldType)) continue;
            var mapped = new MappedClassField(prop, target);
                
            if (baseType == Mapper.Type.Null) // this type needs to be wrapped in order to be turned into a signal
            {
                var converter = _converters[prop.FieldType];
                l.Add(new WrappedMappedProperty(mapped, converter));
                Debug.Log("Extracted property: " + prop.Name + " of type: " + converter.SimpleType + " for libmapper.");
            }
            else
            {
                l.Add(mapped);
                Debug.Log("Extracted property: " + prop.Name + " of type: " + baseType + " for libmapper.");
            }
        }

        return l;
    }
}