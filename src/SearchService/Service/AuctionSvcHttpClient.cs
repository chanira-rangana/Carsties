using MongoDB.Entities;

namespace SearchService;

public class AuctionSvcHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<Item>> GetItemsForSearchDb()
    {
        // await => awaited without blocking the thread
        var lastUpdated = await DB.Find<Item, string>() // second generic parameter is projected result
            .Sort(x => x.Descending(x => x.UpdatedAt)) // descending all documents as most recent first
            .Project(x => x.UpdatedAt.ToString()) // 
            .ExecuteFirstAsync();

        return await _httpClient.GetFromJsonAsync<List<Item>>(_config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
    }
}
