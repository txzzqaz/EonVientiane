using System.Reflection;

namespace EonVientiane.GUI.GuiMenus;

public sealed record GuiModuleBundle(
    IReadOnlyList<GuiMenuDefinition> Menus,
    IReadOnlyDictionary<string, GuiContentProviderDefinition> ContentProviders);

public static class GuiMenuLoader
{
    public static GuiModuleBundle LoadModules()
    {
        var menuDefinitions = new List<GuiMenuDefinition>();
        var contentModules = new Dictionary<string, GuiContentProviderDefinition>(StringComparer.OrdinalIgnoreCase);

        LoadFromCurrentAppDomain(menuDefinitions, contentModules);

        var menus = menuDefinitions
            .GroupBy(x => x.ModuleId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Order)
            .ToList();

        return new GuiModuleBundle(menus, contentModules);
    }

    private static void LoadFromCurrentAppDomain(
        List<GuiMenuDefinition> menuDefinitions,
        Dictionary<string, GuiContentProviderDefinition> contentModules)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            TryLoadFromAssembly(assembly, menuDefinitions, contentModules);
        }
    }

    private static void TryLoadFromAssembly(
        Assembly assembly,
        List<GuiMenuDefinition> menuDefinitions,
        Dictionary<string, GuiContentProviderDefinition> contentModules)
    {
        try
        {
            foreach (var t in assembly.GetTypes())
            {
                if (TryGetConventionMenuDefinition(t, out var conventionMenu))
                {
                    menuDefinitions.Add(conventionMenu);

                    if (HasConventionContentDefinition(t))
                    {
                        contentModules.TryAdd(
                            conventionMenu.ModuleId,
                            new GuiContentProviderDefinition(conventionMenu.ModuleId, t));
                    }
                }
            }
        }
        catch
        {
        }
    }

    private static bool HasConventionContentDefinition(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        return type.GetMethod("GetGuiContentDefinition", flags) != null;
    }

    private static bool TryGetConventionMenuDefinition(Type type, out GuiMenuDefinition menu)
    {
        menu = default!;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        var method = type.GetMethod("GetGuiMenuDefinition", flags, binder: null, types: Type.EmptyTypes, modifiers: null);
        if (method == null)
        {
            return false;
        }

        object? instance = null;
        if (!method.IsStatic)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                return false;
            }

            instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                return false;
            }
        }

        var raw = method.Invoke(instance, null);
        if (raw is GuiMenuDefinition direct)
        {
            menu = direct;
            return true;
        }

        if (raw is not IDictionary<string, object> map)
        {
            return false;
        }

        if (!map.TryGetValue("ModuleId", out var moduleIdObj) ||
            !map.TryGetValue("Title", out var titleObj) ||
            !map.TryGetValue("Buttons", out var buttonsObj))
        {
            return false;
        }

        var moduleId = moduleIdObj?.ToString();
        var title = titleObj?.ToString();
        if (string.IsNullOrWhiteSpace(moduleId) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var layout = GuiMenuLayout.Vertical;
        if (map.TryGetValue("Layout", out var layoutObj) &&
            layoutObj is string layoutText &&
            Enum.TryParse<GuiMenuLayout>(layoutText, ignoreCase: true, out var parsedLayout))
        {
            layout = parsedLayout;
        }

        var order = 0;
        if (map.TryGetValue("Order", out var orderObj) && orderObj != null)
        {
            _ = int.TryParse(orderObj.ToString(), out order);
        }

        var buttons = new List<GuiMenuButton>();
        if (buttonsObj is IEnumerable<object> buttonList)
        {
            foreach (var entry in buttonList)
            {
                if (entry is not IDictionary<string, object> buttonMap)
                {
                    continue;
                }

                if (!buttonMap.TryGetValue("Text", out var textObj) ||
                    !buttonMap.TryGetValue("Command", out var commandObj))
                {
                    continue;
                }

                var text = textObj?.ToString();
                var command = commandObj?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var activatesContent = false;
                if (buttonMap.TryGetValue("ActivatesContent", out var activatesObj) && activatesObj != null)
                {
                    _ = bool.TryParse(activatesObj.ToString(), out activatesContent);
                }

                buttons.Add(new GuiMenuButton(text, command, activatesContent));
            }
        }

        if (buttons.Count == 0)
        {
            return false;
        }

        menu = new GuiMenuDefinition(moduleId, title, layout, order, buttons);
        return true;
    }
}
