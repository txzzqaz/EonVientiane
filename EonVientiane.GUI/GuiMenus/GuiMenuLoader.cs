using System.Reflection;

namespace EonVientiane.GUI.GuiMenus;

public sealed record GuiModuleBundle(
    IReadOnlyList<GuiMenuDefinition> Menus,
    IReadOnlyDictionary<string, IGuiContentModule> ContentProviders);

public static class GuiMenuLoader
{
    public static GuiModuleBundle LoadModules(string workspaceRoot)
    {
        var menuModules = new List<IGuiMenuModule>();
        var contentModules = new Dictionary<string, IGuiContentModule>(StringComparer.OrdinalIgnoreCase);

        var externalModulesDirectory = Path.Combine(workspaceRoot, "gui-modules");
        if (Directory.Exists(externalModulesDirectory))
        {
            foreach (var dllPath in Directory.GetFiles(externalModulesDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                TryLoadFromAssembly(dllPath, menuModules, contentModules);
            }
        }

        var menus = menuModules
            .Select(x => x.GetMenu())
            .GroupBy(x => x.ModuleId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Order)
            .ToList();

        return new GuiModuleBundle(menus, contentModules);
    }

    private static void TryLoadFromAssembly(
        string dllPath,
        List<IGuiMenuModule> menuModules,
        Dictionary<string, IGuiContentModule> contentModules)
    {
        try
        {
            var assembly = Assembly.LoadFrom(dllPath);
            foreach (var t in assembly.GetTypes())
            {
                if (!t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (typeof(IGuiMenuModule).IsAssignableFrom(t) &&
                        Activator.CreateInstance(t) is IGuiMenuModule menu)
                    {
                        menuModules.Add(menu);
                    }

                    if (typeof(IGuiContentModule).IsAssignableFrom(t) &&
                        Activator.CreateInstance(t) is IGuiContentModule content)
                    {
                        contentModules.TryAdd(content.ModuleId, content);
                    }
                }
            }
        }
        catch
        {
        }
    }
}
