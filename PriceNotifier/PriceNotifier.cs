using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
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

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.FunkyFunc);

        //Service.Framework.Update += this.InitializeFetcher;
    }

    public void Dispose()
    {
        Service.Config.Save();
        this.ConfigWindow.Dispose();
        this.WindowSystem.RemoveAllWindows();

        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.FunkyFunc);

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

    // TODO: Hook a "Put Item For Sale" function or "Update Price" function. Also hook "Remove Item" by the same logic
    public unsafe void FunkyFunc(AddonEvent eventtype, AddonArgs addoninfo)
    {
        var addon = (AtkUnitBase*)addoninfo.Addon;
        if (addon is null || !addon->IsVisible) { return; }

        var items = Service.DataManager.GetExcelSheet<Item>()!.ToList();

        var countString = MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[0].String)).TextValue;
        var count = int.Parse(countString.Split('/')[0]);
        for (int i = 0; i < count; i++)
        {
            int atkIndex = (i + 1) * 10;
            var itemIcon = addon->AtkValues[atkIndex].Int;
            var itemName = MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[atkIndex + 1].String)).TextValue;
            // price per item -> atkIndex + 3
            Service.PluginLog.Debug($"{itemIcon} {itemName}");

            // HQ Check. Collectables are 500,000. Needs checking?
            if (itemIcon > 1000000)
            {
                itemName = itemName.Remove(itemName.Length - 2);
                itemIcon -= 1000000;
            }

            var item = items.Where(item => item.Icon == itemIcon && item.Name.RawString == itemName).ToList();
            if (item.Any())
                ConfigWindow.WatchList.Add(item[0]);
        }
    }
}

