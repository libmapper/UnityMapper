# Developer API

If you want to extend UnityMapper's functionality to support your use case, this is the document for you!

Using the public API, you can tell UnityMapper how to look for properties on your components, or how to convert those properties into primitives.
## Figuring out what you need

The API is split up into multiple parts that can be used independently or together. To figure out what you need to do, consult the following steps:
1. Is my only problem a custom type that UnityMapper can't use?
    - If yes, you want to register a `TypeConverter` to tell UnityMapper how to convert your type into a primitive.
    - If your type can't be directly converted, or it doesn't make semantic sense to only have one signal representing it, read on.
2. Is my problem that UnityMapper isn't discovering all the properties available on my component?
    - If yes, you want to register a `PropertyExtractor` to tell UnityMapper how to find the properties you want to expose.

You may need to register both `TypeConverter` and `PropertyExtractor` to get the desired behavior. If you're not sure, have a look through
the `Builtin` namespace to see how UnityMapper handles common Unity types.

## Prerequisites
Create a custom class that derives from `LibmapperDevice`. This will allow you override the necessary methods to register your custom extensions.

## Type Converters
Type converters are the simplest part of the API to implement. They allow you to tell UnityMapper how to convert your custom types to simple numerical values.

An example implementation for Unity's `Vector3` type is shown below:
```csharp
public class MyVector3Converter : ITypeConverter<Vector3, float[]>
{
    public float[] CreateSimple(Vector3 complex)
    {
        return new float[] {complex.x, complex.y, complex.z};
    }

    public Vector3 CreateComplex(float[] simple)
    {
        return new Vector3(simple[0], simple[1], simple[2]);
    }

    public int VectorLength => 3;
}
```
Let's break down what's happening here:
- `ITypeConverter<Vector3, float[]>` tells UnityMapper that this will convert a `Vector3` to a `float[]` and vice versa.
- `CreateSimple` is called when UnityMapper needs to convert a `Vector3` to a `float[]`.
- `CreateComplex` is called when UnityMapper needs to convert a `float[]` to a `Vector3`.
- `VectorLength` is needed when you're returning an array of primitives. Libmapper only supports fixed-length vectors, so this
  will tell libmapper how many elements are in the vector. If you're returning a single value, this would read `public int VectorLength => 1;`.

To register your custom type converter, override `RegisterExtensions`:
```csharp
    public override void RegisterExtensions()
    {
        RegisterTypeConverter(new MyVector3Converter());
    }
```

## Property Extractors
Property extractors are a bit more complex than type converter, but they allow you to tell UnityMapper how to find the properties you want to expose.

Here's a (simplified) example implementation for Unity's `Transform` component:
```csharp
public class TransformExtractor : IPropertyExtractor<Transform>
{
    public List<IBoundProperty> ExtractProperties(Transform component)
    {
        return
        [
            new MappedPosition(component)
            // position and rotation would go here
        ];
    }
}
```
The `PropertyExtractor` itself is actually fairly simple. You need to specify what kind of Component it supports in the generic type, and implement the `ExtractProperties` method.

You need to return a set of `IBoundProperty` objects. These objects are what UnityMapper uses to actually get and set the values of the properties you're exposing.
Each `IBoundProperty` should contain a reference to the Unity component and some metadata, an example for a unity `Transform` is seen below:

> [!IMPORTANT]  
> Note that `GetVectorLength` returns one in the below example, even though a Transform is a 3-dimensional vector. This is correct because you're only returning one `Vector3`. The `ITypeConverter` will convert this to a
> `float[3]`, which will override this vector length property.

```csharp
internal class BoundPosition(Transform transform) : IBoundProperty
{
    public void SetObject(object val)
    {
        transform.position = (Vector3) val;
    }
    public object GetValue()
    {
        return transform.position;
    }

    public Type GetMappedType()
    {
        return typeof(Vector3);
    }

    public int GetVectorLength()
    {
        return 1;
    }

    public string GetName()
    {
        return "Position";
    }
}
```


Note that this implementation is actually different from the one in the source code. The real implementation actually maps directly to a `float[]`,
instead of letting UnityMapper do the additional step of converting the `Vector3`. This is a bit faster in practice, but you can likely just use a built-in or custom type converter
unless your property is performance-critical.

Hopefully, in the [future](https://github.com/EggAllocationService/libmapper-unity/issues/5), UnityMapper will be able to generate classes implementing IMappedProperty for you.

To register your property extractor override the `RegisterExtensions` method and call `RegisterPropertyExtractor` like so:
```csharp
    public override void RegisterExtensions()
    {
        RegisterPropertyExtractor(new TransformExtractor());
    }
```