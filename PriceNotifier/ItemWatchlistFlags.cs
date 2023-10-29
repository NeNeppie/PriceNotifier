using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

using PriceNotifier.UI;

namespace PriceNotifier;

[Flags]
public enum ItemWatchlistFlags
{
    None            = 0b00000,
    Retainer        = 0b00001,
    HighQuality     = 0b00010, //  0xE03C
    DisableFetching = 0b00100
    // ???          = 0b01000
}

internal static class ItemWatchlistFlagsEx
{
    public static int Count => Enum.GetNames<ItemWatchlistFlags>().Length - 1;

    public static Dictionary<ItemWatchlistFlags, Predicate<bool>> FlagDrawFuncs = new()
    {
        { ItemWatchlistFlags.Retainer, DrawRetainerFlag },
        { ItemWatchlistFlags.HighQuality, DrawHighQualityFlag },
        { ItemWatchlistFlags.DisableFetching, DrawDisableFetchingFlag }
    };

    private static readonly ushort _retainerIconId = 060425;
    private static readonly ushort _disableFetchingIconId = 063938;

    public static ItemWatchlistFlags DrawFlags(this ItemWatchlistFlags flags)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f));
        foreach (var value in Enum.GetValues<ItemWatchlistFlags>())
        {
            if (value is ItemWatchlistFlags.None) { continue; }

            bool active = flags.HasFlag(value);
            if (FlagDrawFuncs[value](active))
            {
                flags ^= value;
            }
            ImGui.SameLine();
        }
        ImGui.Text("");
        ImGui.PopStyleVar();

        return flags;
    }

    private static bool DrawRetainerFlag(bool active)
    {
        GuiUtilities.DrawFlagIcon(_retainerIconId, active, new Vector2(GuiUtilities.FlagIconSize));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("This item is sold by your retainer");
        }

        return false;
    }

    private static bool DrawHighQualityFlag(bool active)
    {
        GuiUtilities.DrawFlagIcon("HighQualityFlagIcon", active, new Vector2(GuiUtilities.FlagIconSize));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle High Quality status");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            return true;
        }
        return false;
    }

    private static bool DrawDisableFetchingFlag(bool active)
    {
        GuiUtilities.DrawFlagIcon(_disableFetchingIconId, active, new Vector2(GuiUtilities.FlagIconSize));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle automatic fetching for this item.\nActive means disabled");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            return true;
        }
        return false;
    }
}
