using System.Net;

namespace Weather.Tests.Helpers;

public sealed class FakeHttpMessageHandler(
    string responseJson,
    HttpStatusCode status = HttpStatusCode.OK,
    string? airQualityJson = null,
    HttpStatusCode airQualityStatus = HttpStatusCode.OK)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isAirQuality = request.RequestUri!.Host.Contains("air-quality") && airQualityJson is not null;
        var response = new HttpResponseMessage(isAirQuality ? airQualityStatus : status)
        {
            Content = new StringContent(
                isAirQuality ? airQualityJson! : responseJson,
                System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
