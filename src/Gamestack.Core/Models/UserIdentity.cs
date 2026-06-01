namespace Gamestack.Core.Models;

/// <summary>
/// Identity of the signed-in user, resolved from the connected cloud account
/// (OneDrive/SharePoint via Graph <c>/me</c>, later GitHub/Google). It is never typed in
/// by hand, and is stamped on every pushed <see cref="AssetVersion"/> and feedback <see cref="Comment"/>.
/// </summary>
/// <param name="Id">Stable provider identifier (e.g. Azure AD object id, GitHub user id).</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="Email">Primary email address or user principal name.</param>
public sealed record UserIdentity(string Id, string DisplayName, string Email);
