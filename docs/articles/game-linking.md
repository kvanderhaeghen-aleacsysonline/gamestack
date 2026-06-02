# Game linking

Each top-level project folder is tied to a game so that exported assets can later be discovered per
game (e.g. by a VS Code extension).

## `gamestack.json`

Every top-level directory under the synced root carries a marker file:

```json
{ "gameId": "cosmic-slots" }
```

- `gameId` defaults to the folder name and is editable in the app's **Games** tab.
- Managed by [`GameLinkService`](xref:Gamestack.Core.Projects.GameLinkService): it scans, seeds missing
  markers, and counts unlinked folders.

## When markers are created

- On (re)configuring the synced folder, a marker is seeded in every top-level directory.
- On a returning launch, if some folders lack a marker (e.g. a new folder synced in from OneDrive), the
  app offers to create them.
- A `FileSystemWatcher` on the synced root detects new top-level folders live and prompts the same way.

The format is intentionally simple and forward-compatible: the planned VS Code extension reads
`gamestack.json` by `gameId` to list and pull a game's exported assets.
