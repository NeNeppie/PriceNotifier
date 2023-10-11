using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace PriceNotifier;

public class Query
{
    public List<Listing> listings { get; set; } = new();

    public class Listing
    {
        public int pricePerUnit { get; set; }
        public bool hq { get; set; }
        public string retainerName { get; set; } = "";
    }
}

public class ItemPriceFetcher : IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly Timer? _timer;

    private const string _universalisFields = "listings.pricePerUnit,listings.hq,listings.retainerName";

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

    public bool IsActive => _timer != null;

    public ItemPriceFetcher()
    {
        if (this.Interval <= 0)
            this.Interval = 30;

        _timer = new Timer(this.Interval * 60000);
        _timer.Elapsed += this.FetchPricesAll;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void FetchPricesAll(object? s, ElapsedEventArgs e)
    {
        var taskList = new List<Task>();

        foreach (var entry in Service.ItemWatchlist.Entries.Values)
        {
            //taskList.Add(this.FetchPrices(item.RowId, item.Name, "Phoenix")); // TEMP:
            taskList.Add(DebugFetch(entry.Item.Name.RawString));
        }
        Task.WaitAll(taskList.ToArray());
    }

    // TODO: Better chat printing, plus item linking. Player-made chat notification?
    //       Multple fetches per api request, see PriceInsight and Universalis doc.
    public async Task FetchPricesAsync(WatchlistEntry entry, string region, bool ignoreTax = false, bool sameQuality = false)
    {
        var url = $"https://universalis.app/api/v2/{region}/{entry.Item.RowId}?noGst={ignoreTax}&fields={_universalisFields}";
        try
        {
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unsuccessful status code: {response.StatusCode}", null, response.StatusCode);

            var responseStream = await response.Content.ReadAsStreamAsync();
            var query = await JsonSerializer.DeserializeAsync<Query>(responseStream)
                ?? throw new HttpRequestException("Returned null response");

            Query.Listing? cheapestListing = null;
            foreach (var listing in query.listings)
            {
                if (listing.pricePerUnit >= entry.Price && entry.Price != 0)
                    break;

                if (sameQuality && listing.hq != entry.HQ)
                    continue;

                if (cheapestListing is null || listing.pricePerUnit < cheapestListing.pricePerUnit)
                    cheapestListing = listing;
            }
            if (cheapestListing is not null)
            {
                var chatMessage = $"[PriceNotifier] Found lower price for '{entry.Item.Name}{(cheapestListing.hq ? " \xE03C" : "")}'" +
                                  $" - {cheapestListing.pricePerUnit}\xE049 by {cheapestListing.retainerName}";
                Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
            }
        }
        catch (Exception e)
        {
            Service.PluginLog.Error(e, $"Couldn't retrieve data for '{entry.Item.Name}' (id {entry.Item.RowId}) at region {region}");
        }
    }

    private static Task DebugFetch(string itemName)
    {
        Service.PluginLog.Debug($"Beep! {itemName}");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= this.FetchPricesAll;
            _timer.Dispose();
        }
    }
}
