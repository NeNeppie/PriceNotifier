using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace PriceNotifier;

public class UniversalisResponse
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

        _timer = new Timer(this.Interval * 60000);
        _timer.Elapsed += this.FetchWatchlistPrices;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void FetchWatchlistPrices(object? s, ElapsedEventArgs e)
    {
        var taskList = new List<Task>();
        var region = Service.ClientState.LocalPlayer?.HomeWorld.GameData?.RowId.ToString();
        if (region is null)
        {
            Service.PluginLog.Error("Error in automatic price fetch: Home World is null");
            return;
        }

        var queue = new Dictionary<uint, WatchlistEntry>();
        foreach (var entry in Service.ItemWatchlist.Entries)
        {
            queue[entry.Key] = entry.Value;
            if (queue.Count >= 10)
            {
                taskList.Add(this.FetchPricesMultiAsync(queue, region, true, true));
                queue.Clear();
            }
        }
        if (queue.Any())
        {
            taskList.Add(this.FetchPricesMultiAsync(queue, region, true, true));
        }

        Task.WaitAll(taskList.ToArray());
    }

    public async Task FetchPricesMultiAsync(Dictionary<uint, WatchlistEntry> entries, string region, bool ignoreTax = false, bool sameQuality = false)
    {
        var itemIDs = string.Join(',', entries.Select(x => x.Key));
        var url = $"https://universalis.app/api/v2/{region}/{itemIDs}?noGst={ignoreTax}&fields={_universalisFieldsMulti}";
        try
        {
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unsuccessful status code: {response.StatusCode}", null, response.StatusCode);

            var responseStream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<UniversalisResponse>(responseStream)
                ?? throw new HttpRequestException("Returned null response");

            foreach (var item in data.items)
            {
                if (!entries.TryGetValue(item.Key, out var entry)) { continue; }
                var itemData = item.Value;

                var cheapestListing = GetCheapestListing(itemData.listings, entry, sameQuality);
                if (cheapestListing is not null)
                {
                    var chatMessage = $"[PriceNotifier] Found lower price for '{entry.Item.Name}{(cheapestListing.hq ? " \xE03C" : "")}'" +
                                      $" - {cheapestListing.pricePerUnit}\xE049 by {cheapestListing.retainerName}";
                    Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
                }
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, $"Couldn't retrieve data for {entries.Count} items, region {region}");
        }
    }

    public async Task FetchPricesAsync(WatchlistEntry entry, string region, bool ignoreTax = false, bool sameQuality = false)
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

            var cheapestListing = GetCheapestListing(itemData.listings, entry, sameQuality);
            if (cheapestListing is not null)
            {
                var chatMessage = $"[PriceNotifier] Found lower price for '{entry.Item.Name}{(cheapestListing.hq ? " \xE03C" : "")}'" +
                                  $" - {cheapestListing.pricePerUnit}\xE049 by {cheapestListing.retainerName}";
                Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, $"Couldn't retrieve data for '{entry.Item.Name}' (id {entry.Item.RowId}), region {region}");
        }
    }

    private static ItemData.Listing? GetCheapestListing(List<ItemData.Listing> listings, WatchlistEntry entry, bool sameQuality)
    {
        ItemData.Listing? cheapestListing = null;
        foreach (var listing in listings)
        {
            if (listing.pricePerUnit >= entry.Price && entry.Price != 0)
                break;

            if (sameQuality && listing.hq != entry.HQ)
                continue;

            if (cheapestListing is null || listing.pricePerUnit < cheapestListing.pricePerUnit)
                cheapestListing = listing;
        }
        return cheapestListing;
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
