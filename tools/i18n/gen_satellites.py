#!/usr/bin/env python3
"""Generate satellite .resx files for PCEdit.App.Core from per-locale JSON.

Source of truth for keys and order: PCEdit.App.Core/Resources/Strings.resx (neutral).
Translations: tools/i18n/<culture>.json  ({ "Key": "translated value", ... }).

Run from the repo root:  python tools/i18n/gen_satellites.py
"""
from __future__ import annotations

import json
import re
import sys
import xml.sax.saxutils as sax
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
RES_DIR = REPO / "PCEdit.App.Core" / "Resources"
NEUTRAL = RES_DIR / "Strings.resx"
I18N_DIR = REPO / "tools" / "i18n"

CULTURES = [
    "en-GB", "fr", "de", "es-ES", "zh-Hans", "ru", "pl", "pt-PT",
    "ko", "ja", "pt-BR", "it", "zh-Hant", "tr",
]

HEADER = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
"""

DATA_RE = re.compile(
    r'<data\s+name="(?P<name>[^"]+)"[^>]*>\s*<value>(?P<value>.*?)</value>\s*</data>',
    re.DOTALL,
)
PLACEHOLDER_RE = re.compile(r"\{(\d+)\}")


def read_neutral() -> list[tuple[str, str]]:
    text = NEUTRAL.read_text(encoding="utf-8")
    pairs = [(m.group("name"), sax.unescape(m.group("value"))) for m in DATA_RE.finditer(text)]
    if not pairs:
        sys.exit("No <data> entries found in neutral Strings.resx")
    return pairs


def placeholders(s: str) -> set[str]:
    return set(PLACEHOLDER_RE.findall(s))


def main() -> int:
    neutral = read_neutral()
    neutral_map = dict(neutral)
    problems: list[str] = []

    for culture in CULTURES:
        src = I18N_DIR / f"{culture}.json"
        if not src.exists():
            problems.append(f"{culture}: missing {src.relative_to(REPO)}")
            continue

        translations = json.loads(src.read_text(encoding="utf-8"))
        missing = [k for k, _ in neutral if k not in translations]
        extra = [k for k in translations if k not in neutral_map]
        if missing:
            problems.append(f"{culture}: missing keys: {', '.join(missing)}")
        if extra:
            problems.append(f"{culture}: unknown keys: {', '.join(extra)}")

        for key, neutral_value in neutral:
            tv = translations.get(key, "")
            if not str(tv).strip():
                problems.append(f"{culture}: empty value for {key}")
            elif placeholders(neutral_value) != placeholders(str(tv)):
                problems.append(
                    f"{culture}: placeholder mismatch for {key}: "
                    f"{sorted(placeholders(neutral_value))} vs {sorted(placeholders(str(tv)))}"
                )

        lines = [HEADER]
        for key, _ in neutral:
            value = sax.escape(str(translations.get(key, "")))
            lines.append(f'  <data name="{key}" xml:space="preserve"><value>{value}</value></data>')
        lines.append("</root>\n")

        out = RES_DIR / f"Strings.{culture}.resx"
        out.write_text("\n".join(lines), encoding="utf-8")
        print(f"wrote {out.relative_to(REPO)} ({len(neutral)} entries)")

    if problems:
        print("\nISSUES:", file=sys.stderr)
        for p in problems:
            print("  " + p, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
