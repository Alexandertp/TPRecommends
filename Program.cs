using System;
using Avalonia;

namespace SpotifyRecommender;

class Program
{
    //STAThread er kort for Single Thread Apartment, så den kan kun tilgås af en enkelt tråd af gangen.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    //Her begår vi lidt Dependency Injection for at bygge vores app, så den kører med de funktioner den kræver
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
