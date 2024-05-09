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
    /// Each MappedProperty should contain a reference to the component so it can independently update or get values.
    /// </summary>
    /// <param name="component">Provided component</param>
    /// <returns></returns>
    List<IAccessibleProperty> ExtractProperties(T component);
    
    List<IAccessibleProperty> IPropertyExtractor.ExtractProperties(Component component)
    {
        if (!(component is T))
        {
            throw new ArgumentException($"Expected type {typeof(T)}, got {component.GetType()}");
        }
        return ExtractProperties((T) component);
    }
    Type IPropertyExtractor.ComponentType => typeof(T);
}

public interface IPropertyExtractor
{
    List<IAccessibleProperty> ExtractProperties(Component component);
    Type ComponentType { get; }
}