using Gamestack.Core.Models;

namespace Gamestack.Core.Abstractions;

/// <summary>
/// Authenticates against the cloud backend and exposes the signed-in identity. The resolved
/// <see cref="UserIdentity"/> is the single source of authorship for pushes and feedback —
/// the user never types their own name or email.
/// </summary>
public interface IAuthProvider
{
    /// <summary>True when a usable session/token is currently available.</summary>
    bool IsSignedIn { get; }

    /// <summary>The current signed-in user, or <c>null</c> when signed out.</summary>
    UserIdentity? CurrentUser { get; }

    /// <summary>Interactively sign in (opens a browser/system dialog) and resolve the identity.</summary>
    Task<UserIdentity> SignInAsync(CancellationToken ct = default);

    /// <summary>Sign out and clear any cached tokens.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}
