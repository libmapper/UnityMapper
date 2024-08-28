# UnityMapper

UnityMapper is a wrapper for [libmapper](https://github.com/libmapper/libmapper) that allows you to easily integrate your
Unity projects with other libmapper devices.

## Quick Start

1. Download the zip file containing the binaries from the [latest release](https://github.com/libmapper/UnityMapper/releases/latest).
2. Extract the contents of the zip file into your Unity project's `Assets` directory.
3. Add the `Libmapper Device` component to the highest level GameObject you want to be visible to libmapper.
4. Add `Libmapper Component List` components to descendant GameObjects that have interesting components.
5. Drag and drop interesting components onto the list on the `Libmapper Component List` component to automatically expose their
    configurable properties as signals.

## Compiling
This project uses the `dotnet`/`.csproj` build system. You can compile the project using the `dotnet` command:

```bash
dotnet build
```
The compiled DLL will be in the `bin/Debug` directory.

To get an optimized DLL file for more serious use, use `dotnet publish` instead:
```bash
dotnet publish -c Release -o ./publish
```


## Usage
See [docs/usage.md](docs/usage.md) and [docs/api.md](docs/api.md)