namespace Gamestack.App.Services;

/// <summary>A view-model that loads its data asynchronously after being shown.</summary>
public interface IAsyncLoad
{
    /// <summary>Load (or refresh) the view-model's data.</summary>
    Task LoadAsync();
}

/// <summary>Top-level navigation, implemented by the main window view-model.</summary>
public interface INavigator
{
    /// <summary>Show the project explorer.</summary>
    Task GoToExplorerAsync();

    /// <summary>Show the pending-changes view.</summary>
    Task GoToChangesAsync();

    /// <summary>Show the settings page.</summary>
    void GoToSettings();

    /// <summary>Reload settings, rebuild the session, and return to the explorer (used after setup/settings save).</summary>
    Task ApplyConfigurationAsync();
}
