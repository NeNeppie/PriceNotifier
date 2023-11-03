using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace PriceNotifier.UI;

public class ConfigWindow : Window
{
    private static readonly ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
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

        var label = "Fetch All";
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X);
        if (ImGui.SmallButton(label))
        {
            Task.Run(Service.ItemPriceFetcher.FetchWatchlistPrices);
        }

        ImGui.Text("Timer interval:");
        ImGui.SliderInt("Minutes##config-interval", ref _intervalMinutes, 5, 120, null, ImGuiSliderFlags.NoInput);
        if (Service.ItemPriceFetcher.Interval != _intervalMinutes)
            Service.ItemPriceFetcher.Interval = _intervalMinutes;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("Only fetch items with the same quality", ref Service.Config.FetchingSameQuality);

        ImGui.TextWrapped("Send individual notifications per item when below...");
        ImGui.SliderInt("Notifications##config-spam", ref Service.Config.FetchingSpamLimit, 1, 10);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("Clear update flash by hovering", ref Service.Config.ClearFlashOnHover);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("When off, clicking the item clears the flash");
        }

        ImGui.SetCursorPosY(ImGui.GetContentRegionAvail().Y + ImGui.GetCursorPosY() - ImGui.GetTextLineHeightWithSpacing());
        if (GuiUtilities.ColoredButton("Clear Watchlist", new Vector4(0.78f, 0.33f, 0.33f, 0.7f)))
        {
            Service.ItemWatchlist.Clear();
        }
    }
}
