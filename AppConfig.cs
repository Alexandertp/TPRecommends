namespace SpotifyRecommender;

/// <summary>
/// Application-level constants.
///
/// CLIENT_ID:   Paste your Spotify Client ID here at build time.
///              Register exactly this redirect URI in your Spotify Dashboard:
///              http://127.0.0.1:5543/callback
///
/// Why 127.0.0.1 and not localhost?
///   Spotify's developer policy for desktop apps requires the loopback IP
///   rather than the hostname "localhost". Both resolve to the same address,
///   but Spotify validates the string literally, so the URI here must exactly
///   match what you register in the dashboard.
///
/// Why is the Client ID embedded?
///   Desktop apps cannot keep secrets — the binary can always be decompiled.
///   Spotify's own guidance is to embed the Client ID and use PKCE (already
///   in place), which means there is no Client Secret to steal. The PKCE
///   code_verifier proves each login is legitimate without needing a secret.
/// </summary>
public static class AppConfig
{
    public const string ClientId    = "abd9df8f9b4e4e128d525529f54363dc";
    public const string RedirectUri = "http://127.0.0.1:8080";
}
