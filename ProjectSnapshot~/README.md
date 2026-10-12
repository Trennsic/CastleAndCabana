# ProjectSnapshot~

This folder is a tracked-in-git copy of the parts of the Unity project that
live **outside** `Assets\_Project\` but that a from-scratch rebuild still
needs: project-wide settings and the Package Manager manifest. It exists
because `Assets\_Project\` is the only folder under version control — this is
how the rest of `ProjectSettings\` and `Packages\` survive a clean clone.

The trailing `~` on this folder's name is intentional: Unity ignores any
folder ending in `~`, so the `.asset`/`.json` files copied in here are never
picked up by Unity's importer and never show up as real project assets.

## What's in here

- `ProjectSettings\` — a full recursive mirror of the project's
  `ProjectSettings\` folder (tag/layer manager, input manager, quality,
  graphics, URP settings, `ProjectVersion.txt`, etc.), including a nested
  `Packages\` subfolder if Unity ever writes one there.
- `Packages\manifest.json` and `Packages\packages-lock.json` — the Package
  Manager dependency manifest and its resolved lockfile. (The rest of
  `Packages\` — each installed package's cached contents — is not copied
  here; Package Manager re-downloads those from the version/source recorded
  in `manifest.json`.)

## How it stays up to date

Two ways, both doing the same byte-for-byte mirror copy (including mirroring
deletions — a file removed from the source is removed from here too):

1. **Automatic:** a git pre-commit hook at `Tools~\git-hooks\pre-commit`
   refreshes this folder and stages it into every commit. Enabled per-clone
   via `git config core.hooksPath Tools~/git-hooks` (see PACKAGES.md's
   rebuild steps).
2. **Manual:** the Unity menu item **Tools → Castle and Cabana → Snapshot
   Project Settings** (`ProjectSnapshotTool.cs`), which also saves any
   pending in-memory settings changes to disk first. This does not touch
   git — it only refreshes the files on disk.

## How to restore from this snapshot

1. **Close Unity completely first.** Unity actively writes to
   `ProjectSettings\` while running, and restoring into a live project is
   unsafe — don't do this with the Editor open.
2. Copy the contents of `ProjectSnapshot~\ProjectSettings\` into the
   project's real `ProjectSettings\` folder (the one next to `Assets\`, two
   levels up from this file), overwriting what's there.
3. Copy `ProjectSnapshot~\Packages\manifest.json` and
   `ProjectSnapshot~\Packages\packages-lock.json` into the project's real
   `Packages\` folder, overwriting what's there.
4. Open Unity. It will re-resolve packages from the restored manifest and
   pick up the restored project settings.
