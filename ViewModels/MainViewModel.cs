using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyRecommender.Models;
using SpotifyRecommender.Services;

namespace SpotifyRecommender.ViewModels;

/// <summary>
/// Claude wrote all of this :)
/// MainViewModel is the single source of truth for the app's state.
/// The View (AXAML) binds to properties here and never touches services directly.
///
/// MVVM data flow:
///   View binds to  →  ObservableProperty (auto-notifies UI on change)
///   View invokes   →  [RelayCommand] (async-safe, tracks IsBusy automatically)
///   ViewModel calls→  Services (Auth / API)
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ── Dependencies (injected via constructor) ───────────────────────────────
    private readonly SpotifyAuthService _auth;
    private readonly SpotifyApiService  _api;
    private readonly DatabaseService _db;
    
    // ── Backing fields managed by [ObservableProperty] ────────────────────────
    // The source generator creates the public property, getter/setter, and
    // PropertyChanged notification automatically.
    //Store dele af direkte nedenstående kode er AI-genereret, og har ingen funktion
    //Efterladt til forklaring om "Hvad nu hvis"

    [ObservableProperty] private string  _statusMessage = "Log in with Spotify to get started.";
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private bool    _isAuthorised;
    [ObservableProperty] private bool    _hasRecentTracks;
    [ObservableProperty] private bool    _hasRecommendations;
    [ObservableProperty] private string? _accessToken;

    // Audio-feature tuning sliders (0.0 – 1.0 unless noted)
    [ObservableProperty] private double  _targetEnergy      = 0.5;
    [ObservableProperty] private double  _targetDanceability = 0.5;
    [ObservableProperty] private bool    _useAudioFeatures;

    // ── Collections bound to ItemsControl / ListBox in the View ──────────────
    public ObservableCollection<DatabaseTrack> RecentTracks       { get; } = new();
    public ObservableCollection<SpotifyTrack> Recommendations    { get; } = new();
    public DatabaseUser CurrentUser { get; set; }
    // ── Constructor ───────────────────────────────────────────────────────────
    //Dependency injection - Kind of
    public MainViewModel(SpotifyAuthService auth, SpotifyApiService api, DatabaseService db)
    {
        _auth = auth;
        _api  = api;
        _db = db;
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    // [RelayCommand] generates an IAsyncRelayCommand and wires CanExecute
    // automatically.  The View binds a Button's Command to AuthorizeCommand,
    // LoadRecentCommand, etc.
    
    /// <summary>Opens the browser and runs the full PKCE OAuth flow.
    /// Af Alexander</summary>
    [RelayCommand(CanExecute = nameof(CanAuthorize))]
    private async Task AuthorizeAsync(CancellationToken ct)
    {
        try
        {
            IsLoading     = true;
            StatusMessage = "Opening Spotify in your browser…";
            DatabaseUser currentUser = new DatabaseUser();
            if (_db.doesUserExist())
            {
                 currentUser = _db.getUserFromDB();
                 var refreshedResponse = await _auth.GetAccessTokenFromRefreshTokenAsync(currentUser.refreshToken, ct);
                 AccessToken = refreshedResponse.AccessToken;
                 currentUser.refreshToken = refreshedResponse.RefreshToken;
                 currentUser.lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                 _db.updateLoginOnCurrentUser(currentUser);
                 
                 IsAuthorised = true;
            }
            else
            {
                // Rebuild auth service with current form values
                var authService = BuildAuthService();
                var token = await authService.AuthorizeAsync(ct);

                AccessToken   = token.AccessToken;
                IsAuthorised  = true;
                var profilrespons = await _api.GetUserAsync(AccessToken, ct);

                currentUser.id = profilrespons.AccountId;
                currentUser.displayName = profilrespons.DisplayName;
                currentUser.lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                currentUser.refreshToken = token.RefreshToken;
                _db.CreateUser(currentUser);
            }

            CurrentUser = currentUser;
            StatusMessage = "Authorised! Loading your recently played tracks…";    

            await LoadRecentAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Authorisation cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Auth error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Login is always available — no user-supplied credentials needed
    //AI
    private bool CanAuthorize() => !IsLoading;

    /// <summary>Fetches /me/player/recently-played and populates RecentTracks.
    /// Af Alexander</summary>
    [RelayCommand(CanExecute = nameof(CanCallApi))]
    private async Task LoadRecentAsync(CancellationToken ct)
    {
        if (AccessToken is null) return;
        try
        {
            if (!_db.HasRecentTracks(CurrentUser.id))
            {
                IsLoading = true;
                StatusMessage = "Fetching recently played tracks…";

                var tracks = await _api.GetRecentlyPlayedAsync(AccessToken, limit: 20, ct);

                RecentTracks.Clear();
                foreach (var t in tracks)
                {
                    DatabaseTrack newTrack = new DatabaseTrack();
                    newTrack.name = t.Name;
                    RecentTracks.Add(newTrack);   
                }

                HasRecentTracks = RecentTracks.Count > 0;
                StatusMessage = $"Loaded {RecentTracks.Count} recent tracks. Ready to recommend!";
                _db.SaveRecentTracks(tracks, CurrentUser.id);
            }
            else
            {
                List<DatabaseTrack> recentTracks = _db.LoadRecentTracks(CurrentUser.id);
                foreach (var t in recentTracks)
                {
                    RecentTracks.Add(t);
                }
                HasRecentTracks = RecentTracks.Count > 0;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading recent tracks: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCallApi() => IsAuthorised && AccessToken is not null;

    // ── Private helpers ───────────────────────────────────────────────────────
    //AI

    private SpotifyAuthService BuildAuthService()
    {
        var http = new System.Net.Http.HttpClient();
        // ClientId and RedirectUri come from AppConfig — no user input needed
        return new SpotifyAuthService(http, AppConfig.ClientId, AppConfig.RedirectUri);
    }
}
