# MCP server

`Gamestack.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server that exposes
**read-only** asset discovery to AI agents (Claude Desktop, Cursor, VS Code). It is built on the
official `ModelContextProtocol` C# SDK and communicates over **stdio** (JSON-RPC on stdout; all logging
goes to stderr).

It reads the same `settings.json` and sharded manifest as the desktop app, via
`WorkspaceManifestAccessor` — it never mutates them.

## Tools

| Tool | Purpose |
|------|---------|
| `search_assets` | Search by name, tags, file type, game id, and/or a custom attribute. |
| `get_asset` | Full metadata for one asset: versions, tags, attributes, review status, comments. |
| `list_assets` | Every tracked asset with current version + tags. |
| `list_tags` | The workspace tag vocabulary. |
| `get_workspace_info` | Project name, linked game, file/tag counts, attribute definitions. |

## Running & registering

```bash
dotnet run --project src/Gamestack.Mcp
```

Register it with a client by pointing at the built DLL — for example in Claude Desktop's
`claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "gamestack": {
      "command": "dotnet",
      "args": ["<repo>/src/Gamestack.Mcp/bin/Debug/net10.0/Gamestack.Mcp.dll"]
    }
  }
}
```

Complete setup in the desktop app first; otherwise tool calls return a "not configured" error. The
server is read-only by design — a future revision could add pull/download or write tools behind a
setting. It is also the natural backbone for the planned VS Code extension.
