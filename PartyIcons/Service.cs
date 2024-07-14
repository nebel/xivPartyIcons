using Dalamud.Game.Gui.NamePlate;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace PartyIcons;

internal class Service
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IPartyList PartyList { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IToastGui ToastGui { get; private set; } = null!;
    [PluginService] public static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static INamePlateGui NamePlateGui { get; private set; } = null!;

    internal static string Dump(object? o, string name = "", int depth = 3, bool showStatics = false)
    {
        try
        {
            var leafprefix = string.IsNullOrWhiteSpace(name) ? name : name + " = ";
            if (null == o) return leafprefix + "null";
            var t = o.GetType();
            if (depth-- < 1 || t == typeof (string) || t.IsValueType)
                return  leafprefix + o;
            var sb = new StringBuilder();
            if (o is IEnumerable enumerable)
            {
                name = (name??"").TrimEnd('[', ']') + '[';
                var elements = enumerable.Cast<object>().Select(e => Dump(e, "", depth)).ToList();
                var arrayInOneLine = elements.Count + "] = {" + string.Join(",", elements) + '}';
                if (!arrayInOneLine.Contains(Environment.NewLine)) // Single line?
                    return name + arrayInOneLine;
                var i = 0;
                foreach (var element in elements)
                {
                    var lineheader = name + i++ + ']';
                    sb.Append(lineheader).AppendLine(element.Replace(Environment.NewLine, Environment.NewLine+lineheader));
                }
                return sb.ToString();
            }
            foreach (var f in t.GetFields().Where(f => showStatics || !f.IsStatic))
                sb.AppendLine(Dump(f.GetValue(o), name + '.' + f.Name, depth));
            foreach (var p in t.GetProperties().Where(p => showStatics || p.GetMethod is not { IsStatic: true }))
                sb.AppendLine(Dump(p.GetValue(o, null), name + '.' + p.Name, depth));
            if (sb.Length == 0) return leafprefix + o;
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return name + "???";
        }
    }

}
