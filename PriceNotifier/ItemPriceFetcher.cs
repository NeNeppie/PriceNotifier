using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Dalamud.Logging;

using PriceNotifier.UI;

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
    private readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly Timer? Timer;

    private static TimeSpan Interval => TimeSpan.FromMinutes(Service.Config.TimerInterval); // TODO: Config setting & setter
    public bool IsActive => this.Timer != null;

    public ItemPriceFetcher()
    {
        this.Timer = new Timer(Interval);
        this.Timer.Elapsed += this.FetchPricesAll;
        this.Timer.AutoReset = true;
        this.Timer.Start();
    }

    private void FetchPricesAll(object? s, ElapsedEventArgs e)
    {
        var taskList = new List<Task>();

        foreach (var item in ConfigWindow.WatchList)
        {
            taskList.Add(this.FetchPrices(item.RowId, item.Name, "Phoenix")); // TEMP:
        }
        Task.WaitAll(taskList.ToArray());
    }

    // TODO: Put guards, checks, and all that other stuff. Wrap function in try clause, JsonSerializer seems to act up rarely.
    public async Task FetchPrices(uint itemID, string itemName, string region)
    {
        var url = $"https://universalis.app/api/v2/{region}/{itemID}?fields=listings.pricePerUnit,listings.hq,listings.retainerName"; // TEMP:
        string[] retainers = { "Transmongold", "Sebastibun" };  // TEMP:

        var response = await this.Client.GetAsync(url);
        var responseStream = await response.Content.ReadAsStreamAsync();
        var query = await JsonSerializer.DeserializeAsync<Query>(responseStream)
            ?? throw new HttpRequestException("Well shit");

        foreach (var listing in query.listings)
        {
            PluginLog.Debug($"{listing.pricePerUnit} by {listing.retainerName}"); // DEBUG:
            if (retainers.Contains(listing.retainerName))
                break;

            var chatMessage = $"[PriceNotifier] Found a lower price for '{itemName}' - {listing.pricePerUnit} by {listing.retainerName}";
            if (listing.hq)
                chatMessage += " (HQ)";

            Service.ChatGui.PrintChat(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
        }
    }

    public void Dispose()
    {
        if (this.Timer != null)
        {
            this.Timer.Stop();
            this.Timer.Elapsed -= this.FetchPricesAll;
            this.Timer.Dispose();
        }
    }
}
