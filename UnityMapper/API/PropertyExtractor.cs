using UnityEngine;

namespace UnityMapper.API;


/// <summary>
/// Extracts a list of mapped properties from a component.
/// </summary>
/// <typeparam name="T">Component type that properties will be extracted from</typeparam>
public interface IPropertyExtractor<T> where T : Component
{
    /// <summary>
    /// Create a list of mapped properties from a component.
    ///
    /// Each MappedProperty should contain a reference to the component so it can independently update or get values.
    /// </summary>
    /// <param name="component">Provided component</param>
    /// <returns></returns>
    List<IMappedProperty> ExtractProperties(T component);
}