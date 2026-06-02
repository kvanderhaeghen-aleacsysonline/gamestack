# Gamestack MCP server

A [Model Context Protocol](https://modelcontextprotocol.io) server that exposes **read-only**
discovery of Gamestack assets to AI agents (Claude Desktop, Cursor, VS Code, etc.). It reads the
same `settings.json` and synced-folder manifest as the desktop app — it never mutates them.

## Tools

| Tool | Description |
|------|-------------|
| `search_assets` | Search by name substring, tags, file type, game id, and/or a custom attribute (all filters AND together). |
| `get_asset` | Full metadata for one asset: version history, tags, attributes, review status, comment thread. |
| `list_assets` | Every tracked asset with current version + tags. |
| `list_tags` | The workspace tag vocabulary. |
| `get_workspace_info` | Project name, linked game, file/tag counts, custom-attribute definitions. |

The server resolves the workspace from `%AppData%\Gamestack\settings.json` (`SyncedFolderRoot`);
complete setup in the desktop app first, otherwise tool calls return a "not configured" error.

## Run

```bash
rtk dotnet run --project src/Gamestack.Mcp
```

Transport is **stdio** (JSON-RPC on stdout; all logging goes to stderr).

## Register with a client

Publish a self-contained build (or use `dotnet <path-to-dll>`), then add to the client's MCP config.
Example (Claude Desktop `claude_desktop_config.json`):

```jsonc
{
  "mcpServers": {
    "gamestack": {
      "command": "dotnet",
      "args": ["C:\\Develop\\FreakyFriday\\game-control\\src\\Gamestack.Mcp\\bin\\Debug\\net10.0\\Gamestack.Mcp.dll"]
    }
  }
}
```

## Notes

- Built on the official `ModelContextProtocol` C# SDK (MIT).
- Read-only by design. A future revision could add pull/download or manifest-write tools behind a
  setting; until then agents can discover but never change assets.
