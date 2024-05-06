namespace UnityMapper.API;

/// <summary>
/// Converts from complex types to simple types that can be assigned to a signal.
/// </summary>
/// <typeparam name="T">Complex type</typeparam>
/// <typeparam name="U">Primitive type, one of float, int, double, or an array</typeparam>
public interface ITypeMapper<T, U> : ITypeMapper where T : class where U : class

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
    
    
    object ITypeMapper.CreateSimpleObject(object complex)
    {
        if (!(complex is T))
        {
            throw new ArgumentException($"Expected type {typeof(T)}, got {complex.GetType()}");
        }
        return CreateSimple((T) complex);
    }
    
    object ITypeMapper.CreateComplexObject(object simple)
    {
        if (!(simple is U))
        {
            throw new ArgumentException($"Expected type {typeof(U)}, got {simple.GetType()}");
        }
        return CreateComplex((U) simple);
    }
    
    Type ITypeMapper.ComplexType => typeof(T);
    Type ITypeMapper.SimpleType => typeof(U);
}


/// <summary>
/// Dynamic version of <see cref="ITypeMapper{T,U}"/>. Only useful for internal use.
/// </summary>
public interface ITypeMapper
{
    object CreateComplexObject(object simple);
    object CreateSimpleObject(object complex);
    
    int VectorLength { get; }
    
    Type ComplexType { get; }
    Type SimpleType { get; }
}