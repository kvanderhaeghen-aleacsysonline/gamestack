# Gamestack

**Source control for artists** — a desktop app that manages large game assets (Spine, PSD, AI,
3ds Max/Maya, GLB/GLTF, PNG/JPG/WEBP, MP4/MOV, PDF…) the way [mudstack](https://mudstack.com) does,
but backed by the studio's **own** OneDrive/SharePoint cloud rather than a SaaS. Artists never touch
git: the app downloads only the files they want, tracks local edits, and pushes them back with an
auto-assigned version and a change description.

## Where to start

- **[Introduction](articles/introduction.md)** — what Gamestack is and the problem it solves.
- **[Architecture](articles/architecture.md)** — projects, layering, and the core abstractions.
- **[Synced-folder model](articles/synced-folder-model.md)** — how the OneDrive/SharePoint backend works.
- **[Manifest & sharding](articles/manifest-and-sharding.md)** — metadata layout and concurrency.
- **[Game linking](articles/game-linking.md)** — `gamestack.json` per project folder.
- **[Notifications & secrets](articles/notifications-and-secrets.md)** — review notifications and SMTP password protection.
- **[MCP server](articles/mcp-server.md)** — AI agent access to assets and metadata.
- **[API reference](api/index.md)** — generated from the XML `///` documentation.
