using System;
using System.Collections.Generic;
using ImGuiNET;

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
    public static Dictionary<ItemWatchlistFlags, Action> NoName = new()
    {
        { ItemWatchlistFlags.Retainer, DrawRetainerFlag },
        { ItemWatchlistFlags.HighQuality, DrawHighQualityFlag },
        { ItemWatchlistFlags.DisableFetching, DrawDisableFetchingFlag }
    };

    public static void DrawFlags(this ItemWatchlistFlags flags)
    {
        foreach (var value in Enum.GetValues<ItemWatchlistFlags>())
        {
            if (value is ItemWatchlistFlags.None) { continue; }

            if (flags.HasFlag(value))
                NoName[value]();
            ImGui.SameLine();
        }
        ImGui.Text("");
    }

    private static void DrawRetainerFlag()
    {
        ImGui.Text("R");
    }

    private static void DrawHighQualityFlag()
    {
        ImGui.Text("H");
    }

    private static void DrawDisableFetchingFlag()
    {
        ImGui.Text("D");
    }
}
