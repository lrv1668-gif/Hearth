using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace Spotify;

public sealed class SpotifyClientService(
    SpotifyStore store, IConfiguration config, IHttpClientFactory? httpClients = null)
{
    /// <summary>The named <see cref="HttpClient"/> Spotify API calls go through.</summary>
    public const string HttpClientName = "spotify";

    private readonly string _clientId     = config["SPOTIFY_CLIENT_ID"]!;
    private readonly string _clientSecret = config["SPOTIFY_CLIENT_SECRET"]!;

    public SpotifyClient? TryGetClient()
    {
        var token = store.Load();
        if (token is null) return null;

        // Use actual remaining seconds; negative value ensures IsExpired = true for stale tokens
        var secondsRemaining = (int)(token.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        var initialResponse = new AuthorizationCodeTokenResponse
        {
            AccessToken  = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn    = secondsRemaining,
            TokenType    = "Bearer",
        };

        var authenticator = new AuthorizationCodeAuthenticator(_clientId, _clientSecret, initialResponse);

        authenticator.TokenRefreshed += (_, refreshed) =>
            store.Save(
                refreshed.AccessToken,
                refreshed.RefreshToken ?? token.RefreshToken,
                DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn));

        var clientConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(authenticator);
        // A pooled handler, rather than the library's own HttpClient per client built every poll
        if (httpClients is not null)
            clientConfig = clientConfig.WithHTTPClient(new NetHttpClient(httpClients.CreateClient(HttpClientName)));

        return new SpotifyClient(clientConfig);
    }

    public async Task SaveTokensFromCode(string code, string redirectUri)
    {
        var response = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(_clientId, _clientSecret, code, new Uri(redirectUri)));

        store.Save(
            response.AccessToken,
            response.RefreshToken,
            DateTime.UtcNow.AddSeconds(response.ExpiresIn));
    }
}
