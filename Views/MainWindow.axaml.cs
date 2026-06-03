using System;
using Avalonia.Controls;
using SpotifyRecommender.ViewModels;
using SpotifyRecommender.Services;

namespace SpotifyRecommender.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // In a larger app you'd use a DI container (e.g. Microsoft.Extensions.DependencyInjection).
        // For clarity here we wire up manually.
        var http       = new System.Net.Http.HttpClient();
        var apiService = new SpotifyApiService(http);
        
        // Auth service is built with current ClientId/RedirectUri when the
        // AuthorizeCommand runs (so it picks up what the user typed).
        // We pass a placeholder here; MainViewModel rebuilds it before use.
        var authService = new SpotifyAuthService(http, "", "http://localhost:8080/");
        var dbService = new DatabaseService();
        DataContext = new MainViewModel(authService, apiService, dbService);
    }
}
