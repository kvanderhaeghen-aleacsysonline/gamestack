# Architecture

A thin Avalonia UI over a platform-agnostic core, with all cloud, platform, and persistence concerns
isolated behind interfaces.

## Projects

| Project | Responsibility |
|---------|----------------|
| **Gamestack.Core** | Domain models, sync engine, validators, and abstractions. **Pure BCL — zero external dependencies.** |
| **Gamestack.Storage.SyncedFolder** | Folder-backed `IStorageBackend` (the synced-folder model). |
| **Gamestack.Infrastructure** | JSON local-state / settings stores; Slack + SMTP notifiers (MailKit). |
| **Gamestack.Platform** | Windows-only: OneDrive identity, run-on-startup, shutdown-block, DPAPI secret protection. |
| **Gamestack.App** | Avalonia MVVM (CommunityToolkit.Mvvm) + Microsoft.Extensions.DependencyInjection. |
| **Gamestack.Mcp** | Model Context Protocol server exposing assets to AI agents. |

## Core abstractions

Everything backend-, platform-, or persistence-specific lives behind an interface in
`Gamestack.Core.Abstractions`, so the UI and sync engine never depend on a concrete implementation:

- `IStorageBackend` — list / download / upload / read-write text against the "remote".
- `IAuthProvider` — resolves the signed-in `UserIdentity` (never typed by the user).
- `ILocalStateStore` — selective-sync membership + per-file synced baselines.
- `ISettingsStore` — persisted `AppSettings`.
- `INotifier` — outbound review notifications (Slack, SMTP).
- `IStartupService` — run-on-startup registration.
- `IAssetValidator` — pluggable validation rules.
- `ISecretProtector` — at-rest encryption of secrets (see [notifications & secrets](notifications-and-secrets.md)).

## Rules

- New cloud/UI/platform code goes **behind a Core abstraction**; never add dependencies to Core.
- Before adding a NuGet package, check its license and CVEs.
- MVVM: `ViewLocator` maps `ViewModels.XxxViewModel` → `Views.XxxView` by name.
