namespace Gamestack.Core.Abstractions;

/// <summary>
/// Encrypts and decrypts small secret strings (e.g. an SMTP password) for at-rest storage in the
/// local settings file. Implementations are expected to be tolerant of round-tripping values that
/// are <c>null</c>, empty, or already in plaintext (so migrating an existing plaintext value is safe).
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// Encrypt <paramref name="plaintext"/> for storage. Returns the protected form (carrying a marker
    /// so it can be recognized on load). <c>null</c>/empty input is returned unchanged.
    /// </summary>
    string? Protect(string? plaintext);

    /// <summary>
    /// Reverse <see cref="Protect"/>. Values that are <c>null</c>/empty are returned unchanged; values
    /// that are not in protected form are treated as legacy plaintext and returned as-is. Returns
    /// <c>null</c> when a protected value cannot be decrypted (e.g. moved to a different machine/user),
    /// so callers treat the secret as missing and re-prompt rather than fail.
    /// </summary>
    string? Unprotect(string? stored);

    /// <summary>True when <paramref name="value"/> is in this protector's encrypted form.</summary>
    bool IsProtected(string? value);
}
