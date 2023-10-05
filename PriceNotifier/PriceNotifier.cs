using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PriceNotifier.UI;

namespace PriceNotifier;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Price Notifier";

    public WindowSystem WindowSystem = new("PriceNotifier");

    private ConfigWindow ConfigWindow;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Initialize(Service.PluginInterface);

        this.ConfigWindow = new();
        this.WindowSystem.AddWindow(this.ConfigWindow);

        Service.CommandManager.AddHandler("/pricenotifier", new CommandInfo(this.OnCommand)
        { HelpMessage = "Toggle Price Notifier's window" });

        Service.PluginInterface.UiBuilder.Draw += this.DrawUI;

        //Service.Framework.Update += this.InitializeFetcher;
    }

    public void Dispose()
    {
        this.ConfigWindow.Dispose();
        this.WindowSystem.RemoveAllWindows();

        //Service.Framework.Update -= this.InitializeFetcher;

        Service.CommandManager.RemoveHandler("/pricenotifier");
    }

    private void OnCommand(string command, string args)
    {
        this.ConfigWindow.IsOpen ^= true; // Wow so fancy
    }

    private void DrawUI()
    {
        this.WindowSystem.Draw();
    }

    /*public unsafe void InitializeFetcher(IFramework framework)
    {
        if (ItemPriceFetcher.IsActive || !Service.ClientState.IsLoggedIn) { return; }

        this.ItemPriceFetcher = new();
        
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName("RetainerSellList");
        if (addon is null || !addon->IsVisible)
        {
            this.isActive = false;
            return;
        }

        if (!this.isActive)
        {
            var itemsString = MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[0].String)).TextValue;
            var items = int.Parse(itemsString.Split('/')[0]);
            for (int i = 0; i < items; i++)
            {
                int atkIndex = (i + 1) * 10;
                var itemIconId = addon->AtkValues[atkIndex].Int;
                var itemName = MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[atkIndex + 1].String)).TextValue;
                PluginLog.Debug($"{itemIconId} {itemName}");

                // HQ Check. Collectables are 500,000. Needs checking?
                if (itemIconId > 1000000)
                    itemIconId -= 1000000;

                //Service.DataManager.Excel.GetSheet<Item>()?.GetRow();
                // TODO: this addon doesn't store item IDs. Alternatives to storing info on items being sold:
                //      * Manually, via a config window (Better to start with this)
                //      * Hook a "Put Item For Sale" function or "Update Price" function. Also hook "Remove Item" by the same logic
            }
            this.isActive = true;
        }
    } */
}

