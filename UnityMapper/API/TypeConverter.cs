namespace UnityMapper.API;

/// <summary>
/// Converts from complex types to simple types that can be assigned to a signal.
/// </summary>
/// <typeparam name="TComplex">Complex type</typeparam>
/// <typeparam name="TSimple">Primitive type, one of float, int, double, or an array</typeparam>
public interface ITypeConverter<TComplex, TSimple> : ITypeConverter where TComplex: notnull where TSimple: notnull

{
    /// <summary>
    /// Converts a complex type to a simple type.
    /// </summary>
    /// <param name="complex">The complex type to convert</param>
    /// <returns>A simple type that can be assigned to a signal</returns>
    TSimple CreateSimple(TComplex complex);
    
    /// <summary>
    /// Converts a simple type to a complex type.
    /// </summary>
    /// <param name="simple">The simple type to convert</param>
    /// <returns>A complex type</returns>
    TComplex CreateComplex(TSimple simple);
    
    
    object ITypeConverter.CreateSimpleObject(object complex)
    {
        if (!(complex is TComplex))
        {
            throw new ArgumentException($"Expected type {typeof(TComplex)}, got {complex.GetType()}");
        }
        return CreateSimple((TComplex) complex);
    }
    
    object ITypeConverter.CreateComplexObject(object simple)
    {
        if (!(simple is TSimple))
        {
            throw new ArgumentException($"Expected type {typeof(TSimple)}, got {simple.GetType()}");
        }
        return CreateComplex((TSimple) simple);
    }
    
    Type ITypeConverter.ComplexType => typeof(TComplex);
    Type ITypeConverter.SimpleType => typeof(TSimple);
}


/// <summary>
/// Dynamic version of <see cref="ITypeConverter{T,U}"/>. Only useful for internal use.
/// </summary>
public interface ITypeConverter
{
    object CreateComplexObject(object simple);
    object CreateSimpleObject(object complex);
    
    int VectorLength { get; }
    
    Type ComplexType { get; }
    Type SimpleType { get; }
}