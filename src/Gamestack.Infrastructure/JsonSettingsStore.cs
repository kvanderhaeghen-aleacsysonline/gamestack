using System.Text.Json;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Security;
using Gamestack.Core.Settings;
using Gamestack.Core.Versioning;

namespace Gamestack.Infrastructure;

/// <summary>
/// <see cref="ISettingsStore"/> that persists <see cref="AppSettings"/> as a JSON file
/// (by default under <c>%AppData%\Gamestack\settings.json</c>). The SMTP password is encrypted at
/// rest via the supplied <see cref="ISecretProtector"/>; the in-memory <see cref="AppSettings"/>
/// always holds the plaintext value so callers (e.g. the SMTP notifier) use it directly.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly ISecretProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Create a store at the given file path (or the default app-data location), with an
    /// optional secret protector (defaults to plaintext pass-through).</summary>
    public JsonSettingsStore(string? path = null, ISecretProtector? protector = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gamestack", "settings.json");
        _protector = protector ?? new PassthroughSecretProtector();
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return new AppSettings();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json = await File.ReadAllTextAsync(_path, ct).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, ManifestService.JsonOptions) ?? new AppSettings();
            // Decrypt the at-rest SMTP password into plaintext for in-memory use. A failed decrypt
            // (moved machine/user) yields null, so the user is prompted to re-enter it.
            settings.Notifications.SmtpPassword = _protector.Unprotect(settings.Notifications.SmtpPassword);
            return settings;
        }
        catch (JsonException)
        {
            return new AppSettings(); // corrupt file — start fresh rather than crash
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            // Encrypt the SMTP password for at-rest storage, then restore the plaintext on the live
            // object so the caller keeps using the usable value.
            var plaintext = settings.Notifications.SmtpPassword;
            settings.Notifications.SmtpPassword = _protector.Protect(plaintext);
            try
            {
                var json = JsonSerializer.Serialize(settings, ManifestService.JsonOptions);
                await File.WriteAllTextAsync(_path, json, ct).ConfigureAwait(false);
            }
            finally
            {
                settings.Notifications.SmtpPassword = plaintext;
            }
        }
        finally { _gate.Release(); }
    }
}
