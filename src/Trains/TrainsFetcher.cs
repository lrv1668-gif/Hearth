using System.Text.Json;
using Trains.Records;

namespace Trains;

public sealed class TrainsFetcher(HttpClient http)
{
    public async Task<StopDepartures> FetchAsync(string apiKey, string stopKey)
    {
        var url = $"https://transit.land/api/v2/rest/stops/{Uri.EscapeDataString(stopKey)}/departures?apikey={apiKey}";

        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        if (!doc.RootElement.TryGetProperty("stops", out var stops) || stops.GetArrayLength() == 0)
            return new StopDepartures(stopKey, null, []);

        var stop = stops[0];
        var stopName = stop.TryGetProperty("stop_name", out var n) ? n.GetString() : null;

        // A parent station (location_type 1) carries no departures of its own — the actual trips are
        // attached to its child stops (individual platforms/gates), so departures must be pulled from
        // both the stop itself and any children and merged.
        var departures = new List<TrainDeparture>(ParseDepartures(stop));
        if (stop.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
                departures.AddRange(ParseDepartures(child));
        }

        return new StopDepartures(stopKey, stopName, departures);
    }

    private static IEnumerable<TrainDeparture> ParseDepartures(JsonElement stopElement)
    {
        if (!stopElement.TryGetProperty("departures", out var departuresEl))
            yield break;

        foreach (var d in departuresEl.EnumerateArray())
        {
            var routeType = -1;
            var routeShortName = "";
            string? routeLongName = null;
            if (d.TryGetProperty("trip", out var trip) && trip.TryGetProperty("route", out var route))
            {
                routeType = route.TryGetProperty("route_type", out var rt) ? rt.GetInt32() : -1;
                routeShortName = route.TryGetProperty("route_short_name", out var rsn) ? rsn.GetString() ?? "" : "";
                routeLongName = route.TryGetProperty("route_long_name", out var rln) ? rln.GetString() : null;
            }

            // GTFS stop_headsign is an optional per-stop override, rarely populated by feeds —
            // trip_headsign (the rider-facing direction/destination for the whole trip, e.g.
            // "Richmond") is what almost every agency actually sets, so fall back to it.
            var headsign = d.TryGetProperty("stop_headsign", out var hs) ? hs.GetString() : null;
            if (string.IsNullOrEmpty(headsign) && trip.ValueKind == JsonValueKind.Object &&
                trip.TryGetProperty("trip_headsign", out var th))
            {
                headsign = th.GetString();
            }
            var scheduled = d.TryGetProperty("departure_time", out var dt) ? dt.GetString()
                : d.TryGetProperty("arrival_time", out var at) ? at.GetString() : null;

            var scheduleRelationship = d.TryGetProperty("schedule_relationship", out var sr) ? sr.GetString() : null;
            var isRealtime = scheduleRelationship == "SCHEDULED";

            // "departure"/"arrival" is a StopTimeEvent: {scheduled, scheduled_local, scheduled_utc,
            // estimated, estimated_local, estimated_utc, delay, uncertainty} — "estimated" is null
            // when no real-time prediction is available (e.g. schedule_relationship == "STATIC").
            string? estimated = null;
            if (d.TryGetProperty("departure", out var depEl) && depEl.ValueKind == JsonValueKind.Object &&
                depEl.TryGetProperty("estimated", out var est) && est.ValueKind == JsonValueKind.String)
            {
                estimated = est.GetString();
            }

            yield return new TrainDeparture(
                routeShortName,
                routeLongName,
                routeType,
                RouteTypeName(routeType),
                headsign,
                scheduled,
                estimated,
                isRealtime);
        }
    }

    private static string RouteTypeName(int routeType) => routeType switch
    {
        0 => "tram",
        1 => "subway",
        2 => "rail",
        3 => "bus",
        4 => "ferry",
        5 => "cable tram",
        6 => "aerial lift",
        7 => "funicular",
        11 => "trolleybus",
        12 => "monorail",
        _ => "transit",
    };
}
