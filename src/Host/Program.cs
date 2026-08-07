using Host;
using PluginContracts;

Console.WriteLine("=== C# Plugin Host Demo ===\n");

// The plugins folder sits next to the host's build output. Each plugin gets
// its own subfolder containing its DLL (and any dependencies it needs).
var pluginsRoot = Path.Combine(AppContext.BaseDirectory, "plugins");

var hostContext = new HostContext(config: new Dictionary<string, string>
{
    ["greeting.name"] = "World"
});

var manager = new PluginManager(hostContext);

Console.WriteLine($"Scanning for plugins in: {pluginsRoot}\n");
var pluginPaths = manager.DiscoverPlugins(pluginsRoot).ToList();

if (pluginPaths.Count == 0)
{
    Console.WriteLine("No plugins found. Build the plugin projects and copy their");
    Console.WriteLine("output DLLs into subfolders under ./plugins first (see README).");
    return;
}

foreach (var path in pluginPaths)
{
    try
    {
        var plugin = manager.LoadPlugin(path);
        Console.WriteLine($"Loaded: {plugin.Name} v{plugin.Version} - {plugin.Description}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load {Path.GetFileName(path)}: {ex.Message}");
    }
}

Console.WriteLine("\n--- Running all plugins ---");
var context = new PluginContext();
context.Data["input.number"] = 21;
manager.RunAll(context);

Console.WriteLine("\n--- Context after execution ---");
foreach (var kv in context.Data)
    Console.WriteLine($"  {kv.Key} = {kv.Value}");

Console.WriteLine("\n--- Unloading all plugins ---");
manager.UnloadAll();
Console.WriteLine("Done. Plugin assemblies unloaded and memory reclaimed.");
