using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Gamestack.Core.Abstractions;

namespace Gamestack.Platform;

/// <summary>
/// Windows DPAPI implementation of <see cref="ISecretProtector"/> using
/// <see cref="ProtectedData"/> at <see cref="DataProtectionScope.CurrentUser"/> scope. Protected
/// values are stored as <c>dpapi:&lt;base64&gt;</c> so they are recognizable and migratable.
/// </summary>
/// <remarks>
/// CurrentUser scope ties the ciphertext to the signed-in Windows user on this machine — it cannot be
/// decrypted by another user or on another machine. That is intentional for a per-user desktop app:
/// the secret never leaves the device. If a protected value is moved (e.g. a copied/roamed settings
/// file), <see cref="Unprotect"/> returns <c>null</c> so the caller treats it as missing and re-prompts.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiSecretProtector : ISecretProtector
{
    private const string Marker = "dpapi:";

    /// <inheritdoc />
    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;
        var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), null, DataProtectionScope.CurrentUser);
        return Marker + Convert.ToBase64String(cipher);
    }

    /// <inheritdoc />
    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
            return stored;
        if (!IsProtected(stored))
            return stored; // legacy plaintext — leave as-is (will be protected on next save)

        try
        {
            var cipher = Convert.FromBase64String(stored[Marker.Length..]);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Wrong user/machine or corrupted value — treat the secret as missing so the user re-enters it.
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsProtected(string? value) =>
        value is not null && value.StartsWith(Marker, StringComparison.Ordinal);
}
