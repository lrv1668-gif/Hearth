using System.Net;
using System.Net.Http.Json;
using Display.Records;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ServiceDefaults;

namespace Display.Tests;

/// <summary>/display/palette — the frame's Color ↔ B&amp;W mode.</summary>
public sealed class PaletteEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PaletteEndpointTests()
    {
        _factory = HearthApp.Create<Program>("display", _db, host => host.UseSetting("TZ", "Etc/UTC"));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    private async Task<FramePalette> Palette() =>
        (await _client.GetFromJsonAsync<FramePalette>("/display/palette", HearthJson.SnakeCaseLower))!;

    [Fact]
    public async Task GetPalette_NothingSaved_ReturnsColor()
    {
        Assert.Equal(FramePalette.Default, await Palette());
    }

    [Fact]
    public async Task PutPalette_BlackAndWhite_IsSaved()
    {
        var response = await _client.PutAsJsonAsync("/display/palette", new { palette = "bw" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FramePalette(FramePalette.BlackAndWhite), await Palette());
    }

    [Fact]
    public async Task PutPalette_Unknown_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync("/display/palette", new { palette = "sepia" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(FramePalette.Default, await Palette());
    }

    [Fact]
    public async Task TogglePalette_Twice_FlipsAndFlipsBack()
    {
        var first = await (await _client.PostAsync("/display/palette/toggle", null))
            .Content.ReadFromJsonAsync<FramePalette>(HearthJson.SnakeCaseLower);
        Assert.Equal(FramePalette.BlackAndWhite, first!.Palette);

        await _client.PostAsync("/display/palette/toggle", null);
        Assert.Equal(FramePalette.Default, await Palette());
    }
}
