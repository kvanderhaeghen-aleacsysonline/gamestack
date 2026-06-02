# Synced-folder model

Gamestack's first (and current) storage backend leans on the machine's **existing OneDrive/SharePoint
sync client** rather than calling Microsoft Graph. This was a deliberate decision: no Azure AD app
registration, no pay-as-you-go storage, no separate credentials — it uses the studio's existing M365.

## How it works

- The app keeps a **local working directory** where artists edit files.
- A locally-synced **OneDrive/SharePoint folder** is treated as the "remote".
- **Push** = copy the file into the synced folder (+ update metadata); the OS OneDrive client uploads it.
- **Download** = copy from the synced folder into the working directory.

This is implemented by `SyncedFolderBackend`, a folder-copy `IStorageBackend` with zero dependencies.

## Honest trade-offs

- A push "completes" when the file lands in the synced folder. The OneDrive client uploads
  **asynchronously**, so the app does not confirm the cloud upload itself.
- Fetching the **bytes of a specific old version** is not possible through the filesystem. The manifest
  retains version *metadata* and history; byte-level old-version retrieval is a later add-on (it would
  use a Graph backend).

## Why behind an abstraction

Because everything goes through `IStorageBackend`, other backends — Microsoft Graph (for old-version
bytes), GitHub + LFS, Google Drive — can be added later without touching the UI or sync engine.
