using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace PriceNotifier;

public class UniversalisDataMulti
{
    public Dictionary<uint, ItemData> items { get; set; } = new();
}

public class ItemData
{
    public List<Listing> listings { get; set; } = new();

    public class Listing
    {
        public int pricePerUnit { get; set; }
        public bool hq { get; set; }
        public string retainerName { get; set; } = "";
    }
}

public record MarketboardInfo(ItemData.Listing Listing, string ItemName);

// TODO: Better chat printing, plus item linking. Player-made chat notification?
public class ItemPriceFetcher : IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly Timer? _timer;

    private const string _universalisFields = "listings.pricePerUnit,listings.hq,listings.retainerName";
    private const string _universalisFieldsMulti = "items.listings.pricePerUnit,items.listings.hq,items.listings.retainerName";

    public int Interval
    {
        get => Service.Config.TimerInterval;
        set
        {
            Service.Config.TimerInterval = value;
            if (_timer != null)
            {
                // minutes -> milliseconds
                var interval = value * 60000;
                if (interval > 0)
                    _timer.Interval = interval;
            }
        }
    }

    public bool IsActive => _timer != null && _timer.Enabled;

    public ItemPriceFetcher()
    {
        if (this.Interval <= 0)
            this.Interval = 30;

        // minutes -> milliseconds
        _timer = new Timer(this.Interval * 60000);
        _timer.Elapsed += this.FetchWatchlistPrices;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void FetchWatchlistPrices(object? s, ElapsedEventArgs e)
    {
        var region = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.RowId.ToString();
        if (region is null)
        {
            Service.PluginLog.Error("Error in automatic price fetch: Home World is null");
            return;
        }

        var taskList = new List<Task<List<MarketboardInfo>?>>();
        var listings = new List<MarketboardInfo>();
        var config = Service.Config;

        var queue = new Dictionary<uint, WatchlistEntry>();
        foreach (var entry in Service.ItemWatchlist.Entries)
        {
            if (entry.Value.Flags.HasFlag(ItemWatchlistFlags.DisableFetching)) { continue; }

            queue[entry.Key] = entry.Value;
            if (queue.Count >= 10)
            {
                taskList.Add(this.FetchPricesMultiAsync(new(queue), region, true, config.FetchingSameQuality));
                queue.Clear();
            }
        }
        if (queue.Any())
        {
            taskList.Add(this.FetchPricesMultiAsync(queue, region, true, config.FetchingSameQuality));
        }

        Task.WaitAll(taskList.ToArray());

        foreach (var task in taskList)
        {
            if (task.Result is null) { continue; }
            listings = listings.Concat(task.Result).ToList();
        }

        if (listings.Count > config.FetchingSpamLimit)
        {
            var chatMessage = $"[PriceNotifier] Found lower prices for {listings.Count} items. See `/pricenotifier` for more info";
            Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
        }
        else
        {
            foreach (var (itemData, itemName) in listings)
            {
                var chatMessage = $"[PriceNotifier] Found lower price for '{itemName}{(itemData.hq ? " \xE03C" : "")}'" +
                              $" - {itemData.pricePerUnit}\xE049 by {itemData.retainerName}";
                Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
            }
        }
    }

    public async Task<List<MarketboardInfo>?> FetchPricesMultiAsync(Dictionary<uint, WatchlistEntry> entries, string region, bool ignoreTax = false, bool sameQuality = false)
    {
        if (entries.Count == 1)
        {
            if (await this.FetchPricesAsync(entries.First().Value, region, ignoreTax, sameQuality) is { } data)
            {
                return new() { data };
            }
            return null;
        }

        var itemIDs = string.Join(',', entries.Select(x => x.Key));
        var url = $"https://universalis.app/api/v2/{region}/{itemIDs}?noGst={ignoreTax}&fields={_universalisFieldsMulti}";
        try
        {
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unsuccessful status code: {response.StatusCode}", null, response.StatusCode);

            var responseStream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<UniversalisDataMulti>(responseStream)
                ?? throw new HttpRequestException("Returned null response");

            var listings = new List<MarketboardInfo>();
            foreach (var item in data.items)
            {
                if (!entries.TryGetValue(item.Key, out var entry)) { continue; }
                var itemData = item.Value;

                if (GetListing(itemData.listings, entry, sameQuality) is { } listing)
                    listings.Add(new(listing, entry.Item.Name));
            }

            return listings;
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, $"Couldn't retrieve data for {entries.Count} items, region {region}");
        }
        return null;
    }

    public async Task<MarketboardInfo?> FetchPricesAsync(WatchlistEntry entry, string region, bool ignoreTax = false, bool sameQuality = false)
    {
        var url = $"https://universalis.app/api/v2/{region}/{entry.Item.RowId}?noGst={ignoreTax}&fields={_universalisFields}";
        try
        {
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unsuccessful status code: {response.StatusCode}", null, response.StatusCode);

            var responseStream = await response.Content.ReadAsStreamAsync();
            var itemData = await JsonSerializer.DeserializeAsync<ItemData>(responseStream)
                ?? throw new HttpRequestException("Returned null response");

            if (GetListing(itemData.listings, entry, sameQuality) is { } listing)
            {
                return new(listing, entry.Item.Name.ToString());
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, $"Couldn't retrieve data for '{entry.Item.Name}' (id {entry.Item.RowId}), region {region}");
        }
        return null;
    }

    private static ItemData.Listing? GetListing(List<ItemData.Listing> listings, WatchlistEntry entry, bool sameQuality)
    {
        if (sameQuality)
            listings = listings.Where(listing => listing.hq == entry.Flags.HasFlag(ItemWatchlistFlags.HighQuality)).ToList();

        if (listings.Any())
        {
            var cheapestListing = listings[0];
            if (entry.FetchedPrice != cheapestListing.pricePerUnit)
            {
                entry.Update(cheapestListing.pricePerUnit);
            }
            if (cheapestListing.pricePerUnit < entry.ThresholdPrice || entry.ThresholdPrice == 0)
            {
                return cheapestListing;
            }
        }
        return null;
    }

    public void ToggleTimer()
    {
        if (_timer != null)
        {
            _timer.Enabled ^= true;
            Service.PluginLog.Info($"Periodic item fetching set to '{_timer.Enabled}'");
        }
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= this.FetchWatchlistPrices;
            _timer.Dispose();
        }
    }
}
