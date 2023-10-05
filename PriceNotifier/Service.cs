using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PriceNotifier;

internal sealed class Service
{
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;

    public static Configuration Config { get; set; } = null!;

    public static void Initialize(DalamudPluginInterface pi)
    {
        Config = (Configuration?)pi.GetPluginConfig() ?? new Configuration();
        Config.Initialize(pi);
    }
}
