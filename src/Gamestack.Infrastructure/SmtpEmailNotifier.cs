using Gamestack.Core.Notifications;
using Gamestack.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Gamestack.Infrastructure;

/// <summary>
/// Sends notifications by email over SMTP (via MailKit). Configured from <see cref="NotificationSettings"/>.
/// Note: Microsoft 365 tenants often disable SMTP AUTH — an admin may need to enable it or issue an
/// app password for this to work.
/// </summary>
public sealed class SmtpEmailNotifier : INotifier
{
    private readonly NotificationSettings _settings;

    /// <summary>Create the notifier from the current notification settings.</summary>
    public SmtpEmailNotifier(NotificationSettings settings) => _settings = settings;

    /// <inheritdoc />
    public string Name => "Email";

    /// <inheritdoc />
    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
            return; // nothing to address

        var from = _settings.FromAddress ?? _settings.SmtpUser
            ?? throw new InvalidOperationException("No SMTP From address or username configured.");
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            throw new InvalidOperationException("No SMTP host configured.");

        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(from));
        mime.To.Add(new MailboxAddress(message.RecipientName ?? message.RecipientEmail, message.RecipientEmail));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.Body };

        using var client = new SmtpClient();
        var security = _settings.SmtpUseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, security, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(_settings.SmtpUser))
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword ?? string.Empty, ct).ConfigureAwait(false);
        await client.SendAsync(mime, ct).ConfigureAwait(false);
        await client.DisconnectAsync(true, ct).ConfigureAwait(false);
    }
}
