# Gamestack — project guide for Claude

Desktop "source control for artists" for large game assets (Avalonia, .NET 10). Hides git/cloud
from artists: edit in a local workspace, push to the OneDrive/SharePoint **synced folder** (the
"remote"); the OS OneDrive client uploads. See `AGENT_NOTES.md` for build gotchas and decisions,
and the user memory for goals/status.

## Always
- Prefix every shell command with **`rtk`** (incl. inside `&&` chains). Trim verbose output with `| tail`.
- Build: `rtk dotnet build Gamestack.sln` · Test: `rtk dotnet test tests/Gamestack.Tests/Gamestack.Tests.csproj` · Run: `rtk dotnet run --project src/Gamestack.App`
- Target **net10.0** everywhere. `Nullable` + `ImplicitUsings` on. Core has `GenerateDocumentationFile` → XML `///` docs required on public members.

## Architecture (everything behind abstractions in Core)
- **Gamestack.Core** — domain + engine + validators. **Pure BCL, ZERO external deps — keep it that way.** Abstractions: `IStorageBackend`, `IAuthProvider`, `ILocalStateStore`, `ISettingsStore`, `INotifier`, `IStartupService`, `IAssetValidator`.
- **Gamestack.Storage.SyncedFolder** — folder-backed `IStorageBackend` (the synced-folder model). NO Graph/MSAL/Azure AD.
- **Gamestack.Infrastructure** — JSON `ILocalStateStore`/`ISettingsStore`; Slack + SMTP notifiers (MailKit).
- **Gamestack.Platform** — Windows-only: OneDrive identity, run-on-startup (registry), shutdown-block P/Invoke. Guard with `OperatingSystem.IsWindows()`.
- **Gamestack.App** — Avalonia MVVM (CommunityToolkit.Mvvm) + Microsoft.Extensions.DependencyInjection. ViewLocator maps `ViewModels.XxxViewModel`→`Views.XxxView`. Startup runs from `MainWindow.Opened`.
- **tests/Gamestack.Tests** — xUnit; in-memory/temp-dir fakes.

## Conventions
- New cloud/UI/platform code goes behind a Core abstraction; never add deps to Core.
- Before adding a NuGet package, check license + CVEs (ImageSharp v4 = paid; v2 = CVE → we read image headers ourselves).
- Game linking = per top-level dir `gamestack.json` `{ "gameId": "<folder name>" }` (`Core.Projects.GameLinkService`).
- Plan file: `~/.claude/plans/dynamic-toasting-shamir.md` (source of truth; the IDE Plan pane is a stale snapshot).
