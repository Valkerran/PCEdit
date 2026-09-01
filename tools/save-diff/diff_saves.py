#!/usr/bin/env python3
"""Diff two Planet Crafter save files at the framing / section / key level.

Answers the question a new game version raises: *did the save format change, and where?*
Reports BOM presence, section count, per-section record counts, JSON keys added or removed
per section, and a full value diff of the three single-object sections (0 unlocks,
5 statistics, 8 metadata).

Works on a Steam save (`Standard-2.json`, BOM) and on a raw Xbox / PC Game Pass (WGS) blob
(GUID-named, no BOM) alike -- the framing is identical, only the BOM differs.

Run from the repo root:

    python tools/save-diff/diff_saves.py OLD_SAVE NEW_SAVE

See tools/save-diff/README.md for the recommended before/after folder layout.
"""
from __future__ import annotations

import json
import sys

BOM = b"\xef\xbb\xbf"

# Section index -> name. Mirrors PlanetCrafterSaveFileSerializer / PlanetCrafterSaveFile.
SECTIONS = [
    "Unlocks", "Terraformation", "Players", "WorldObjects", "Inventories",
    "Statistics", "ReadMessages", "StoryEvents", "Metadata", "ProceduralInstances",
]
SINGLE_OBJECT_SECTIONS = (0, 5, 8)


def read(path):
    """Return (has_bom, [section_text, ...]) after stripping the game's framing."""
    raw = open(path, "rb").read()
    has_bom = raw.startswith(BOM)
    text = raw[len(BOM):].decode("utf-8") if has_bom else raw.decode("utf-8")
    if not text.startswith("\r") or not text.endswith("\r@"):
        raise SystemExit(f"{path}: not a Planet Crafter save (framing missing)")
    return has_bom, text[1:-2].split("\r@\r")


def records(section):
    section = section.strip()
    return section.split("|\n") if section else []


def keys_of(section):
    """Every distinct JSON key used by the records in one section."""
    found = set()
    for record in records(section):
        found.update(json.loads(record).keys())
    return found


def main(argv):
    if len(argv) != 3:
        raise SystemExit(__doc__)
    old_path, new_path = argv[1], argv[2]
    old_bom, old = read(old_path)
    new_bom, new = read(new_path)

    print(f"OLD  {old_path}")
    print(f"NEW  {new_path}")
    print(f"BOM      {old_bom} -> {new_bom}"
          + ("   *** CHANGED ***" if old_bom != new_bom else ""))
    print(f"sections {len(old)} -> {len(new)}"
          + ("   *** CHANGED ***" if len(old) != len(new) else ""))
    print()

    changed = False
    for index in range(max(len(old), len(new))):
        name = SECTIONS[index] if index < len(SECTIONS) else f"UNKNOWN-{index}"
        old_section = old[index] if index < len(old) else ""
        new_section = new[index] if index < len(new) else ""
        old_keys, new_keys = keys_of(old_section), keys_of(new_section)
        added, removed = sorted(new_keys - old_keys), sorted(old_keys - new_keys)
        counts = f"{len(records(old_section))} -> {len(records(new_section))} records"
        if added or removed:
            changed = True
            print(f"[{index}] {name}: {counts}")
            if added:
                print(f"      keys added:   {added}")
            if removed:
                print(f"      keys removed: {removed}")
        else:
            print(f"[{index}] {name}: {counts}, same keys")

    print()
    for index in SINGLE_OBJECT_SECTIONS:
        old_object = json.loads(old[index].strip())
        new_object = json.loads(new[index].strip())
        for key in sorted(set(old_object) | set(new_object)):
            before = old_object.get(key, "<MISSING>")
            after = new_object.get(key, "<MISSING>")
            if before != after:
                changed = True
                print(f"[{index}] {SECTIONS[index]}.{key}")
                print(f"      old {json.dumps(before, ensure_ascii=False)[:300]}")
                print(f"      new {json.dumps(after, ensure_ascii=False)[:300]}")

    if not changed:
        print("No schema or single-object value differences.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
