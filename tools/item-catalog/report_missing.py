#!/usr/bin/env python3
"""List the ids in one or more save files that the bundled catalogs do not cover.

A new game version adds content ids the app has never seen. Unknown ids degrade gracefully at
runtime (an item falls back to its raw id + the `misc` icon; a logistics group resolves to
itself and is flagged unknown), so nothing breaks -- but the Inventories page reads better
once they are curated in.

This reports what to add:

  * `WorldObject.gId` values missing from PCEdit.App.Core/Data/ItemCatalog.json
  * `unlockedGroups` entries missing from that same catalog
  * `demandGrps` / `supplyGrps` ids missing from PCEdit.App.Core/Data/LogisticsGroups.json

Curate the output into the ITEMS table in gen_catalog.py and the GROUP_IDS list in
gen_logistics_groups.py, then re-run those two scripts.

Run from the repo root:

    python tools/item-catalog/report_missing.py SAVE [SAVE ...]
"""
from __future__ import annotations

import collections
import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
ITEM_CATALOG = REPO / "PCEdit.App.Core" / "Data" / "ItemCatalog.json"
LOGISTICS_CATALOG = REPO / "PCEdit.App.Core" / "Data" / "LogisticsGroups.json"

BOM = b"\xef\xbb\xbf"
WORLD_OBJECTS, INVENTORIES, UNLOCKS = 3, 4, 0


def read_sections(path):
    raw = open(path, "rb").read()
    text = raw[len(BOM):].decode("utf-8") if raw.startswith(BOM) else raw.decode("utf-8")
    if not text.startswith("\r") or not text.endswith("\r@"):
        raise SystemExit(f"{path}: not a Planet Crafter save (framing missing)")
    return text[1:-2].split("\r@\r")


def records(section):
    section = section.strip()
    return [json.loads(r) for r in section.split("|\n")] if section else []


def comma_ids(value):
    return [part for part in (value or "").split(",") if part]


def report(title, counts, total_known):
    print(f"\n{title} -- {len(counts)} uncovered id(s), catalog holds {total_known}")
    if not counts:
        print("  (none)")
        return
    for gid, count in sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])):
        print(f"  {gid:<40} x{count}")


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)

    items = json.loads(ITEM_CATALOG.read_text(encoding="utf-8"))["items"]
    groups = json.loads(LOGISTICS_CATALOG.read_text(encoding="utf-8"))["groups"]

    missing_items = collections.Counter()
    missing_unlocks = collections.Counter()
    missing_groups = collections.Counter()

    for path in argv[1:]:
        sections = read_sections(path)
        print(f"scanned {path}")

        for world_object in records(sections[WORLD_OBJECTS]):
            gid = world_object.get("gId")
            if gid and gid not in items:
                missing_items[gid] += 1

        unlocks = records(sections[UNLOCKS])
        for unlocked in comma_ids(unlocks[0].get("unlockedGroups") if unlocks else ""):
            if unlocked not in items:
                missing_unlocks[unlocked] += 1

        for inventory in records(sections[INVENTORIES]):
            for key in ("demandGrps", "supplyGrps"):
                for group in comma_ids(inventory.get(key)):
                    if group not in groups:
                        missing_groups[group] += 1

    report("WorldObject.gId not in ItemCatalog.json", missing_items, len(items))
    report("unlockedGroups entry not in ItemCatalog.json", missing_unlocks, len(items))
    report("demandGrps/supplyGrps id not in LogisticsGroups.json", missing_groups, len(groups))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
