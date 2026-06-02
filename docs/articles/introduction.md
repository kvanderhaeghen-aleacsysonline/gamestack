# Introduction

Gamestack is a desktop application (Avalonia, .NET 10) that gives artists git-like source control for
large binary game assets — **without exposing git or the cloud**. It is modeled on mudstack but backed
by the studio's existing Microsoft 365 storage.

## The problem

Artists work with very large files (multi-hundred-MB PSDs, 3D scenes, video). Git is hostile to
binaries, and SaaS asset managers mean another vendor, another bill, and the studio's IP leaving its
own tenant. Artists also shouldn't have to learn branching, LFS, or sync clients.

## The approach

- A **local workspace** where artists edit files.
- The machine's existing **OneDrive/SharePoint synced folder** acts as the "remote". Pushing copies a
  file into that folder; the OS OneDrive client uploads it. See the
  [synced-folder model](synced-folder-model.md).
- The app assigns **semantic versions** and records a change description, author, and content hash in a
  shared manifest. See [manifest & sharding](manifest-and-sharding.md).
- **Identity is resolved automatically** from the signed-in OneDrive/Windows account — artists never
  type a name or email; it is stamped on every version and comment.

## What's built

Sync core (download / change-detect / push with auto-version), per-asset version history and feedback
threads, asset validation (image dimensions, Spine version), a review workflow with Slack/email
notifications, Windows tray + run-on-startup + end-of-day reminder, per-folder
[game linking](game-linking.md), [tags / custom attributes / search](manifest-and-sharding.md), and an
[MCP server](mcp-server.md) for AI agents.
