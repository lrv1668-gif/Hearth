using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Quote.Records;

namespace Quote;

public sealed class QuoteFetcher(HttpClient http)
{
    private record ZenResponse(
        [property: JsonPropertyName("q")] string Q,
        [property: JsonPropertyName("a")] string A);

    public async Task<QuoteItem?> FetchAsync()
    {
        try
        {
            var items = await http.GetFromJsonAsync<ZenResponse[]>("https://zenquotes.io/api/today");
            var first = items?.FirstOrDefault();
            return first is null ? null : new QuoteItem(first.Q, first.A);
        }
        catch
        {
            return null;
        }
    }
}
