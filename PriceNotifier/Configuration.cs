using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace PriceNotifier
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public int TimerInterval = 30;
        public int FetchingSpamLimit = 3;
        public bool FetchingSameQuality = true;
        public Dictionary<uint, (int, int, bool)> WatchlistData = new();

        [NonSerialized]
        private DalamudPluginInterface? _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;

            var items = Service.DataManager.GetExcelSheet<Item>()!.ToList();
            foreach (var watchlistItem in this.WatchlistData)
            {
                var (itemThreshPrice, itemFetchedPrice, itemHQ) = watchlistItem.Value;
                var item = items.Where(item => item.RowId == watchlistItem.Key).FirstOrDefault();
                if (item is not null)
                {
                    Service.ItemWatchlist.Entries[watchlistItem.Key] = new(item, itemThreshPrice, itemHQ);
                    Service.ItemWatchlist.Entries[watchlistItem.Key].FetchedPrice = itemFetchedPrice;
                }
            }
        }

        public void Save()
        {
            this.WatchlistData.Clear();
            foreach (var entry in Service.ItemWatchlist.Entries)
            {
                var entryData = entry.Value;
                this.WatchlistData[entry.Key] = (entryData.ThresholdPrice, entryData.FetchedPrice, entryData.HQ);
            }

            _pluginInterface!.SavePluginConfig(this);
        }
    }
}
