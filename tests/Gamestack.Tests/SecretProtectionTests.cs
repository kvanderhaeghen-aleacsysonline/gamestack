using System.Text;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Security;
using Gamestack.Core.Settings;
using Gamestack.Infrastructure;
using Gamestack.Platform;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class SecretProtectionTests
{
    /// <summary>Reversible fake protector (cross-platform) for exercising the settings-store integration.</summary>
    private sealed class FakeProtector : ISecretProtector
    {
        public const string Marker = "enc:";
        public string? Protect(string? plaintext) =>
            string.IsNullOrEmpty(plaintext) ? plaintext : Marker + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string? Unprotect(string? stored) =>
            string.IsNullOrEmpty(stored) || !IsProtected(stored)
                ? stored
                : Encoding.UTF8.GetString(Convert.FromBase64String(stored[Marker.Length..]));
        public bool IsProtected(string? value) => value is not null && value.StartsWith(Marker, StringComparison.Ordinal);
    }

    [Fact]
    public void Passthrough_returns_input_unchanged()
    {
        var p = new PassthroughSecretProtector();
        Assert.Equal("secret", p.Protect("secret"));
        Assert.Equal("secret", p.Unprotect("secret"));
        Assert.False(p.IsProtected("secret"));
    }

    [Fact]
    public async Task SettingsStore_encrypts_password_on_disk_and_keeps_plaintext_in_memory()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        var store = new JsonSettingsStore(path, new FakeProtector());

        var settings = new AppSettings();
        settings.Notifications.SmtpPassword = "hunter2";
        await store.SaveAsync(settings);

        // On-disk value is protected (marked, not the raw secret); live object keeps plaintext.
        var raw = await File.ReadAllTextAsync(path);
        Assert.Contains(FakeProtector.Marker, raw);
        Assert.DoesNotContain("hunter2", raw);
        Assert.Equal("hunter2", settings.Notifications.SmtpPassword);

        // Reloading decrypts back to plaintext for use.
        var loaded = await store.LoadAsync();
        Assert.Equal("hunter2", loaded.Notifications.SmtpPassword);
    }

    [Fact]
    public async Task SettingsStore_loads_legacy_plaintext_password()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        // Write a pre-encryption settings file with a plaintext password.
        await File.WriteAllTextAsync(path, "{\"notifications\":{\"smtpPassword\":\"legacyPlain\"}}");

        var loaded = await new JsonSettingsStore(path, new FakeProtector()).LoadAsync();

        Assert.Equal("legacyPlain", loaded.Notifications.SmtpPassword);
    }

    [Fact]
    public void WindowsDpapi_round_trips_and_fails_gracefully()
    {
        if (!OperatingSystem.IsWindows())
            return; // DPAPI is Windows-only; skip elsewhere.

        var p = new WindowsDpapiSecretProtector();

        var protectedValue = p.Protect("hunter2");
        Assert.True(p.IsProtected(protectedValue));
        Assert.NotEqual("hunter2", protectedValue);
        Assert.Equal("hunter2", p.Unprotect(protectedValue));

        // A marked-but-undecryptable value (e.g. moved machine/user) yields null, not an exception.
        Assert.Null(p.Unprotect("dpapi:" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })));

        // Unmarked legacy plaintext passes through.
        Assert.Equal("plain", p.Unprotect("plain"));
        Assert.Empty(p.Protect("") ?? "x");
    }
}
