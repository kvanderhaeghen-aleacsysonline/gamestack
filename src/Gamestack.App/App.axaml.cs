using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gamestack.App.Services;
using Gamestack.App.ViewModels;
using Gamestack.App.Views;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Projects;
using Gamestack.Core.Validation;
using Gamestack.Core.Versioning;
using Gamestack.Infrastructure;
using Gamestack.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestack.App;

public partial class App : Application
{
    /// <summary>The application service provider.</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Keep the app alive in the tray when the window is closed (for reminders / shutdown-hold).
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow { DataContext = mainViewModel };
            desktop.MainWindow = window;

            var integration = new WindowsIntegration(desktop, window, Services.GetRequiredService<WorkspaceSession>());
            integration.Start();

            // Startup runs from MainWindow.Opened (so dialogs have a shown owner window).
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore());
        services.AddSingleton<ILocalStateStore>(_ => new JsonLocalStateStore());
        services.AddSingleton<IAuthProvider, OneDriveIdentityProvider>();
        services.AddSingleton<IStartupService, WindowsStartupService>();

        services.AddSingleton<IAssetValidator, ImageDimensionValidator>();
        services.AddSingleton<IAssetValidator, SpineVersionValidator>();
        services.AddSingleton<AssetValidationRunner>();
        services.AddSingleton<ManifestService>();
        services.AddSingleton<GameLinkService>();
        services.AddSingleton<WorkspaceSession>();
        services.AddSingleton<ReviewService>();
        services.AddSingleton<DialogService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<MainWindowViewModel>());
        services.AddTransient<SetupViewModel>();
        services.AddTransient<ExplorerViewModel>();
        services.AddTransient<ChangesViewModel>();
        services.AddTransient<ReviewInboxViewModel>();
        services.AddTransient<GamesViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
