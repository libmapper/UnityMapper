# UnityMapper

UnityMapper is a wrapper for [libmapper](https://github.com/libmapper/libmapper) that allows you to easily integrate your
Unity projects with other libmapper devices.

## Compiling
This project uses the `dotnet`/`.csproj` build system. You can compile the project using the `dotnet` command:

```bash
dotnet build
```
The compiled DLL will be in the `bin/Debug` directory.

To get an optimized DLL file for more serious use, use `dotnet build` instead:
```bash
dotnet publish -c Release -o ./publish
```


## Usage
See [docs/usage.md](docs/usage.md) and [docs/api.md](docs/api.md)