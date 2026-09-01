# Save-file diff

`diff_saves.py` compares two Planet Crafter save files at the level that matters when the game
ships a new version: **did the save format change, and where?**

```bash
python tools/save-diff/diff_saves.py OLD_SAVE NEW_SAVE
```

It reports BOM presence, section count, per-section record counts, JSON keys added/removed per
section, and a full value diff of the three single-object sections (0 unlocks, 5 statistics,
8 metadata). It reads a Steam save and a raw Xbox / PC Game Pass (WGS) blob equally — the framing
is identical, only the BOM differs.

## Procedure for a new game version

1. **Before updating the game**, copy the whole save folder for each platform into
   `<compare-root>/<old-version>/`.
2. Update the game, load each save once, and save in-game so the new build rewrites the file.
3. Copy the folders again into `<compare-root>/<new-version>/`.
4. Run `diff_saves.py` for each matching pair, on **both** platforms — a schema change that shows
   up on one and not the other would be a packaging difference worth knowing about.

Layout that has worked (`D:\DevTest\Planet Crafter`):

```
<compare-root>/
  <version>/
    steam/                      Backup.json, Chill-1.json, Standard-2.json, ...
    xbox gamepass windows/      GUID-named blobs + container.NNN
```

The Game Pass blobs are GUID-named with no extension; `container.NNN` is the WGS index and is not
a save. The largest two GUID files are usually the save and its backup — `diff_saves.py` will
reject anything that is not a save, so pointing it at the wrong blob is safe.

Then run `tools/item-catalog/report_missing.py` over the new saves to find content ids the app's
catalogs do not cover yet.

## Result for 2.008 → 2.102 (Skeo update)

Framing, section count/order, BOM behaviour, and every key in sections 1–9 were unchanged on both
platforms. The only schema change was **`logisticsPaused` added to section 0 (unlocks)** — modelled
as `SaveFileUnlocks.LogisticsPaused` (nullable, so a pre-2.102 save does not gain the key on save).
