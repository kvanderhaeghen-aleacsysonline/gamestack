namespace Gamestack.Core.Abstractions;

/// <summary>Manages whether the app launches automatically when the user signs in (Windows).</summary>
public interface IStartupService
{
    /// <summary>True when run-on-startup is currently registered.</summary>
    bool IsEnabled { get; }

    /// <summary>Register or unregister the app to run at user sign-in. No-op on unsupported platforms.</summary>
    void SetEnabled(bool enabled);
}
