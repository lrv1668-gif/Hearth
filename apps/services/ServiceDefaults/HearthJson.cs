using System.Text.Json;

namespace ServiceDefaults;

public static class HearthJson
{
    public static readonly JsonSerializerOptions SnakeCaseLower = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
