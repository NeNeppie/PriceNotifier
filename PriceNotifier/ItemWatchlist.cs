using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier;

public record WatchlistEntry(Item Item, int ThresholdPrice, ItemWatchlistFlags Flags)
{
    public int ThresholdPrice { get; set; } = ThresholdPrice; //  0xE049
    public int FetchedPrice { get; set; } = 0;
    public ItemWatchlistFlags Flags { get; set; } = Flags;
    public bool Updated { get; set; } = false;

    public void Update(int price)
    {
        this.FetchedPrice = price;
        this.Updated = true;
    }
}

// TODO: Hook a "Put Item For Sale" function or "Update Price" function. Also hook "Remove Item" by the same logic
public class ItemWatchlist
{
    public Dictionary<uint, WatchlistEntry> Entries = new();

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
            var itemFlags = ItemWatchlistFlags.Retainer;
            var isHQ = itemIcon > 1000000;

            // HQ Check. Collectables are 500,000.
            if (isHQ)
            {
                itemName = itemName.Remove(itemName.Length - 2);
                itemIcon -= 1000000;
                itemFlags |= ItemWatchlistFlags.HighQuality;
            }

            var item = items.Where(item => item.Icon == itemIcon && item.Name.RawString == itemName).FirstOrDefault();
            if (item is null) { continue; }

            if (!this.Entries.TryGetValue(item.RowId, out var entry))
            {
                this.Entries.Add(item.RowId, new(item, itemPrice, itemFlags));
            }
            else
            {
                if (entry.Flags.HasFlag(ItemWatchlistFlags.Retainer))
                    entry.ThresholdPrice = itemPrice;
            }
        }
    }

    public void Clear()
    {
        this.Entries.Clear();
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSellList", this.GetRetainerSellList);
    }
}
