using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using PriceNotifier.UI;

namespace PriceNotifier;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ConfigWindow _configWindow;

    public WindowSystem WindowSystem = new("PriceNotifier");

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Initialize(Service.PluginInterface);

        _configWindow = new();
        this.WindowSystem.AddWindow(_configWindow);

        Service.CommandManager.AddHandler("/pricenotifier", new CommandInfo(this.OnCommand)
        { HelpMessage = "Toggle Price Notifier's window" });

        Service.PluginInterface.UiBuilder.Draw += this.DrawUI;
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        Service.CommandManager.RemoveHandler("/pricenotifier");
        Service.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        _configWindow.IsOpen ^= true; // Wow so fancy
    }

    private void DrawUI()
    {
        this.WindowSystem.Draw();
    }
}

