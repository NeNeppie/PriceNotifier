using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

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

    public int Interval
    {
        get => Service.Config.TimerInterval;
        set
        {
            Service.Config.TimerInterval = value;
            if (this.Timer != null)
            {
                var interval = value * 60000;
                if (interval > 0)
                    this.Timer.Interval = interval;
            }
        }
    }

    public bool IsActive => this.Timer != null;

    public ItemPriceFetcher()
    {
        if (this.Interval <= 0)
            this.Interval = 30;

        this.Timer = new Timer(this.Interval * 60000);
        this.Timer.Elapsed += this.FetchPricesAll;
        this.Timer.AutoReset = true;
        this.Timer.Start();
    }

    private void FetchPricesAll(object? s, ElapsedEventArgs e)
    {
        var taskList = new List<Task>();

        foreach (var item in ConfigWindow.WatchList)
        {
            //taskList.Add(this.FetchPrices(item.RowId, item.Name, "Phoenix")); // TEMP:
            taskList.Add(this.DebugFetch(item.Name.RawString));
        }
        Task.WaitAll(taskList.ToArray());
    }

    // TODO: Put guards, checks, and all that other stuff. Wrap function in try clause, JsonSerializer seems to act up rarely.
    //       Better handling of logging and printing to chat, plus item linking
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
            Service.PluginLog.Debug($"{listing.pricePerUnit} by {listing.retainerName}"); // DEBUG:
            if (retainers.Contains(listing.retainerName))
                break;

            var chatMessage = $"[PriceNotifier] Found a lower price for '{itemName}' - {listing.pricePerUnit} by {listing.retainerName}";
            if (listing.hq)
                chatMessage += " (HQ)";

            Service.ChatGui.Print(new() { Message = chatMessage, Type = Dalamud.Game.Text.XivChatType.Echo });
        }
    }

    private Task DebugFetch(string itemName)
    {
        Service.PluginLog.Debug($"Beep! {itemName}");
        return Task.CompletedTask;
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
