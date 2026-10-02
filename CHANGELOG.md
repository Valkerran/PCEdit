# Changelog

What changed in each release of PCEdit. Downloads for every version are on the
[Releases page](https://github.com/Valkerran/PCEdit/releases); the newest is on the
[latest release](https://github.com/Valkerran/PCEdit/releases/latest) page.

## v1.7.0

Help for saves altered by an inventory-stacking mod, which can leave containers holding far more
than they should and the game stuttering or freezing. ([#64](https://github.com/Valkerran/PCEdit/issues/64))

- **New: PCEdit warns you about over-full containers.** When you open a save, the Overview says
  how many containers hold more items than their size, and how many are close to the game's
  limit of 8,000 items in one container, past which the game drops the rest when it loads. On
  the Inventories page those containers carry an "over-full" or "near load limit" badge, and a
  **Needs attention** filter lists just them. An unmodded save shows none of this.

- **New: Repair, without touching your save.** **Repair…** on the Overview trims every container
  back to its size (or a multiple of it you choose), showing exactly what it will remove before
  it does anything. You can tick item types to keep: they move into free slots in your own
  storage crates first, and only what does not fit is removed. The result is written to **a new
  save file** beside the original, in the next free save slot so it shows up in the game; the
  save you opened is never changed, so if the repaired copy misbehaves, the original is still
  there. A copy of an Xbox / PC Game Pass save keeps that version's file format.

- **New: move part of a stack.** Moving items now asks how many to move, with an **All**
  button. Full containers are no longer offered as destinations, and one without room for
  the number you picked is greyed out.

- **New: see what you built.** Each container on the Inventories page is tagged **Built by
  you**, **Map** (placed in the game's map by the developers) or **Wreck**, and a **Built by
  you** filter lists just your own. Where the save cannot tell (a countertop, fridge or vault
  can be either built or found in a wreck), there is no tag rather than a guess.

- **Improved: big saves no longer freeze the Inventories page.** Identical items in a
  container can show as one row with a count ("Wheat x 1500"). On a modded save with 276,000
  items, opening the largest container went from a 6-second freeze to about 50 ms. A new
  **Identical items** setting chooses: *Automatic* (the default) groups only on a save with
  containers that need attention and otherwise lists every item with its id as before;
  *Grouped* and *One by one* force either. PCEdit remembers your choice.

- **Fixed:** the player vitals on the Overview (and the colour of status messages) stayed in
  the old theme's colours when the system switched between light and dark mode with PCEdit open,
  leaving the numbers nearly invisible in dark mode.

- The new text is machine-translated into the 14 other languages PCEdit ships (British English
  and 13 others).

- Applies to every platform (Linux, Windows and macOS) alike. No new dependencies, and no change
  to how PCEdit reads or writes saves: an unedited save still saves back byte for byte.

## v1.6.0

Not published as a download of its own: this change first shipped in v1.7.0.

- **Planet Crafter 2.103 is supported.** The save format did not change: a world saved by
  2.102 and then again by 2.103 has exactly the same structure, and PCEdit opens and saves 2.103
  saves back unchanged. Saves from 2.008 and 2.102 still work as before. Checked on Steam; the
  Xbox / PC Game Pass version of 2.103 has not been tested yet, but uses the same format as
  Steam in every earlier version. ([#61](https://github.com/Valkerran/PCEdit/issues/61))
- Every item in the 2.103 saves checked (Prime and Humble) already has its proper name in
  PCEdit; nothing shows up as a raw in-game id.
- **No change to the application itself**, on any platform (Linux, Windows or macOS): the
  downloads work exactly as v1.5.1 did. Only the version number moves.

## v1.5.1

- **No change to the application.** Build process only: every package the app is built from
  is now pinned, by exact version and content hash, in files kept in the repository, and the
  build fails if anything drifts from them. Before, each build fetched whatever versions
  resolved that day, so an altered or substituted dependency could have slipped in unnoticed.
  The downloads contain the same files as v1.5.0, and the Linux build still carries its own
  copy of ICU, so it starts on distros that have none.
  ([#40](https://github.com/Valkerran/PCEdit/issues/40))

## v1.5.0

- **New: inventory and item ids on the Inventories page.** Every inventory now shows its id
  under its name, and every item shows its own id beside its Move button. Before, player
  inventories had no visible id at all, and a container's name showed the container's
  *object* number, which for most containers is not its inventory id.
  ([#3](https://github.com/Valkerran/PCEdit/issues/3))

- **New: search by id.** Type an inventory or item id into the search box (with or without a
  leading `#`) and the list narrows as you type: `1014` finds only the inventories whose id,
  container number or item ids start with those digits. A search made only of digits looks
  for ids, not names. You can also search by an item's internal type name, such as `Iron` or
  `Tree12Seed`, which until now only appeared as a tooltip.

- **Improved: searching for items shows just those items.** A search that matches items
  inside a container now lists only the matching items on that container's card, with a
  "Showing 2 of 38 items" note, instead of the container's entire contents. The card's
  capacity still shows how full the container really is. Searching for the inventory itself
  (its name, world or id) still shows everything in it.

- **Improved: the Move dialog** shows each destination's inventory id and accepts the same id
  searches, so identically named containers can finally be told apart.

- Screen readers now announce the item id with each Move button, so two identical items no
  longer sound the same.

- The new and reworded text is machine-translated in all 14 non-English languages.

## v1.4.3

- **No change to the application.** The downloads are the same as v1.4.2 apart from the
  version stamp. PCEdit itself has never made a network connection of any kind, and still
  does not. This release removes one from the *build*: the Avalonia UI framework PCEdit is
  written with ships a component that reported each build to its authors (an anonymous
  machine id, your operating system, which editor you build in, and similar), and PCEdit no
  longer includes it. That component only ever ran while compiling the app from source, so
  nobody running a downloaded PCEdit was affected; now it doesn't run for anyone.

- The README has a new **Privacy** section setting out what PCEdit does and does not touch.

## v1.4.2

- **Updated the UI framework.** PCEdit now runs on Avalonia 12.1.3 (from 12.1.1), which
  brings upstream fixes to parts of the interface PCEdit relies on: stray "ghost" rows in
  lists like the Inventories page, a crash when the system switches between light and dark
  theme, and fallback fonts used for characters the main font lacks, as in the Chinese,
  Japanese and Korean translations. No PCEdit behaviour changed, and nothing about how save
  files are read or written. The supporting MVVM and dependency-injection libraries got
  patch updates too.

- No new runtime dependency, and the minimum OS is unchanged on every platform.

## v1.4.1

- **No change to the application.** Build and release process only: the pipeline that
  produces the downloads was hardened. The tool that assembles the Linux AppImage is now a
  fixed, checksum-verified release instead of whatever build was newest that day; only the
  final publishing step can write to the repository; and every build action is pinned to an
  exact version and moved off a runtime GitHub is retiring. The Windows and macOS downloads
  are the same as v1.4.0 apart from the version stamp. The AppImage is expected to behave
  identically, but it is now packed by that pinned tool.
  ([#24](https://github.com/Valkerran/PCEdit/issues/24),
  [#39](https://github.com/Valkerran/PCEdit/issues/39))

- `RELEASING.md` now says what `SHA256SUMS.txt` does and does not prove: it shows a
  download arrived intact, not who made it, and none of the builds is code-signed.

## v1.4.0

- **Improved: item names on the Inventories page now match the game.** Several hundred
  items — every tree seed, frog and fish egg, butterfly larva, and spacesuit skin among
  them — previously shared one generic name per family (e.g. every tree seed just said
  "Tree Seed"); each now shows its real, distinct in-game name. A further 92 items that
  previously showed as a raw internal id are now named and iconed correctly, including
  content from the Toxicity DLC, the Moons Update, and the 2.1 Skeo Update.
  ([#25](https://github.com/Valkerran/PCEdit/issues/25),
  [#19](https://github.com/Valkerran/PCEdit/issues/19))

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
