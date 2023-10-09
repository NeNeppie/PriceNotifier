using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier.UI;

public class ConfigWindow : Window
{
    private readonly List<Item> _items;

    private string _itemSearchQuery = "";
    private List<Item> _itemsFiltered;
    private WatchlistEntry _selectedEntry = new(new(), 0, false);
    private int _intervalMinutes = Service.Config.TimerInterval;

    public ConfigWindow() : base("PriceNotifier##config")
    {
        this.IsOpen = true; // DEBUG:
        this.Size = new Vector2(200, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(200, 75),
            MaximumSize = new(500, 500)
        };

        _items = Service.DataManager.GetExcelSheet<Item>()!
            .Where(item => item.ItemSearchCategory.Row != 0).ToList();
        Service.PluginLog.Debug($"Number of items loaded: {_items.Count}");
        _itemsFiltered = _items;
    }

    public override unsafe void Draw()
    {
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);

        if (ImGui.InputTextWithHint("##item-search", "Item Name...", ref _itemSearchQuery, 50))
            _itemsFiltered = _items.Where(item => item.Name.ToString().ToLower().Contains(_itemSearchQuery.ToLower())).ToList();

        if (ImGui.BeginListBox("##item-selectlist", new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5f)))
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            clipper.Begin(_itemsFiltered.Count);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var item = _itemsFiltered[i];
                    if (ImGui.Selectable($"{item.Name}"))
                    {
                        lock (Service.ItemWatchlist)
                        {
                            Service.ItemWatchlist.Entries.Add(new(item, 0, false));
                        }
                    }
                }
            }
            clipper.End();
            ImGui.EndListBox();
        }

        ImGui.Spacing();
        ImGui.Text("Item Watchlist:");

        bool openPopup = false;
        if (ImGui.BeginListBox("##item-watchlist", new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5f)))
        {
            foreach (var entry in Service.ItemWatchlist.Entries.Reverse())
            {
                if (ImGui.Selectable(entry.Item.Name))
                {
                    _selectedEntry = entry;
                    openPopup = true;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.PopItemWidth();

        if (openPopup)
        {
            ImGui.OpenPopup("##item-watchlist-popup");
        }
        if (ImGui.BeginPopup("##item-watchlist-popup"))
        {
            if (ImGui.Selectable("Fetch Price"))
            {
                var region = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.RowId.ToString();
                if (region is not null)
                    Task.Run(() => Service.ItemPriceFetcher.FetchPricesAsync(_selectedEntry.Item.RowId, _selectedEntry.Item.Name, region));
            }

            if (ImGui.Selectable("Remove From Watchlist"))
            {
                lock (Service.ItemWatchlist.Entries)
                {
                    Service.ItemWatchlist.Entries.Remove(_selectedEntry);
                }
            }

            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.Text("Timer interval:");

        ImGui.SliderInt("Minutes##config-interval", ref _intervalMinutes, 1, 120, null, ImGuiSliderFlags.NoInput);
        if (Service.ItemPriceFetcher.Interval != _intervalMinutes)
            Service.ItemPriceFetcher.Interval = _intervalMinutes;
    }
}
