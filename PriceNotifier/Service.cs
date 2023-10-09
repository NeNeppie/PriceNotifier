using System;
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
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    public static Configuration Config { get; set; } = null!;
    public static ItemPriceFetcher ItemPriceFetcher { get; set; } = null!;
    public static ItemWatchlist ItemWatchlist { get; set; } = null!;

    public static void Initialize(DalamudPluginInterface pi)
    {
        Config = (Configuration?)pi.GetPluginConfig() ?? new Configuration();
        Config.Initialize(pi);

        ItemWatchlist = new();
        ItemPriceFetcher = new();
    }

    public static void Dispose()
    {
        ItemPriceFetcher.Dispose();
        ItemWatchlist.Dispose();
        Config.Save();
    }
}
