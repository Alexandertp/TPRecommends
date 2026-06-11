using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpotifyRecommender.Models;

namespace SpotifyRecommender.Services;

/// <summary>
/// Handles Spotify PKCE OAuth flow for desktop apps.
/// Opens the system browser, spins up a local HttpListener to catch the
/// redirect, then exchanges the auth code for an access token.
/// </summary>
public class SpotifyAuthService
{
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";
    private const string AuthEndpoint  = "https://accounts.spotify.com/authorize";
    private const string Scopes        = "user-read-recently-played user-top-read";

    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _redirectUri;
    //AI-genereret
    public SpotifyAuthService(HttpClient http, string clientId, string redirectUri)
    {
        _http        = http;
        _clientId    = AppConfig.ClientId;
        _redirectUri = redirectUri;
    }

    // ── PKCE helpers ─────────────────────────────────────────────────────────
    //AI-genereret
    private static string GenerateCodeVerifier()
    {
        // 64 random bytes → 86-char base64url string (well within Spotify's 43–128 limit)
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }
    //AI-genereret
    private static string GenerateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }
    
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
               .TrimEnd('=')
               .Replace('+', '-')
               .Replace('/', '_');

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full PKCE flow:
    /// 1. Opens the Spotify auth page in the system browser
    /// 2. Waits for the redirect on localhost
    /// 3. Exchanges the code for a token
    /// Returns the TokenResponse on success, throws on failure.
    /// </summary>
    public async Task<TokenResponse> AuthorizeAsync(CancellationToken ct = default)
    {
        var verifier   = GenerateCodeVerifier();
        var challenge  = GenerateCodeChallenge(verifier);
        var state      = Guid.NewGuid().ToString("N")[..16]; // CSRF guard

        // Build the authorization URL
        var query = new Dictionary<string, string>
        {
            ["client_id"]             = _clientId,
            ["response_type"]         = "code",
            ["redirect_uri"]          = _redirectUri,
            ["code_challenge_method"] = "S256",
            ["code_challenge"]        = challenge,
            ["state"]                 = state,
            ["scope"]                 = Scopes,
        };
        var authUrl = BuildUrl(AuthEndpoint, query);

        // Open the browser
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        // Listen for the callback
        var (code, returnedState) = await WaitForCallbackAsync(ct);

        if (returnedState != state)
            throw new InvalidOperationException("OAuth state mismatch — possible CSRF attack.");

        // Exchange code for token
        return await ExchangeCodeAsync(code, verifier, ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Spins up a temporary HttpListener on the redirect URI port and waits
    /// for Spotify to send the auth code back.
    /// </summary>
    private async Task<(string Code, string State)> WaitForCallbackAsync(CancellationToken ct)
    {
        // HttpListener needs the prefix to end with "/" so we strip the path
        // and re-add it — e.g. "http://127.0.0.1:5543/"
        var uri    = new Uri(_redirectUri);
        var prefix = $"{uri.Scheme}://{uri.Authority}/";  // e.g. http://127.0.0.1:5543/

        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        // GetContextAsync doesn't accept a CancellationToken directly, so we
        // race it against the token via a TaskCompletionSource
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5)); // 5-minute timeout

        var tcs = new TaskCompletionSource<HttpListenerContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        cts.Token.Register(() => tcs.TrySetCanceled());

        _ = listener.GetContextAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully) tcs.TrySetResult(t.Result);
            else if (t.IsFaulted)          tcs.TrySetException(t.Exception!);
        }, TaskScheduler.Default);

        var context = await tcs.Task;

        // Send a friendly "you can close this tab" page back to the browser
        var response = context.Response;
        var html = "<html><body style='font-family:sans-serif;text-align:center;padding:3rem'>"
                 + "<h2>✓ Authorised!</h2><p>You can close this tab and return to the app.</p>"
                 + "</body></html>";
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(buffer, ct);
        response.Close();
        listener.Stop();

        var qs   = context.Request.QueryString;
        var code = qs["code"]  ?? throw new InvalidOperationException("No code in callback.");
        var st   = qs["state"] ?? "";

        if (qs["error"] is { } err)
            throw new InvalidOperationException($"Spotify auth error: {err}");

        return (code, st);
    }
    /// <summary>
    /// Takes a refreshToken and returns an AccessToken for use in API calls
    ///
    /// Af Alexander
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<TokenResponse> GetAccessTokenFromRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = AppConfig.ClientId,
        });
        var resp = await _http.PostAsync(TokenEndpoint, requestBody, ct);
        var tokenResp = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Refresh token exchange failed ({resp.StatusCode}): {tokenResp}");
        }
        return JsonSerializer.Deserialize<TokenResponse>(tokenResp);
    }
    //AI-genereret
    private async Task<TokenResponse> ExchangeCodeAsync(string code, string verifier, CancellationToken ct)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"]     = _clientId,
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = _redirectUri,
            ["code_verifier"] = verifier,
        });

        var resp = await _http.PostAsync(TokenEndpoint, body, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token exchange failed ({resp.StatusCode}): {json}");

        return JsonSerializer.Deserialize<TokenResponse>(json)
               ?? throw new InvalidOperationException("Empty token response.");
    }
    //AI-genereret
    private static string BuildUrl(string baseUrl, Dictionary<string, string> query)
    {
        var sb = new StringBuilder(baseUrl).Append('?');
        foreach (var (k, v) in query)
            sb.Append(Uri.EscapeDataString(k)).Append('=').Append(Uri.EscapeDataString(v)).Append('&');
        return sb.ToString().TrimEnd('&');
    }
}
