# Manifest & sharding

Gamestack stores app-managed version metadata, feedback threads, tags, and custom attributes as JSON
under a `.gamestack/` folder inside the synced "remote", so all clients share it.

## Sharded layout

Rather than one growing `manifest.json`, metadata is **sharded**:

```
.gamestack/
  workspace.json            # project id/name, game link, tag vocabulary, attribute definitions
  files/
    characters/hero.psd.json   # one shard per asset: its versions, comments, tags, attributes
    env/rock.png.json
```

- `workspace.json` ([`WorkspaceMetadata`](xref:Gamestack.Core.Models.WorkspaceMetadata)) holds small,
  workspace-wide data and is written only when those settings change.
- Each asset has its own shard ([`AssetShard`](xref:Gamestack.Core.Models.AssetShard) wrapping
  [`AssetFile`](xref:Gamestack.Core.Models.AssetFile)).

### Why shard?

A single manifest was rewritten in full on **every push**. Over a OneDrive synced folder with several
artists pushing many assets per day, that caused two real problems: concurrent whole-file writes
**clobbered each other** (lost updates), and the OneDrive client spawned **conflict copies**. Sharding
means a push rewrites only the affected asset's small file, so:

- Concurrent pushes to **different** assets never collide.
- OneDrive only ever conflicts on the **same** asset — which is a genuine edit conflict the app already
  detects via the per-file version baseline.
- Memory stays flat; load assembles only what exists.

[`AssetMetadataStore`](xref:Gamestack.Core.Sync.AssetMetadataStore) owns the layout. It assembles the
in-memory [`Manifest`](xref:Gamestack.Core.Models.Manifest) from the shards (with an in-session
last-modified cache that skips re-reading unchanged shards), and migrates a legacy single
`manifest.json` to shards automatically on first load. On push,
[`SyncEngine`](xref:Gamestack.Core.Sync.SyncEngine) re-reads the single shard for the conflict check so
same-file races are caught even if a teammate pushed since the manifest was loaded.

## Versioning & change detection

- `ManifestService` increments a file's version on each successful push, recording description, author
  (`pushedBy`, from the connected identity), UTC time, content hash, and the backend version id.
- `ChangeDetector` marks a file *dirty* when its current hash differs from the stored baseline.

## Tags, attributes & search

- **Tags**: a workspace vocabulary; per-asset tags drawn from it. New assets are **auto-tagged** on
  first push by matching filename tokens against the vocabulary (`FileNameTokenizer`).
- **Custom attributes**: workspace-defined fields (text/number/boolean/date) set per asset.
- **Search**: `AssetSearch` filters assets by name, tags, file type, game id, and attributes.
