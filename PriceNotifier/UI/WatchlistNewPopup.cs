using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier.UI;

public static class WatchlistNewPopup
{
    private static readonly List<Item> _items;
    private static string _itemSearchQuery = "";
    private static List<Item> _itemsFiltered;

    static WatchlistNewPopup()
    {
        _items = Service.DataManager.GetExcelSheet<Item>()!
            .Where(item => item.ItemSearchCategory.Row != 0).ToList();
        Service.PluginLog.Debug($"Number of items loaded: {_items.Count}");
        _itemsFiltered = _items;
    }

    public static unsafe void Draw(string label)
    {
        if (ImGui.BeginPopup(label))
        {
            if (ImGui.InputTextWithHint("##item-search", "Item Name...", ref _itemSearchQuery, 50))
                _itemsFiltered = _items.Where(item => item.Name.ToString().ToLower().Contains(_itemSearchQuery.ToLower())).ToList();

            if (ImGui.BeginListBox("##item-selectlist", new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 10f)))
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

            ImGui.EndPopup();
        }
    }
}
