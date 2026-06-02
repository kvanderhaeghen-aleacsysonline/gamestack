# Notifications & secrets

## Review notifications

When a reviewer is assigned to a version, or a verdict is recorded, Gamestack notifies the relevant
person over any configured channel (best-effort — a channel failure never blocks the workflow):

- **Slack** — an Incoming Webhook URL (`SlackWebhookNotifier`).
- **Email** — SMTP via MailKit (`SmtpEmailNotifier`). Note M365 tenants often block SMTP AUTH; an app
  password or relay may be required.

Channels are configured in **Settings → Notifications** and rebuilt from `AppSettings` whenever
settings change. Both implement the Core `INotifier` abstraction.

## SMTP password protection (DPAPI)

The SMTP password is the one secret Gamestack stores. It is encrypted at rest via
[`ISecretProtector`](xref:Gamestack.Core.Abstractions.ISecretProtector):

- On **Windows**, `WindowsDpapiSecretProtector` uses DPAPI (`ProtectedData`, `CurrentUser` scope). The
  ciphertext is stored as `dpapi:<base64>` in `settings.json`.
- On other platforms, a `PassthroughSecretProtector` stores the value as plaintext (documented limitation).

The in-memory `AppSettings` always holds the **plaintext** password (so the notifier can use it
directly); `JsonSettingsStore` encrypts on save and decrypts on load.

### New machine / moved settings

DPAPI `CurrentUser` scope ties the ciphertext to the signed-in Windows user **on that machine** — by
design, the secret never leaves the device. Consequences:

- A **fresh install on a new laptop** starts with no settings file, so the user simply re-enters the
  password during setup — no regression.
- If a settings file is **copied or roamed** to another machine/user, the value cannot be decrypted.
  `Unprotect` returns `null` in that case, so the app treats the password as **missing and re-prompts**
  rather than failing. The next save re-encrypts it under the new machine's key.

This matches how browsers and Windows Credential Manager behave: DPAPI secrets don't follow you to a new
machine; you re-authenticate. Since the SMTP password is a re-enterable credential (not precious data),
this is an acceptable trade-off.
