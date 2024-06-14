# Usage

## Building and Importing the Library

To build the library, use the `dotnet publish` command:

    dotnet publish -c Release -o ./publish

This will create a `publish` directory with the compiled library.

To use it in Unity, first ensure you have [libmapper](https://github.com/libmapper/libmapper) installed and available 
in your library search path. 

After making sure libmapper is properly installed, drag-and-drop `publish/UnityMapper.dll` onto your unity
assets window.

### Getting Libmapper

Follow the instructions how to build libmapper [here](https://github.com/libmapper/libmapper/blob/main/doc/how_to_compile_and_run.md).

On Windows you'll need three files: `libmapper.dll`, `liblo.dll`, and `zlib.dll`. Drag those three files into your Unity assets, making an appropriate folder is a good idea!

Make sure in the Unity inspector those library files are set to be used on the correct platform. For example, on Windows, you'll want to set them to be used on Windows, only x86/x64.

## Using Libmapper

The library includes a component, named `Libmapper Device`. In the Unity inspector, click Add Component and search 
for `Libmapper Device`.

The component exposes a few properties:
- `Poll Time`: The amount of time in milliseconds that libmapper will poll for events at once.
   The value you want depends on your target framerate and what your fixed timestep is set to. 1 ms is a good starting point.
- `Non Blocking Polling`: Use libmapper's non-blocking polling feature on the main thread instead of scheduling a job. `Poll Time`
   will be ignored if this is enabled. Note that this seems to have some latency variation issues that are being investigated.
- `Use API`: If checked, the device will not start until you call `Freeze()`. This allows you to register custom type converters
   and property extractors before the device is initalized.

### Exposing Components

Your GameObject with the `Libmapper Device` shoud be a parent to any GameObjects you want to expose. The `Libmapper Device` will
search for any children with the `Libmapper Component List` component and scan them.

The `Libmapper Component List` component allows you to specify which components you want to expose. Simply drag and drop any component from the
GameObject you want to expose onto the list, and as long as it is a child or descendent of the GameObject with the `Libmapper Device`, it will be exposed as a signal.

If there are properties with similar names at a similar path (e.g cloned objects next to each other in the hierarchy),
UnityMapper will express these as instances of the same signal. UnityMapper will group objects with the same name, dropping a `.[0-9]+` suffix.

At the moment, the following components have special handling:
- `Transform`: Maps to two three-component vectors for location and scale, and one four-component vector for rotation (Quaternion).
- `Camera`: Provides a single float for the camera's field of view.
- `AudioSource`: Provides two floats for the volume and pitch of the audio source.
- `Light`: Provides a three-component vector for the light's RGB color and a float for the intensity (candelas).

You can add your own behavior to this list or override existing behavior, see [api.md](api.md) for more information.

#### Property Discovery

If no special handling exists for your component type, libmapper will use reflection to discover mappable public fields.
Discoverability is determined by the following criteria:
- The field is not static
- The field is either public or has the `[SerializeField]` attribute
- The field does not have the `[MapperIgnore]` attribute
- The field is not readonly
- The field is one of these types (or a registered `ITypeConverter` can convert it to one of these):
  - `int` or `int[]`
  - `float` or `float[]`
  - `double` or `double[]`

### Metadata
Libmapper supports adding metadata to your signals, namely a unit and minimum/maximum bounds. If using a custom `PropertyExtractor`, implement
the getters for `Unit` and `Bounds` on your `IMappedProperty` to add metadata to your signals.

If you're using the reflection-based property extractor (the default), you can use the `SignalUnit` and `SignalBounds` attributes to add metadata to your signals. For example:
```csharp
    [SignalUnit("degrees"), SignalBounds(0f, 360f)]
    public float hue = 0.0f;
```

You can also tell UnityMapper to automatically clamp values to the bounds you've set by setting the `enforce` parameter on the `SignalBounds` attribute:
```csharp
[SignalBounds(0f, 1f, enforce: true)]
public float bounciness = 0.5f;
```
Note that this only works when using the reflection-based property extractor, and has no effect for array types.
