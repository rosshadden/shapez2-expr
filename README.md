# Shapez 2 mod template

this is a template for shapez 2 mods

## Usage

```
dotnet new install Raphdf201.Shapez2Template
dotnet new shapez2mod -n MyMod -o ./MyMod
```

dont forget to customize the manifest.json

to uninstall the template : `dotnet new uninstall Raphdf201.Shapez2Template`

## Releasing new versions

```
nuget pack .\.nuspec -NoDefaultExcludes
```
