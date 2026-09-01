# Changelog

What changed in each release of PCEdit. Downloads for every version are on the
[Releases page](https://github.com/Valkerran/PCEdit/releases); the newest is on the
[latest release](https://github.com/Valkerran/PCEdit/releases/latest) page.

## v1.2.2

- **Fix: the AppImage's "report a bug" and homepage links pointed nowhere.** Every release
  up to v1.2.1 shipped AppStream metadata naming a repository that does not exist, so those
  links led to a 404 from the desktop entry. The application itself is unchanged from
  v1.2.1. ([#28](https://github.com/Valkerran/PCEdit/pull/28))

## v1.2.1

- **Fix: the Linux AppImage now starts on distros with no system `libicu`.** A self-contained
  build does not bundle ICU, and .NET aborts at startup when the system has none — so on
  openSUSE Tumbleweed, and on minimal or container images generally, PCEdit would not launch
  at all. The AppImage now carries its own ICU. It is larger for it: **56.5 MB, up from
  43.0 MB**. Windows and macOS are unaffected — they use the ICU in the OS.
  ([#4](https://github.com/Valkerran/PCEdit/issues/4))

## v1.2.0

- **Planet Crafter 2.102 (the *Skeo* update) is supported.** The save format barely moved:
  one new key (`logisticsPaused`) on the unlocks section, and nothing else across all ten
  sections, on Steam and Game Pass alike. Saves from 2.008 still open and still save back
  unchanged. ([#17](https://github.com/Valkerran/PCEdit/pull/17))
- **Item catalog: 278 → 466 items.** The catalog had been seeded from a single save, so
  plenty of real content showed up under its raw in-game id. Anything still unnamed remains
  editable and moves between inventories normally.

## v1.1.1

- **Fix: saves edited on Xbox / PC Game Pass no longer corrupt.** The editor was adding a
  UTF-8 byte-order mark that the Game Pass (WGS) build of the game does not write; on next
  load the game reported a "file error". The editor now preserves whatever byte framing the
  save already has (Steam saves keep their BOM). ([#13](https://github.com/Valkerran/PCEdit/issues/13))

## v1.1.0

- **Inventories — filter by world.** On a save that spans more than one planet, the Inventories
  page gains a **World** filter. Inventories that can't be matched to a planet (drones, vehicles,
  rockets, unplaced buffers) group under *Unknown world*.
- **Teleport — worlds.** Each landmark shows the planet it sits on and the list filters to the
  chosen destination world. Picking a destination world different from where the player stands
  fills X / Y / Z from that world's arrival point, and *Use current position* now restores the
  player's world as well as their coordinates.
- **Teleport — multiplayer note** about non-host players (see [Using the editor](README.md#teleport)).
- **About page** now lists the game's default save-file locations per platform.
- Fixes: *Use current position* no longer leaves a stale world selected.
