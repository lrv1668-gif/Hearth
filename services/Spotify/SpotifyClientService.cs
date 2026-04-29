using SpotifyAPI.Web;

namespace Spotify;

public sealed class SpotifyClientService(SpotifyStore store, IConfiguration config)
{
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
