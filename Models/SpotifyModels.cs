using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpotifyRecommender.Models;

// --- Shared primitives ---

public record SpotifyImage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height
);

public record SpotifyArtist(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    //Bliver ikke brugt, men det er en del af Spotify API'en.
    [property: JsonPropertyName("external_urls")] Dictionary<string, string> ExternalUrls
);

public record SpotifyAlbum(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("images")] List<SpotifyImage> Images
);

public record SpotifyTrack(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("artists")] List<SpotifyArtist> Artists,
    [property: JsonPropertyName("album")] SpotifyAlbum Album,
    [property: JsonPropertyName("external_urls")] Dictionary<string, string> ExternalUrls,
    [property: JsonPropertyName("preview_url")] string? PreviewUrl,
    [property: JsonPropertyName("duration_ms")] int DurationMs
)
{
    // Convenience helpers used in the ViewModel
    public string ArtistNames => string.Join(", ", Artists.ConvertAll(a => a.Name));
    public string? SmallImageUrl => Album.Images.Count >= 2 ? Album.Images[^1].Url : Album.Images.FirstOrDefault()?.Url;
    public string SpotifyUrl => ExternalUrls.GetValueOrDefault("spotify", "#");
    public string DurationFormatted => $"{DurationMs / 60000}:{(DurationMs % 60000 / 1000):D2}";
}

// --- Recently-played endpoint ---

public record PlayHistoryItem(
    [property: JsonPropertyName("track")] SpotifyTrack Track,
    [property: JsonPropertyName("played_at")] string PlayedAt
);

public record RecentlyPlayedResponse(
    [property: JsonPropertyName("items")] List<PlayHistoryItem> Items
);

// --- Recommendations endpoint ---

public record RecommendationsResponse(
    [property: JsonPropertyName("tracks")] List<SpotifyTrack> Tracks
);

// --- Token exchange (PKCE) ---

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("scope")] string Scope
);
