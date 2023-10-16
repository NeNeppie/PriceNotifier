using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier.UI;

internal static class WatchlistTable
{
    private static readonly ImGuiTableFlags _tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg;
    private static float _iconSize => (ImGui.GetTextLineHeight() * 1.5f) + ImGui.GetStyle().ItemSpacing.Y;

    private static void DrawTableHeader()
    {
        ImGui.TableSetupColumn("##Icon", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, _iconSize);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 100f); // Not consistent, but what can you do about it.
        ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("HQ", ImGuiTableColumnFlags.WidthStretch, 100f);

        ImGui.TableHeadersRow();
    }

    // TODO: Optimize draw time
    public static void DrawTable()
    {
        if (!ImGui.BeginTable("##item-watchlist-table", 4, _tableFlags, ImGui.GetContentRegionAvail())) { return; }

        DrawTableHeader();

        foreach (var entry in Service.ItemWatchlist.Entries.Reverse())
        {
            ImGui.TableNextRow();
            var value = entry.Value;

            // Icon Column
            ImGui.TableNextColumn();
            DrawIcon(value.Item, value.HQ, new Vector2(_iconSize));

            // Name Column
            ImGui.TableNextColumn();
            ImGui.Text(value.Item.Name);

            // Price Column
            ImGui.TableNextColumn();
            var priceTag = value.Price;
            var priceInputWidth = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("\xE049").X - ImGui.GetStyle().ItemSpacing.X;

            ImGui.PushItemWidth(priceInputWidth);
            if (ImGui.InputInt($"\xE049##item-watchlist-price-rename-{entry.Key}", ref priceTag, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.Price = priceTag;
            }
            ImGui.PopItemWidth();

            // HQ Column
            ImGui.TableNextColumn();
            ImGui.Text($"{value.HQ}");

            // Row Popup Selectable
            ImGui.SameLine();
            DrawItemPopup(value, entry.Key);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawNewPopup();

        ImGui.EndTable();
    }

    private static void DrawItemPopup(WatchlistEntry entry, uint id)
    {
        var openPopup = false;
        ImGui.Selectable("", false, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, ImGui.GetTextLineHeight() * 1.5f));
        if (ImGui.IsItemClicked())
            openPopup = true;
        if (openPopup)
            ImGui.OpenPopup($"##watchlist-item-popup-{id}");

        if (ImGui.BeginPopup($"##watchlist-item-popup-{id}"))
        {
            if (ImGui.Selectable("Fetch Price"))
            {
                var region = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.RowId.ToString();
                if (region is not null)
                    // TODO: Implement config settings
                    Task.Run(() => Service.ItemPriceFetcher.FetchPricesAsync(entry, region, true, true));
            }

            if (ImGui.Selectable("Remove From Watchlist"))
            {
                lock (Service.ItemWatchlist.Entries)
                {
                    Service.ItemWatchlist.Entries.Remove(id);
                }
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawNewPopup()
    {
        var openPopup = false;
        var popupLabel = "##watchlist-new-popup";

        if (IconButton(FontAwesomeIcon.Plus, new Vector2(_iconSize), "Add items"))
            openPopup = true;
        if (openPopup)
            ImGui.OpenPopup(popupLabel);

        WatchlistNewPopup.Draw(popupLabel);
    }

    public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default, string? tooltip = null)
    {
        var label = icon.ToIconString();

        ImGui.PushFont(UiBuilder.IconFont);
        var res = ImGui.Button(label, size);
        ImGui.PopFont();

        if (tooltip != null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }

        return res;
    }

    public static void DrawIcon(Item item, bool isHQ, Vector2 size)
    {
        var iconFlags = Dalamud.Plugin.Services.ITextureProvider.IconFlags.HiRes;
        if (isHQ)
            iconFlags |= Dalamud.Plugin.Services.ITextureProvider.IconFlags.ItemHighQuality;

        var icon = Service.TextureProvider.GetIcon(item.Icon, iconFlags);
        if (icon is not null)
        {
            ImGui.Image(icon.ImGuiHandle, size);
            ImGui.SameLine();
        }
    }
}
