namespace UnityMapper.API;

/// <summary>
/// Converts from complex types to simple types that can be assigned to a signal.
/// </summary>
/// <typeparam name="T">Complex type</typeparam>
/// <typeparam name="U">Primitive type, one of float, int, double, or an array</typeparam>
public interface ITypeConverter<T, U> : ITypeConverter where T: notnull where U: notnull

{
    /// <summary>
    /// Converts a complex type to a simple type.
    /// </summary>
    /// <param name="complex">The complex type to convert</param>
    /// <returns>A simple type that can be assigned to a signal</returns>
    U CreateSimple(T complex);
    
    /// <summary>
    /// Converts a simple type to a complex type.
    /// </summary>
    /// <param name="simple">The simple type to convert</param>
    /// <returns>A complex type</returns>
    T CreateComplex(U simple);
    
    
    object ITypeConverter.CreateSimpleObject(object complex)
    {
        if (!(complex is T))
        {
            throw new ArgumentException($"Expected type {typeof(T)}, got {complex.GetType()}");
        }
        return CreateSimple((T) complex);
    }
    
    object ITypeConverter.CreateComplexObject(object simple)
    {
        if (!(simple is U))
        {
            throw new ArgumentException($"Expected type {typeof(U)}, got {simple.GetType()}");
        }
        return CreateComplex((U) simple);
    }
    
    Type ITypeConverter.ComplexType => typeof(T);
    Type ITypeConverter.SimpleType => typeof(U);
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