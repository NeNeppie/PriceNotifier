using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier.UI;

public class ConfigWindow : Window, IDisposable
{
    private string ItemSearchQuery = "";
    private readonly List<Item> Items;
    private List<Item> ItemsFiltered;
    // TODO: Refactor. RACE CONDITIONS!
    public static HashSet<Item> WatchList = new();
    private Item? SelectedItem = null;
    private int IntervalMinutes = Service.Config.TimerInterval;

    private ItemPriceFetcher ItemPriceFetcher = new();

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

        var itemsheet = Service.DataManager.GetExcelSheet<Item>();

        this.Items = Service.DataManager.GetExcelSheet<Item>()!
            .Where(item => item.ItemSearchCategory.Row != 0).ToList();
        Service.PluginLog.Debug($"Number of items loaded: {this.Items.Count}");
        this.ItemsFiltered = this.Items;
    }

    public override unsafe void Draw()
    {
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);

        if (ImGui.InputTextWithHint("##item-search", "Item Name...", ref this.ItemSearchQuery, 50))
            this.ItemsFiltered = this.Items.Where(item => item.Name.ToString().ToLower().Contains(this.ItemSearchQuery.ToLower())).ToList();

        if (ImGui.BeginListBox("##item-selectlist", new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5f)))
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            clipper.Begin(this.ItemsFiltered.Count);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var item = this.ItemsFiltered[i];
                    if (ImGui.Selectable($"{item.Name}"))
                    {
                        lock (WatchList)
                        {
                            WatchList.Add(item);
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
            foreach (var item in WatchList.Reverse())
            {
                if (ImGui.Selectable(item.Name))
                {
                    this.SelectedItem = item;
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
                    Task.Run(() => this.ItemPriceFetcher.FetchPrices(this.SelectedItem!.RowId, this.SelectedItem.Name, region));
            }

            if (ImGui.Selectable("Remove From Watchlist"))
            {
                lock (WatchList)
                {
                    WatchList.Remove(this.SelectedItem!);
                }
            }

            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.Text("Timer interval:");

        // FIXME: Performance
        ImGui.SliderInt("Minutes##config-interval", ref this.IntervalMinutes, 1, 120);
        if (this.ItemPriceFetcher.Interval != this.IntervalMinutes)
            this.ItemPriceFetcher.Interval = this.IntervalMinutes;
    }

    public void Dispose()
    {
        this.ItemPriceFetcher.Dispose();
    }
}
