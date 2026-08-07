# C# Plugin Host Demo

A minimal, fully working plugin-loading system for .NET 8, using a
collectible `AssemblyLoadContext` per plugin so plugins can be loaded
**and unloaded** at runtime without recompiling or restarting the host.

## Structure

```
PluginDemo.sln
src/
  PluginContracts/     <- shared IPlugin interface (host + plugins both reference this)
  Host/                <- the application that discovers/loads/runs/unloads plugins
  Plugins/
    HelloPlugin/        <- sample plugin: prints a greeting
    MathPlugin/          <- sample plugin: squares a number from shared context
build.sh                <- builds everything and stages plugin DLLs for the host
```

## Requirements

.NET 8 SDK. Check with `dotnet --version`; install from
https://dotnet.microsoft.com/download if needed.

## Build & run

```bash
./build.sh
dotnet run --project src/Host/Host.csproj
```

Expected output:

```
=== C# Plugin Host Demo ===

Scanning for plugins in: .../plugins

Loaded: HelloPlugin v1.0.0 - Prints a configurable greeting.
Loaded: MathPlugin v1.0.0 - Reads 'input.number' from context and squares it.

--- Running all plugins ---
  [host] HelloPlugin initialized.
  [host] Hello, World! (from HelloPlugin)
  [host] MathPlugin initialized.
  [host] 21 squared is 441

--- Context after execution ---
  input.number = 21
  hello.greetedName = World
  math.squared = 441

--- Unloading all plugins ---
  [host] HelloPlugin shutting down.
  [host] MathPlugin shutting down.
Done. Plugin assemblies unloaded and memory reclaimed.
```

## How it works

1. **`PluginContracts.IPlugin`** is the only thing host and plugins both
   compile against — `Name`, `Version`, `Description`, `Initialize`,
   `Execute`, `Shutdown`.
2. **`PluginManager.DiscoverPlugins`** scans `plugins/<Name>/*.dll` for
   folders whose DLL ends in "Plugin".
3. **`PluginManager.LoadPlugin`** creates a fresh `PluginLoadContext`
   (collectible `AssemblyLoadContext`) per plugin, loads its assembly,
   finds the first type implementing `IPlugin`, and instantiates it.
4. **`PluginLoadContext`** uses `AssemblyDependencyResolver` so each
   plugin's own NuGet dependencies resolve correctly, while
   `PluginContracts` itself is deliberately *not* reloaded — it falls
   through to the default context so host and plugin share the exact
   same `IPlugin` type.
5. **`PluginManager.UnloadPlugin`** calls `Shutdown()`, drops the last
   strong references, calls `AssemblyLoadContext.Unload()`, then loops
   `GC.Collect()` until the context reports itself collected (unloading
   is asynchronous in .NET).

## Adding your own plugin

1. `dotnet new classlib -o src/Plugins/MyPlugin -f net8.0`
2. Reference `PluginContracts.csproj` the same way the sample plugins do
   (`Private=false`, `ExcludeAssets=runtime`) so it doesn't duplicate the
   contracts DLL.
3. Implement `IPlugin` in a public class.
4. Add the project to `PluginDemo.sln` (or just build it standalone).
5. Copy the built DLL into `src/Host/bin/Debug/net8.0/plugins/MyPlugin/`
   (or add it to `build.sh`).

## Extending toward production use

- **Manifest files** — add a `plugin.json` per plugin folder (name,
  version, entry DLL, dependencies) instead of relying on filename
  conventions, and have `DiscoverPlugins` read that.
- **Sandboxing** — this demo runs plugins in-process with full trust.
  For untrusted third-party plugins, run them out-of-process and talk
  over gRPC/named pipes instead of `AssemblyLoadContext`.
- **Dependency ordering** — if plugins can depend on each other, add a
  topological sort over declared dependencies before calling `Execute`.
- **Hot reload** — pair `FileSystemWatcher` on the plugins folder with
  `UnloadPlugin` + `LoadPlugin` to reload a plugin when its DLL changes.
