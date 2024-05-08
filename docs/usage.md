# Usage

## Building and Importing the Library

To build the library, use the `dotnet publish` command:

    dotnet publish -c Release -o ./publish

This will create a `publish` directory with the compiled library.

To use it in Unity, first ensure you have [libmapper](https://github.com/libmapper/libmapper) installed and available 
in your library search path. 

After making sure libmapper is properly installed, drag-and-drop `publish/UnityMapper.dll` onto your unity
assets window.

## Using Libmapper

The library includes a component, named `Libmapper Device`. In the Unity inspector, click Add Component and search 
for `Libmapper Device`.

The component exposes a few properties:
- `Poll Time`: The amount of time in milliseconds that libmapper will poll for events at once.
   The value you want depends on your target framerate and what your fixed timestep is set to. 1 ms is a good starting point.
- `Exposed Components`: The real meat and potatos of the  component, explained below:

### Exposed Components
This is an array containing a list of components libmapper should inspect to find exposed properties that will be
converted into signals. The components do not need to be owned by the `GameObject` that libmapper is attached to,
so you could have one libmapper device for your entire scene or one per object, it's up to you.

To get started, simply drag a component onto the list's title to automatically have it added. At the moment, the
following components have special handling:
- `Transform`: Maps to two three-component vectors for location and scale, and one four-component vector for rotation (Quaternion).

In [the future](https://github.com/EggAllocationService/libmapper-unity/issues/3), you will be able to add your own special handling
for your components.

#### Property Discovery

If no special handling exists for your component type, libmapper will use reflection to discover mappable public fields.
Mappability is determined by the following criteria:
- The field is not static
- The field is not readonly
- The field is not a property (has a defined getter and setter)
- The field is one of these types:
  - `int` or `int[]`
  - `float` or `float[]`
  - `double` or `double[]`
    - In the future, you will be able to register type adapters with libmapper so you can use the default property discoverer
      with more complex types.

Be careful with arrays, as libmapper does not support arrays of changing size. Also ensure that arrays are initialized to their
maximum size when they are constructed, for example:
```csharp
public float[] myArray = new float[3]; // good
public float[] myArray; // bad, libmapper can't infer vector size
```

### Metadata
Libmapper supports adding metadata to your signals, namely a unit and minimum/maximum bounds. If using a custom `PropertyExtractor`, implement
the getters for `Unit` and `Bounds` on your `IMappedProperty` to add metadata to your signals.

If you're using the reflection-based property extractor (the default), you can use the `SignalUnit` and `SignalBounds` attributes to add metadata to your signals. For example:
```csharp
    [SignalUnit("degrees"), SignalBounds(0f, 360f)]
    public float hue = 0.0f;
```

