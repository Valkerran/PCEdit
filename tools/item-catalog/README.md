# Item catalog generators

These scripts (re)generate the app-bundled item catalog used by the Inventories
page to show a friendly name and a category icon for each `WorldObject.GId`. The
catalog is **app-only** — it is never read from or written to a save file.

Run both from the repo root:

```bash
python tools/item-catalog/gen_catalog.py   # -> PCEdit.App.Core/Data/ItemCatalog.json
python tools/item-catalog/gen_icons.py     # -> PCEdit.Desktop/Assets/Icons/cat_*.svg
```

## `gen_catalog.py`

Holds the curated `GId -> (displayName, category)` table and the category table
(`category -> displayName, icon`). Edit the tables at the top and re-run. Every
item's category must exist in `CATEGORIES` (the script asserts this, and
`ItemCatalogTests.EmbeddedCatalog_ParsesAndEveryItemCategoryIsDefined` re-checks
it at build time).

The `items` map was originally seeded from `PCEdit.SaveFileHandler/Standard-2.json`
alone -- a single-planet *Prime* save -- which left most Humble / Toxicity / Aqualis /
Selenea content uncovered. It now covers every distinct `GId` (world objects +
`unlockedGroups` + inventory demand/supply groups) across every sample save available,
on game versions 2.008 and 2.102, Steam and Game Pass. Add new ids as the game adds
content; unknown ids still render at runtime (raw id + the `misc` fallback icon).

To find what a newer game build (or a save on a planet the seed save never visited) is missing:

```bash
python tools/item-catalog/report_missing.py <save> [<save> ...]
```

It lists every `WorldObject.gId` and `unlockedGroups` entry absent from `ItemCatalog.json` and
every `demandGrps`/`supplyGrps` id absent from `LogisticsGroups.json`, with occurrence counts so
the ids worth naming first come out on top. Curate the output into the tables below and re-run the
generators.

To swap in real per-item art later, drop `item_<name>.png`/`.svg` into
`PCEdit.Desktop/Assets/Icons/` and add an `"icon": "item_<name>.png"` field to
that item's entry — it overrides the category icon, no code change needed.

## `gen_icons.py`

Emits one 64×64 rounded-tile SVG per category (coloured background, white
pictogram) so the icons read on both the light and dark inventory cards. The
SVGs are the source; each `<name>.png` shipped beside them is rasterised out of
band.
