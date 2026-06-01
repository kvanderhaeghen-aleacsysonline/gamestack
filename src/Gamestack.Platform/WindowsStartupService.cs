using System.Runtime.Versioning;
using Gamestack.Core.Abstractions;
using Microsoft.Win32;

namespace Gamestack.Platform;

/// <summary>
/// Run-on-startup via the per-user <c>HKCU\…\CurrentVersion\Run</c> registry key. On non-Windows
/// platforms every member is a safe no-op.
/// </summary>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Gamestack";

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            return ReadValue() is not null;
        }
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        Apply(enabled);
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) as string;
    }

    [SupportedOSPlatform("windows")]
    private static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null) return;

        if (enabled)
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe))
                key.SetValue(ValueName, $"\"{exe}\"");
        }
        else if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
