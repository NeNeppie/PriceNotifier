using System.Numerics;
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

        ImGui.Text("Timer interval:");
        ImGui.SliderInt("Minutes##config-interval", ref _intervalMinutes, 5, 120, null, ImGuiSliderFlags.NoInput);
        if (Service.ItemPriceFetcher.Interval != _intervalMinutes)
            Service.ItemPriceFetcher.Interval = _intervalMinutes;
    }
}
