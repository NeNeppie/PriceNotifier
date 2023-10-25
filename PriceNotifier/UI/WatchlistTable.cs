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
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##Icon", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, _iconSize);
        ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch, 100f); // Not consistent, but what can you do about it.
        ImGui.TableSetupColumn("Threshold Price", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Lowest Price", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("##Flags", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, _iconSize * 2f);

        ImGui.TableHeadersRow();
    }

    // TODO: Optimize draw time
    public static void DrawTable()
    {
        if (!ImGui.BeginTable("##item-watchlist-table", 5, _tableFlags, ImGui.GetContentRegionAvail())) { return; }

        DrawTableHeader();

        foreach (var entry in Service.ItemWatchlist.Entries.Reverse())
        {
            ImGui.TableNextRow();
            var value = entry.Value;

            if (value.Updated)
            {
                var rowBgColor = ImGui.GetColorU32(new Vector4(1f, 1f, 0.5f, 0.1f));
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, rowBgColor);
            }

            // Icon Column
            ImGui.TableNextColumn();
            DrawIcon(value.Item, value.Flags.HasFlag(ItemWatchlistFlags.HighQuality), new Vector2(_iconSize));

            // Name Column
            ImGui.TableNextColumn();
            ImGui.Text(value.Item.Name);

            // Threshold Price Column
            ImGui.TableNextColumn();
            var priceTag = value.ThresholdPrice;
            var priceInputWidth = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("\xE049").X - ImGui.GetStyle().ItemSpacing.X;

            ImGui.PushItemWidth(priceInputWidth);
            if (ImGui.InputInt($"\xE049##item-watchlist-price-rename-{entry.Key}", ref priceTag, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.ThresholdPrice = priceTag;
            }
            ImGui.PopItemWidth();

            // Lowest Price Column Column
            ImGui.TableNextColumn();
            ImGui.Text($"{value.FetchedPrice}\xE049");

            // Flags Column
            ImGui.TableNextColumn();
            value.Flags.DrawFlags();

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
        if (ImGui.IsItemHovered())
            entry.Updated = false;
        if (ImGui.IsItemClicked())
            openPopup = true;

        if (openPopup)
            ImGui.OpenPopup($"##watchlist-item-popup-{id}");

        if (ImGui.BeginPopup($"##watchlist-item-popup-{id}"))
        {
            ImGui.Text(entry.Item.Name);
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Selectable("Fetch Price"))
            {
                var region = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.RowId.ToString();
                if (region is null)
                {
                    Service.PluginLog.Error("Error in manual price fetch: Home World is null");
                    ImGui.EndPopup();
                    return;
                }

                Task.Run(() => Service.ItemPriceFetcher.FetchPricesAsync(entry, region, true, Service.Config.FetchingSameQuality));
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
