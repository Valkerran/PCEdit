# Changelog

What changed in each release of PCEdit. Downloads for every version are on the
[Releases page](https://github.com/Valkerran/PCEdit/releases); the newest is on the
[latest release](https://github.com/Valkerran/PCEdit/releases/latest) page.

## v1.3.0

- **New: PCEdit now keeps a copy of your save from before it edited it.** The first time
  PCEdit saves a file after you open it, the original is copied into a folder of its own
  first — `%LocalAppData%\PCEdit\backups` on Windows, `~/.local/share/PCEdit/backups` on
  Linux, `~/Library/Application Support/PCEdit/backups` on macOS. The five most recent
  copies of each save file are kept, and the folder is shown on the About page. Treat it as
  a safety net rather than a replacement for your own backup: the copy is taken once per
  file you open rather than once per save, and if it cannot be written the save still goes
  ahead. ([#36](https://github.com/Valkerran/PCEdit/issues/36))

- **Fix: an interrupted save can no longer destroy the file it was writing.** PCEdit wrote
  straight over your save, which clears the file before the new contents arrive — so a
  crash, a power cut or a full disk part-way through left nothing behind. Edits are now
  written to a temporary file and swapped in once complete, so the save on disk is always
  either wholly the old one or wholly the new one. ([#36](https://github.com/Valkerran/PCEdit/issues/36))

- **Fix: a save with an `@` in a container label or a player name now opens.** The `@`
  character separates the sections of a save file, and PCEdit did not tell the difference
  between one of those and one you had typed into a sign or a container name — so such a
  save either refused to open or, in one case, opened with part of it quietly missing and
  that shortened version written back on the next save. ([#38](https://github.com/Valkerran/PCEdit/issues/38))

- **Fix: a corrupt or hand-edited save no longer closes the app.** Unreadable values in a
  save could bring PCEdit down when you opened the Inventories or Teleport page — after the
  file had loaded, taking any unsaved edits with it. Entries PCEdit cannot read are now
  skipped rather than fatal, and they are left untouched in the file rather than dropped
  when you move an item. If a page still cannot be built, it says so instead of closing.
  ([#37](https://github.com/Valkerran/PCEdit/issues/37))

- **Fix: granting a very large number of terra tokens no longer leaves a negative balance.**
  The total is capped at what the save format can actually hold, and the confirmation now
  reports what was granted rather than what was asked for. ([#42](https://github.com/Valkerran/PCEdit/issues/42))

- **The "use at your own risk" notice now mentions the automatic copy**, so it appears once
  more even if you had already dismissed it.

- Error details are no longer discarded in release builds, so a failed load or save leaves
  something behind to diagnose. ([#41](https://github.com/Valkerran/PCEdit/issues/41))

## v1.2.3

- **No change to the application.** Documentation and release process only, so the
  binaries are identical to v1.2.2 apart from the version stamp: the release history
  moved out of the README into this file, the Linux distro test matrix was completed
  against the published v1.2.2 AppImage (six distros, glibc 2.31 to 2.44, system ICU 66
  to 78 - all passing), and the release checklist now requires a changelog entry with
  every version bump.

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
