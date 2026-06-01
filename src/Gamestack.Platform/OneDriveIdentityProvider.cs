using System.Runtime.Versioning;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Microsoft.Win32;

namespace Gamestack.Platform;

/// <summary>
/// Resolves the user's identity from the machine's OneDrive client (HKCU OneDrive account keys),
/// falling back to the signed-in Windows account. No interactive sign-in is performed — the
/// OneDrive client is already authenticated, so this implementation of <see cref="IAuthProvider"/>
/// simply surfaces that identity for authorship of pushes and feedback.
/// </summary>
public sealed class OneDriveIdentityProvider : IAuthProvider
{
    private UserIdentity? _current;

    /// <inheritdoc />
    public bool IsSignedIn => _current is not null;

    /// <inheritdoc />
    public UserIdentity? CurrentUser => _current;

    /// <inheritdoc />
    public Task<UserIdentity> SignInAsync(CancellationToken ct = default)
    {
        _current = Resolve()
            ?? throw new InvalidOperationException(
                "Could not determine a OneDrive/Windows identity on this machine. Ensure OneDrive is signed in.");
        return Task.FromResult(_current);
    }

    /// <inheritdoc />
    public Task SignOutAsync(CancellationToken ct = default)
    {
        // OneDrive owns the session; we only clear our cached view of it.
        _current = null;
        return Task.CompletedTask;
    }

    private static UserIdentity? Resolve()
    {
        if (OperatingSystem.IsWindows())
        {
            var fromOneDrive = ReadOneDriveAccount();
            if (fromOneDrive is not null)
                return fromOneDrive;
        }

        var windowsName = Environment.UserName;
        return string.IsNullOrWhiteSpace(windowsName)
            ? null
            : new UserIdentity(windowsName, windowsName, "");
    }

    [SupportedOSPlatform("windows")]
    private static UserIdentity? ReadOneDriveAccount()
    {
        using var accounts = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts");
        if (accounts is null)
            return null;

        // Prefer a work/school (Business*) account over a personal one.
        var names = accounts.GetSubKeyNames()
            .OrderBy(n => n.StartsWith("Business", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        foreach (var name in names)
        {
            using var key = accounts.OpenSubKey(name);
            var email = key?.GetValue("UserEmail") as string;
            if (string.IsNullOrWhiteSpace(email))
                continue;

            var display = key?.GetValue("UserName") as string;
            var id = key?.GetValue("cid") as string ?? email;
            return new UserIdentity(id, string.IsNullOrWhiteSpace(display) ? email : display!, email);
        }

        return null;
    }
}
