using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpotifyRecommender.Models;

namespace SpotifyRecommender.Services;

/// <summary>
/// Thin wrapper around the Spotify Web API endpoints this app uses.
/// Each method requires a valid access token from SpotifyAuthService.
/// </summary>
public class SpotifyApiService
{
    private const string BaseUrl = "https://api.spotify.com/v1";

    private readonly HttpClient _http;

    public SpotifyApiService(HttpClient http) => _http = http;

    // ── API calls ─────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /me/player/recently-played
    /// Returns up to <paramref name="limit"/> unique tracks the user recently played.
    /// AI Generated
    /// </summary>
    public async Task<List<SpotifyTrack>> GetRecentlyPlayedAsync(
        string accessToken, int limit = 20, CancellationToken ct = default)
    {
        var json = await GetAsync($"{BaseUrl}/me/player/recently-played?limit={limit}", accessToken, ct);

        var response = JsonSerializer.Deserialize<RecentlyPlayedResponse>(json)
                       ?? new RecentlyPlayedResponse(new());

        // De-duplicate: the endpoint can return the same track played multiple times
        return response.Items
                       .Select(i => i.Track)
                       .DistinctBy(t => t.Id)
                       .ToList();
    }

    public async Task<ProfileResponse> GetUserAsync(string accessToken, CancellationToken ct)
    {
        var json = await GetAsync($"{BaseUrl}/me", accessToken, ct);
        ProfileResponse profilrespons = JsonSerializer.Deserialize<ProfileResponse>(json);
        
        return profilrespons;
    }

    /// <summary>
    /// GET /recommendations
    /// Uses up to 5 seed tracks + 2 seed artists extracted from recentTracks.
    /// Optional audio-feature targets can be passed in <paramref name="targets"/>.
    /// AI Generated
    /// </summary>
    public async Task<List<SpotifyTrack>> GetRecommendationsAsync(
        string accessToken,
        List<SpotifyTrack> recentTracks,
        int limit = 15,
        Dictionary<string, string>? targets = null,
        CancellationToken ct = default)
    {
        var seedTracks  = recentTracks.Take(5).Select(t => t.Id);
        var seedArtists = recentTracks
                          .SelectMany(t => t.Artists)
                          .Select(a => a.Id)
                          .Distinct()
                          .Take(2);

        var qs = new Dictionary<string, string>
        {
            ["seed_tracks"]  = string.Join(",", seedTracks),
            ["seed_artists"] = string.Join(",", seedArtists),
            ["limit"]        = limit.ToString(),
            ["seed_genres"]  = "classical,country"
        };

        // Merge any optional audio-feature targets (e.g. target_energy, min_tempo)
        if (targets is not null)
            foreach (var (k, v) in targets)
                qs[k] = v;

        var url  = BuildUrl($"{BaseUrl}/recommendations", qs);
        var json = await GetAsync(url, accessToken, ct);

        var response = JsonSerializer.Deserialize<RecommendationsResponse>(json)
                       ?? new RecommendationsResponse(new());
        return response.Tracks;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> GetAsync(string url, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request, ct);
        var body     = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Spotify API error {(int)response.StatusCode}: {body}");

        return body;
    }

    private static string BuildUrl(string baseUrl, Dictionary<string, string> query)
    {
        var pairs = query.Select(kv =>
            $"{System.Uri.EscapeDataString(kv.Key)}={System.Uri.EscapeDataString(kv.Value)}");
        return $"{baseUrl}?{string.Join("&", pairs)}";
    }
}
