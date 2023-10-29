using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier.UI;

public static class GuiUtilities
{
    public static float ItemIconSize => (ImGui.GetTextLineHeight() * 1.5f) + ImGui.GetStyle().ItemSpacing.Y;
    public static float FlagIconSize => ImGui.GetTextLineHeight() * 1.5f;

    private static readonly Dictionary<string, IDalamudTextureWrap?> _imageCache = new();

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

    public static void DrawItemIcon(Item item, bool isHQ, Vector2 size)
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

    public static void DrawFlagIcon(uint id, bool flagOn, Vector2 size)
    {
        var icon = Service.TextureProvider.GetIcon(id);
        var alpha = flagOn ? 1f : 0.3f;

        if (icon is not null)
        {
            ImGui.Image(icon.ImGuiHandle, size, Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, alpha));
        }
    }

    public static void DrawFlagIcon(string image, bool flagOn, Vector2 size)
    {
        var icon = LoadImage(image);
        var alpha = flagOn ? 1f : 0.3f;

        if (icon is not null)
        {
            ImGui.Image(icon.ImGuiHandle, size, Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, alpha));
        }
    }

    private static IDalamudTextureWrap? LoadImage(string image)
    {
        if (_imageCache.TryGetValue(image, out var ret))
            return ret;

        var imagePath = Path.Combine(Service.PluginInterface.AssemblyLocation.DirectoryName!, $@"Images\{image}.png");
        var icon = Service.TextureProvider.GetTextureFromFile(new FileInfo(imagePath));

        if (icon is null)
        {
            Service.PluginLog.Error($"Failed to load image {imagePath}");
        }

        _imageCache[image] = icon;
        return icon;
    }
}
