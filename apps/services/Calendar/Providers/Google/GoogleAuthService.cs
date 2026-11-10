using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Tasks.v1;
using ServiceDefaults;

namespace Calendar.Providers.Google;

public sealed class GoogleAuthService(CalendarStore store, IConfiguration config, TimeProvider timeProvider)
{
    public const string ProviderKey = "google";

    private static readonly string[] Scopes =
    [
        CalendarService.Scope.CalendarReadonly,
        TasksService.Scope.Tasks,
    ];

    private readonly OAuthStateStore _pendingStates = new(timeProvider);

    public string GenerateAuthUrl(string state)
    {
        _pendingStates.Remember(state);
        var flow    = BuildFlow();
        var request = flow.CreateAuthorizationCodeRequest(config["GOOGLE_REDIRECT_URI"]!);
        request.State = state;
        // GoogleAuthorizationCodeFlow adds access_type=offline automatically.
        // Append prompt=consent so Google always returns a refresh_token on exchange.
        return request.Build() + "&prompt=consent";
    }

    public bool ValidateAndConsumeState(string state) => _pendingStates.TryConsume(state);

    public async Task HandleCallbackAsync(string code, CancellationToken ct = default)
    {
        var flow          = BuildFlow();
        var tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId:                  "user",
            code:                    code,
            redirectUri:             config["GOOGLE_REDIRECT_URI"]!,
            taskCancellationToken:   ct);

        store.SaveToken(
            ProviderKey,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            timeProvider.GetUtcNow().AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600));
    }

    /// <summary>
    /// Where a failed sign-in lands: Settings → Connections, with Google's error code for
    /// the Calendar card to explain.
    /// </summary>
    public static string ConnectionsErrorUrl(string? frontendUrl, string error) =>
        $"{(frontendUrl ?? "").TrimEnd('/')}/settings?section=connections&calendar_error={Uri.EscapeDataString(error)}";

    /// <summary>
    /// Returns a fresh flow configured with the current client credentials.
    /// Called per-request — flows are stateless configuration objects.
    /// Also used by GoogleCalendarProvider for token refresh.
    /// </summary>
    public GoogleAuthorizationCodeFlow BuildFlow() =>
        new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId     = config["GOOGLE_CLIENT_ID"]!,
                ClientSecret = config["GOOGLE_CLIENT_SECRET"]!,
            },
            Scopes = Scopes,
        });
}
