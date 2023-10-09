using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier;

public record WatchlistEntry(Item item, int Price, bool IsHQ)
{
    public Item Item { get; init; } = item;
    public int Price { get; set; } = Price;
    public bool HQ { get; set; } = IsHQ;
}

// TODO: Hook a "Put Item For Sale" function or "Update Price" function. Also hook "Remove Item" by the same logic
public class ItemWatchlist
{
    // Icon?    Name    Price(1)[ 0xE049]    HQ(w/ the name?)[ 0xE03C]
    public HashSet<WatchlistEntry> Entries = new();

    public ItemWatchlist()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSellList", this.GetRetainerSellList);
    }

    private static int ParseItemPrice(string itemPriceString) => int.Parse(itemPriceString.Remove(itemPriceString.Length - 1).Replace(",", ""));

    public unsafe void GetRetainerSellList(AddonEvent eventtype, AddonArgs addoninfo)
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
            var itemName = MemoryHelper.ReadSeStringNullTerminated((nint)addon->AtkValues[atkIndex + 1].String).TextValue;
            var itemPrice = ParseItemPrice(MemoryHelper.ReadSeStringNullTerminated((nint)addon->AtkValues[atkIndex + 3].String).TextValue);
            var isHQ = itemIcon > 1000000;

            Service.PluginLog.Debug($"{itemIcon} {itemName} {itemPrice}\xE049");

            // HQ Check. Collectables are 500,000.
            if (isHQ)
            {
                itemName = itemName.Remove(itemName.Length - 2);
                itemIcon -= 1000000;
            }

            var item = items.Where(item => item.Icon == itemIcon && item.Name.RawString == itemName).ToList();
            if (item.Any())
            {
                this.Entries.Add(new(item[0], itemPrice, isHQ));
            }
        }
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.GetRetainerSellList);
    }
}
