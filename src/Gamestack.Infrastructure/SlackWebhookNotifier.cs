using System.Text;
using System.Text.Json;
using Gamestack.Core.Notifications;

namespace Gamestack.Infrastructure;

/// <summary>Posts notifications to a Slack channel via an Incoming Webhook (a single HTTP POST).</summary>
public sealed class SlackWebhookNotifier : INotifier
{
    private readonly string _webhookUrl;
    private readonly HttpClient _http;

    /// <summary>Create the notifier for a given webhook URL, using the supplied HTTP client.</summary>
    public SlackWebhookNotifier(string webhookUrl, HttpClient http)
    {
        _webhookUrl = webhookUrl;
        _http = http;
    }

    /// <inheritdoc />
    public string Name => "Slack";

    /// <inheritdoc />
    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        var text = new StringBuilder();
        text.Append('*').Append(message.Subject).Append("*\n").Append(message.Body);
        if (!string.IsNullOrWhiteSpace(message.RecipientName))
            text.Append("\n→ ").Append(message.RecipientName);

        var payload = JsonSerializer.Serialize(new { text = text.ToString() });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(_webhookUrl, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
