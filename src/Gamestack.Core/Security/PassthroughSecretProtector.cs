using Gamestack.Core.Abstractions;

namespace Gamestack.Core.Security;

/// <summary>
/// No-op <see cref="ISecretProtector"/> that stores secrets as plaintext. Used on platforms without
/// an OS secret store (non-Windows) and as a safe default. Keeps Gamestack.Core dependency-free.
/// </summary>
public sealed class PassthroughSecretProtector : ISecretProtector
{
    /// <inheritdoc />
    public string? Protect(string? plaintext) => plaintext;

    /// <inheritdoc />
    public string? Unprotect(string? stored) => stored;

    /// <inheritdoc />
    public bool IsProtected(string? value) => false;
}
