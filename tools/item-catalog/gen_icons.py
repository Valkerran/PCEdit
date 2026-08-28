#!/usr/bin/env python3
"""Generate the category icon SVGs for PCEdit.Desktop/Assets/Icons/.

Each icon is a 64x64 rounded tile in a per-category colour with a simple white
pictogram, so it reads on both the light and dark inventory-card backgrounds and
mirrors the game's item-tile look. These SVGs are the source; the committed
<name>.png beside each one is rasterised from it out of band.
"""
import os

OUT = "PCEdit.Desktop/Assets/Icons"

# name -> (background colour, inner glyph markup drawn in white on a 64x64 canvas)
ICONS = {
    "cat_ore": ("#5C6BC0",
        '<path d="M32 12 51 23V41L32 52 13 41V23Z" fill="#fff"/>'
        '<path d="M32 12 51 23 32 34 13 23Z" fill="#fff" opacity="0.65"/>'),
    "cat_gem": ("#AB47BC",
        '<path d="M20 16H44L54 30 32 54 10 30Z" fill="#fff"/>'
        '<path d="M10 30H54L32 54Z" fill="#fff" opacity="0.55"/>'),
    "cat_component": ("#26A69A",
        '<path d="M32 20a12 12 0 100 24 12 12 0 000-24Zm0 8a4 4 0 110 8 4 4 0 010-8Z" fill="#fff"/>'
        '<g fill="#fff"><rect x="29" y="8" width="6" height="10" rx="1"/>'
        '<rect x="29" y="46" width="6" height="10" rx="1"/>'
        '<rect x="8" y="29" width="10" height="6" rx="1"/>'
        '<rect x="46" y="29" width="10" height="6" rx="1"/></g>'),
    "cat_seed": ("#66BB6A",
        '<path d="M32 12c10 8 14 18 14 26a14 14 0 01-28 0c0-8 4-18 14-26Z" fill="#fff"/>'),
    "cat_plant": ("#9CCC65",
        '<rect x="30" y="30" width="4" height="24" fill="#fff"/>'
        '<path d="M32 34C22 34 14 26 14 16c10 0 18 8 18 18Z" fill="#fff"/>'
        '<path d="M32 30c10 0 18-8 18-18-10 0-18 8-18 18Z" fill="#fff"/>'),
    "cat_food": ("#FFA726",
        '<path d="M32 18c8-8 22-6 22 8 0 12-14 24-22 28-8-4-22-16-22-28 0-14 14-16 22-8Z" fill="#fff"/>'),
    "cat_larva": ("#EC407A",
        '<ellipse cx="32" cy="34" rx="13" ry="19" fill="#fff"/>'
        '<g stroke="#EC407A" stroke-width="3"><path d="M21 26h22"/><path d="M20 34h24"/><path d="M21 42h22"/></g>'),
    "cat_structure": ("#78909C",
        '<g fill="#fff"><rect x="10" y="34" width="20" height="20"/>'
        '<rect x="34" y="20" width="20" height="34"/>'
        '<rect x="18" y="12" width="12" height="12"/></g>'),
    "cat_furniture": ("#8D6E63",
        '<g fill="#fff"><rect x="16" y="14" width="8" height="26" rx="2"/>'
        '<rect x="16" y="34" width="30" height="8" rx="2"/>'
        '<rect x="18" y="42" width="6" height="12"/><rect x="38" y="42" width="6" height="12"/></g>'),
    "cat_equipment": ("#42A5F5",
        '<path d="M32 10 52 18v14c0 12-8 20-20 24-12-4-20-12-20-24V18Z" fill="#fff"/>'
        '<path d="M32 22a7 7 0 100 14 7 7 0 000-14Z" fill="#42A5F5"/>'),
    "cat_consumable": ("#EF5350",
        '<path d="M27 10h10v14l10 22a6 6 0 01-5 9H22a6 6 0 01-5-9l10-22Z" fill="#fff"/>'
        '<rect x="24" y="8" width="16" height="5" rx="2" fill="#fff"/>'),
    "cat_rocket": ("#FF7043",
        '<path d="M32 8c9 6 13 16 13 27l-4 8H23l-4-8c0-11 4-21 13-27Z" fill="#fff"/>'
        '<path d="M23 43l-7 11 11-4M41 43l7 11-11-4" fill="#fff"/>'
        '<circle cx="32" cy="27" r="5" fill="#FF7043"/>'),
    "cat_token": ("#FFCA28",
        '<circle cx="32" cy="32" r="21" fill="#fff"/>'
        '<path d="M32 19l4 9 10 1-7 7 2 10-9-5-9 5 2-10-7-7 10-1Z" fill="#FFCA28"/>'),
    "cat_chip": ("#26C6DA",
        '<rect x="18" y="18" width="28" height="28" rx="3" fill="#fff"/>'
        '<rect x="26" y="26" width="12" height="12" rx="2" fill="#26C6DA"/>'
        '<g fill="#fff"><rect x="24" y="8" width="4" height="8"/><rect x="36" y="8" width="4" height="8"/>'
        '<rect x="24" y="48" width="4" height="8"/><rect x="36" y="48" width="4" height="8"/>'
        '<rect x="8" y="24" width="8" height="4"/><rect x="8" y="36" width="8" height="4"/>'
        '<rect x="48" y="24" width="8" height="4"/><rect x="48" y="36" width="8" height="4"/></g>'),
    "cat_container": ("#A1887F",
        '<path d="M12 20h40v32H12Z" fill="#fff"/>'
        '<path d="M12 20h40M32 20v32M12 32h40" stroke="#A1887F" stroke-width="3"/>'),
    "cat_vehicle": ("#7E57C2",
        '<circle cx="20" cy="42" r="9" fill="#fff"/><circle cx="44" cy="42" r="9" fill="#fff"/>'
        '<path d="M10 34l6-14h24l10 14Z" fill="#fff"/>'),
    "cat_misc": ("#90A4AE",
        '<path d="M24 26a8 8 0 1116 0c0 5-6 6-8 10" stroke="#fff" stroke-width="6" '
        'fill="none" stroke-linecap="round"/>'
        '<circle cx="32" cy="48" r="4" fill="#fff"/>'),
}

SVG = ('<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" '
       'viewBox="0 0 64 64"><rect width="64" height="64" rx="14" fill="{bg}"/>'
       '{glyph}</svg>\n')


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (bg, glyph) in ICONS.items():
        path = os.path.join(OUT, name + ".svg")
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(SVG.format(bg=bg, glyph=glyph))
    print(f"wrote {len(ICONS)} icons to {OUT}")


if __name__ == "__main__":
    main()
