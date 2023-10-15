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
    private static readonly ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    private readonly List<Item> _items;

    private string _itemSearchQuery = "";
    private List<Item> _itemsFiltered;
    private int _intervalMinutes = Service.Config.TimerInterval;

    public ConfigWindow() : base("Price Notifier", _windowFlags)
    {
        this.IsOpen = true; // DEBUG:
        this.Size = new Vector2(900, 450);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(600, 300),
            MaximumSize = new(1500, 750)
        };

        _items = Service.DataManager.GetExcelSheet<Item>()!
            .Where(item => item.ItemSearchCategory.Row != 0).ToList();
        Service.PluginLog.Debug($"Number of items loaded: {_items.Count}");
        _itemsFiltered = _items;
    }

    public override unsafe void Draw()
    {
        var configWidth = ImGui.GetWindowWidth() * 0.33f;

        if (!ImGui.BeginTable("##table-main", 2, ImGuiTableFlags.BordersInnerV, ImGui.GetContentRegionAvail()))
            return;

        ImGui.TableSetupColumn("Settings", ImGuiTableColumnFlags.WidthFixed, configWidth);
        ImGui.TableSetupColumn("Watchlist", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        this.DrawConfigSettings();

        ImGui.TableNextColumn();
        this.DrawItemSelect();

        ImGui.Spacing();
        ImGui.Text("Item Watchlist:");
        var entryCount = $"[{Service.ItemWatchlist.Entries.Count}]";
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(entryCount).X);
        ImGui.Text(entryCount);

        WatchlistTable.DrawTable();

        ImGui.EndTable();
    }

    private void DrawConfigSettings()
    {
        ImGui.Text("Periodic price fetching: ");
        ImGui.SameLine();

        Vector4 textColor = Service.ItemPriceFetcher.IsActive ? new(0f, 1f, 1f, 1f) : new(1f, 0f, 0f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);

        if (ImGui.SmallButton(Service.ItemPriceFetcher.IsActive ? "Enabled" : "Disabled"))
            Service.ItemPriceFetcher.ToggleTimer();

        ImGui.PopStyleColor();

        ImGui.Text("Timer interval:");
        ImGui.SliderInt("Minutes##config-interval", ref _intervalMinutes, 5, 120, null, ImGuiSliderFlags.NoInput);
        if (Service.ItemPriceFetcher.Interval != _intervalMinutes)
            Service.ItemPriceFetcher.Interval = _intervalMinutes;
    }

    private unsafe void DrawItemSelect()
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
                    if (ImGui.Selectable($"{item.Name}") && !Service.ItemWatchlist.Entries.TryGetValue(item.RowId, out _))
                    {
                        lock (Service.ItemWatchlist)
                        {
                            Service.ItemWatchlist.Entries.Add(item.RowId, new(item, 0, false));
                        }
                    }
                }
            }
            clipper.End();
            ImGui.EndListBox();
        }

        ImGui.PopItemWidth();
    }
}
