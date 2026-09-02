# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

PCEdit is a save-file editor for the game *The Planet Crafter*: open a save, view players / world
terraforming / inventories, move items between inventories, grant terra tokens, edit terraform
levels, and teleport a player.

| Project | TFM | Role |
|---|---|---|
| **PCEdit.SaveFileHandler** | `net10.0` | Dependency-free library: parse, edit, re-serialize save files. The real work. |
| **PCEdit.App.Core** | `net10.0` | Portable app layer behind the UI head: ViewModels, services, view-models-for-view, the item catalog, localization, and the platform-abstraction interfaces. No UI-framework dependency. |
| **PCEdit.Desktop** | `net10.0` | The UI head (Linux + Windows + macOS) — Avalonia. Published as a Linux AppImage. |
| **PCEdit.SaveFileHandler.Tests** | `net10.0` | xUnit — serializer / round-trip. |
| **PCEdit.App.Core.Tests** | `net10.0` | xUnit — ViewModels, services, localization-catalog parity. |

`PCEdit.App.Core` keeps a strict UI-framework-agnostic boundary (platform concerns sit behind
interfaces) — a legacy of when a second, MAUI mobile head shared it. That head has been removed;
Avalonia is now the only UI.

**License**: GNU GPL v3-or-later (`LICENSE` at the repo root). The SPDX id `GPL-3.0-or-later` is
set once in `Directory.Build.props` (`<PackageLicenseExpression>`, `<Copyright>`) and mirrored in
`deploy/pcedit.pupnet.conf` (`AppLicenseId`) and the generated AppStream `<project_license>`
(`tools/i18n/gen_metainfo.py`). Keep those in sync. GPL headers are **not** applied per source
file — the `LICENSE` file plus the README section cover the whole repo.

## Working on a change

This is the flow for **any feature, behaviour change, or bug fix**. It does not apply to
answering a question or read-only investigation — those need no plan and no branch.

### 1. Plan first, in phases

Investigate the real code and data before proposing anything, then write a **multi-phase plan**.
Each phase must end in a working, committable state — tests green, nothing half-applied — so the
work can stop or be reviewed at any phase boundary.

The plan states: why the change is being made, what each phase does, the specific files it
touches, existing helpers it reuses, and how to verify the result end to end. Where a decision is
open, **give a recommendation** — name the option to take and why, rather than listing choices
neutrally. Flag anything genuinely blocked on information only the user has, finish everything
that is not, and say plainly what was left out.

### 2. Stress-test the plan before it is accepted

Run the `grill-me` skill (or an equivalent interrogation) on the draft plan so each branch of the
decision tree is challenged and assumptions surface while they are still cheap to change. Raise
concerns with the request as specified at this point, not after the code is written. If the user
reaffirms the request, that is the decision — build it in full.

### 3. Branch only once the plan is accepted

Take a fresh branch from an **up-to-date** `main`. Never commit directly to `main`.

```bash
git checkout main && git pull --ff-only && git checkout -b <topic-branch>
```

If `main` cannot fast-forward, stop and resolve that before branching.

### 4. Commit at the end of every phase

One commit per completed phase, not one commit for the whole plan. Before each commit: build,
run both test projects, and regenerate any generated file the phase touched — the localization
satellites and AppStream metainfo via `tools/i18n/` (CI **fails** on drift here), the item and
logistics catalogs via `tools/item-catalog/` (no CI guard — only a hand-edited `gen_*.py`
followed by a re-run keeps the JSON honest). Commit messages say what changed and why; see
[Git commit conventions](#git-commit-conventions) for the trailer rule.

### 5. Bump the version and write the changelog entry, before opening the PR

The final phase bumps `<VersionPrefix>` in the repo-root `Directory.Build.props` (semver:
patch for a bug fix, minor for a feature or new game-version support, major for a breaking
change). Doing it inside the PR is what leaves `main` immediately releasable after the merge —
Actions → **Release** → *Run workflow* reads `<VersionPrefix>`, creates the matching `vX.Y.Z`
tag and publishes the artifacts, with no follow-up commit needed.

CI's `version-guard` job fails if `<VersionPrefix>` is malformed or *behind* the newest release
tag, and the Release workflow refuses to build if a pushed tag disagrees with it. The full
release procedure is in `RELEASING.md`.

**The same phase adds the entry to `CHANGELOG.md`** — a `## vX.Y.Z` section for the version
being bumped to, at the top of the list, in the voice of the existing entries: what changed
for someone *using* the app, not what changed in the code. A version bump carrying
user-visible change but no entry is an incomplete phase: the next release ships straight from
`main`, and no later step would catch the omission — which is exactly how v1.2.0 and v1.2.1
came to ship undocumented.

The exception is a bare *bump for development* after a release (`RELEASING.md` step 5), which
carries nothing yet — its entry arrives with the change that fills the version.

Call out anything user-visible beyond the fix itself: a size change, a new or dropped runtime
dependency, a renamed artifact, a raised minimum OS — and say which platforms are *not*
affected. `RELEASING.md` step 4 mirrors the entry into the GitHub Release body.

## Commands

```bash
# Fast inner loop — the library only, no workloads:
dotnet build PCEdit.SaveFileHandler/PCEdit.SaveFileHandler.csproj

# Tests:
dotnet test PCEdit.SaveFileHandler.Tests/PCEdit.SaveFileHandler.Tests.csproj
dotnet test PCEdit.App.Core.Tests/PCEdit.App.Core.Tests.csproj

# Run the desktop app (Linux, Windows, macOS):
dotnet run --project PCEdit.Desktop/PCEdit.Desktop.csproj

# Package the desktop head (self-contained; version defaults to Directory.Build.props <VersionPrefix>):
deploy/build-appimage.sh          # Linux AppImage   (toolchain + glibc rule in deploy/README.md)
deploy/build-windows.ps1          # Windows win-x64 zip
deploy/build-macos.sh osx-arm64   # macOS .app zip   (osx-x64 | osx-arm64)
```

`dotnet build PCEdit.slnx` builds everything. `global.json` pins the SDK feature band (`10.0.4xx`).

**Versioning**: the repo-root `Directory.Build.props` `<VersionPrefix>` is the single source of
truth for every project's version. See `RELEASING.md` for the bump→merge→tag flow and the rules
the pipelines enforce.

CI (`.github/workflows/ci.yml`) runs the two test projects, builds `PCEdit.Desktop`, fails if the
generated localization files are out of sync with the catalog, and (`version-guard` job) fails if
`<VersionPrefix>` is malformed or behind the latest release tag. `release.yml` runs on a `vX.Y.Z`
tag push **or** manual dispatch from `main` (which creates the tag); it refuses to build if the tag
and `<VersionPrefix>` disagree, then builds the AppImage (`ubuntu-22.04`), the Windows zip
(`windows-latest`), and the macOS `.app` zips for `osx-x64` + `osx-arm64` (`macos-latest`, unsigned)
and attaches them plus `SHA256SUMS.txt` to a GitHub Release.

## Architecture: the save-file format

A Planet Crafter save file is a single UTF-8 text file, not JSON overall. The framing (see
`PlanetCrafterSaveFileSerializer`): a leading `\r`, then the 10 sections (index 0–9) joined by
`\r@\r`, then a trailing `\r@`. A section holds either one JSON object or a list of JSON objects
joined by `|\n`. The **Steam** build writes the file with a leading UTF-8 BOM; the **Xbox / PC
Game Pass** (WGS) build writes it without one — a spurious BOM makes the game reject the save
("file error"), so `PlanetCrafterSaveFileStore.Save` preserves whatever the file already on disk
has (BOM only for a brand-new path). `Serialize` returns everything after the BOM. Reproduce this
exactly — an unedited load→save is asserted byte-identical, so a diff after an edit shows only the edit.
`PlanetCrafterSaveFileSerializer` (`PCEdit.SaveFileHandler/PlanetCrafterSaveFileSerializer.cs`)
hard-codes the section order — keep it in sync with `PlanetCrafterSaveFile`
(`PCEdit.SaveFileHandler/Models/PlanetCrafterSaveFile.cs`):

| # | Section | Model |
|---|---------|-------|
| 0 | Unlocks | `SaveFileUnlocks` (single object) |
| 1 | Terraformation | `List<PlanetTerraformation>` (one entry per planet) |
| 2 | Players | `List<PlayerData>` |
| 3 | World objects | `List<WorldObject>` |
| 4 | Inventories | `List<Inventory>` |
| 5 | Statistics | `SaveFileStatistics` (single object) |
| 6 | Read messages | `List<ReadMessage>` |
| 7 | Story events | `List<StoryEvent>` |
| 8 | Metadata | `SaveFileMetadata` (single object) |
| 9 | Procedural instances | `List<ProceduralInstance>` |

`PCEdit.SaveFileHandler/Standard-2.json` (despite the `.json` extension) is a real sample save file
in this format — a hand-maintained reference/fixture. Do not let a load→save round-trip overwrite it.
`PCEdit.SaveFileHandler/mini-save.json` is a tiny hand-authored save in the real framing (BOM,
`\r@\r`, `|\n`) exercising the rarer object shapes (ore vein `count`, `ToxicWaterCollector`
`linkedWo`, a labelled container `text`, a logistics container, a deliberately-unknown key); `PCEdit.SaveFileHandler.Tests` links it into `TestData/`. Regenerate it with a byte-writing script, not an editor —
it has no trailing newline and must keep its exact bytes (`.gitattributes` marks it `-text`).

Two more real fixtures cover **game version 2.102**, both byte-exact and both `-text`:

| Fixture | Game | Platform | BOM | Shape |
|---|---|---|---|---|
| `Standard-2.json` | 2.008 | Steam | yes | single planet (Prime), the backward-compat regression |
| `mini-save.json` | 2.008 | hand-authored | yes | the rare object shapes + an unknown key |
| `Humble-2.102.json` | 2.102 | Steam | yes | single planet (Humble) |
| `Interplanetary-2.102.json` | 2.102 | Xbox / PC Game Pass (WGS) | **no** | Prime + Aqualis + Selenea |

`Interplanetary-2.102.json` is a raw WGS blob exactly as the game wrote it, so it is the only
fixture that proves the BOM-less Game Pass path end-to-end on real bytes
(`Save_OverARealBomLessGamePassSave_KeepsItBomLess`) — **it must never gain a BOM**. It is also
the only multi-planet fixture, so it backs the `PlanetHash`/`PlanetIndex` hash-to-planet bridge
against real data. Note it cannot join
`SaveThenLoad_OfAnUnchangedSave_IsByteIdenticalOnDisk`: that theory saves to a path that does not
exist yet, which is "Save As" and correctly emits a BOM.

**Game versions.** The save format was unchanged from 2.008 to 2.102 ("Skeo" update) apart from a
single key: `logisticsPaused` on the unlocks section. It is modelled as
`SaveFileUnlocks.LogisticsPaused` and is **`bool?`, not `bool`, on purpose** — the serializer
ignores nulls when writing, so a pre-2.102 save that never carried the key does not gain a
`"logisticsPaused":false` on save and keeps round-tripping byte-for-byte. Apply the same rule to
any future version-added field. `tools/save-diff/diff_saves.py` produces this comparison for a new
game build (see `tools/save-diff/README.md`); `tools/item-catalog/report_missing.py` then lists the
content ids the app's catalogs do not cover yet.

Layering, each with a small interface for testability/DI:

- `IJsonRecordSerializer` / `JsonRecordSerializer` — wraps `System.Text.Json` with camelCase naming and `WhenWritingNull` ignore semantics; deserializes/serializes a single record (section or list item) and wraps `JsonException` in `InvalidDataException` with the offending section index. Also holds `GameDecimalConverter` (the game writes every decimal with a fractional part — force `N.0`, never `N`). Every leaf model has a `[JsonExtensionData] ExtensionData` dictionary, so a JSON key no model names is **preserved** across a round-trip, not silently dropped. `WorldObject` goes further: the game's world-object key order is not stable (proven — the same pair of keys appears in both orders across records), so `WorldObjectConverter` records each record's key order on read and replays it on write, keeping unknown keys in place too. Tests: `RoundTrip_RealSampleSaveFile_ReserializesCharacterForCharacter` (whole-file, string-exact) and `PlanetCrafterSaveFileStoreTests.SaveThenLoad_...IsByteIdenticalOnDisk` (with BOM); `WorldObjectConverterTests` / `GameDecimalFormatTests` for the pieces; `GameFormatKeyMappingTests` pins the abbreviated-key spellings a symmetric model round-trip can't catch.
- `IPlanetCrafterSaveFileSerializer` / `PlanetCrafterSaveFileSerializer` — splits/joins the 10 sections (framing above), delegating record-level (de)serialization to `IJsonRecordSerializer`. `Deserialize` is lenient about whitespace/line-endings; `Serialize` reproduces the game framing exactly. The section split matches an `@` **only when the framing line breaks bracket it** — an `@` inside a JSON string is player-typed free text (a container/sign label, a player name) and must not split the file; splitting on the bare character truncated those saves, and in one placement dropped data with no error at all (#38). Do not simplify it back to `Split('@')`.
- `IPlanetCrafterSaveFileStore` / `PlanetCrafterSaveFileStore` — file I/O (`Load`/`Save` by path). `Load` uses `File.ReadAllText` (strips any BOM); `Save` re-checks the target file's first bytes and writes UTF-8 with a BOM only if the existing file had one (or the path is new) — matching Steam-with-BOM and Game-Pass-without. It writes through a sibling `.pcedit-tmp` file, flushed to disk, then swaps it in with an atomic `File.Move(overwrite: true)`, so an interrupted write can never leave a truncated save (#36); the BOM probe therefore has to read the **original** path, before the swap, not the temp file.

Model conventions worth knowing before adding/editing a model in `PCEdit.SaveFileHandler/Models/`:
- Fields that are always present in the save file are `required` properties; fields the game may omit are nullable (`decimal?`, `string?`, etc.) — get this right or `Save`/round-trip will crash or lose data.
- Most JSON property names differ from the C# names only in that the JSON uses camelCase — `System.Text.Json`'s `CamelCase` naming policy handles that automatically. Only use an explicit `[JsonPropertyName]` when the JSON key is actually abbreviated/irregular (e.g. `pos`/`rot`/`liId`/`pnls`/`count`/`linkedWo` on `WorldObject`, `woIds`/`demandGrps`/`supplyGrps` on `Inventory`). When you add a model property, verify the real key against `Standard-2.json` / `mini-save.json` — a wrong `[JsonPropertyName]` no longer loses data (the `ExtensionData` catch-all preserves the bytes) but it does mean the property never populates. **`WorldObject` is serialized by `WorldObjectConverter`, not attribute-driven** — a new key needs a `case` in both its `Read` switch and `WriteKey`, plus an entry in `HasValue` and `DefaultOrder` (or just leave it to `ExtensionData`, which the converter still positions correctly).

**A critical consequence for editing code**: every model in `PCEdit.SaveFileHandler.Models`
(including the root `PlanetCrafterSaveFile`) is a `sealed record` with `{ get; init; }` properties —
use a `with` expression to change one field, never hand-copy every property (that is how
`demandGrps`/`supplyGrps` came to be dropped on every save). `PlanetCrafterSaveFile.Unlocks`/
`.Metadata`/`.Statistics` are singular `required init`-only root properties, so changing one is
`save with { Unlocks = … }` — see `SaveFileWorkspace.MutateUnlocks`.
`Terraformations`/`Players`/`WorldObjects`/`Inventories`/etc. are `List<T>` — the list *instance*
stays mutable after construction even though the property is `init`-only, so editing one
planet/player/inventory just replaces that element in the existing list
(`SaveFileWorkspace.ReplaceTerraformation`/`ReplacePlayer`/`ReplaceInventory`, matched by
`PlanetId`/`Id`/`Id` respectively) — no root rebuild needed there.
- Record value-equality now includes the `ExtensionData` dictionary, which compares by reference —
  so two models parsed from identical bytes are not `==`. Nothing in the codebase compares models;
  don't start.

## Architecture: PCEdit.App.Core (shared app layer)

Everything that is not a specific UI framework lives here. The UI head is thin: a set of views plus
implementations of a handful of interfaces.

**Mutation is centralized in `Services/ISaveFileWorkspace` (`SaveFileWorkspace`)** — the DI singleton
holding the loaded `PlanetCrafterSaveFile`, its path, `IsDirty`, and `SaveStatus`. ViewModels never
build a modified model themselves; they call `MutateUnlocks` / `ReplaceTerraformation` /
`ReplacePlayer` / `ReplaceInventory` / `GrantTerraTokens`, which apply the
root-rebuild-vs-list-replace pattern above and flip `IsDirty`.

**Page ViewModels re-read workspace state in a `Load()` method** (`ViewModels/ILoadable`) rather than
caching it, so switching pages always reflects the latest in-memory edits. Page ViewModels are
`Singleton` and the shell calls `Load()` only when a workspace-revision counter changed since that
page last loaded (see `PCEdit.Desktop/ViewModels/MainWindowViewModel`).

**`Services/IInventoryEditor` (`InventoryEditor`)** holds inventory-item domain logic: an item is "in"
an inventory when its `WorldObject.Id` appears in that `Inventory.WorldObjectIds` comma-string
(`Services/WorldObjectIdsCodec`); `TryMoveItem` removes from source before adding to destination and
rejects a move into an inventory already at `Inventory.Size`. `BuildInventoryGroups` is O(n) — it
pre-indexes world objects and container→inventory links (a real save has ~500 inventories /
~5000 world objects) and tags each `InventoryGroup` with an `InventoryKind` for the page's type
filter and a `PlanetId` (via `IPlanetIndex`) for its world filter. `Services/PositionCodec` handles
the `"x,y,z"` string shared by `PlayerData.PlayerPosition` / `WorldObject.Position`.

**`Services/IPlanetIndex` (`PlanetIndex`)** resolves the worlds in a save: `KnownPlanetIds()` is the
ordered union of every `PlanetId` (metadata + terraformations + players), and `ResolvePlanetId(int?)`
maps a `WorldObject.Planet` hash back to one of them through `PlanetHash.Of`. It backs both the
Inventories "World" filter and the Teleport landmark-by-world filter; the `TeleportViewModel` planet
dropdown also sources its list here.

`OverviewViewModel.Players` exposes `PlayerOverviewRow` records (location / progress are
pre-formatted single sentences via `ILocalizer.Format` — no fragment concatenation in XAML).
`OverviewViewModel.Terraforms` holds one `PlanetTerraformViewModel` per `save.Terraformations` entry,
rendered as an accordion (header = planet id + a chevron rotated by `ChevronRotation`).
`PlanetTerraformViewModel` treats `UnitPurificationLevel == -1` as "not applicable to this planet"
(`HasPurification`) — hides that row and preserves the `-1` on apply.

Every `ILoadable` page VM (Overview / Inventories / TerraTokens / Teleport) takes `INavigationService`
and exposes an `OpenFileCommand` for its "no file loaded" empty state; `TeleportViewModel` also has
`UseCurrentPositionCommand` (re-reads the selected player's position into X/Y/Z).

Note: `WorldObject.Planet` (an int hash-hint) and the string `PlanetId` used everywhere else
(`PlayerData` / `PlanetTerraformation` / `SaveFileMetadata`) are **the same identity in two
encodings** — `WorldObject.Planet == PlanetHash.Of(planetId)`, the game's Unity
`GetStableHashCode` (`PCEdit.SaveFileHandler/PlanetHash.cs`). `Services/IPlanetIndex` (`PlanetIndex`)
uses that bridge to resolve a `WorldObject.Planet` back to a known planet id: it powers
`InventoryGroup.PlanetId` (the Inventories page "World" filter, shown only on multi-world saves) and
the Teleport page's per-world landmark filter. Not every `WorldObject` carries `planet` — placed
top-level objects (pods, teleporters, containers) do; items inside inventories and child objects
usually don't, so an inventory's world is derived from its owner (player `PlanetId` or owning
container's `Planet`) and is `null` for orphan inventories.

### Platform-abstraction interfaces (`PCEdit.App.Core/Services/` + `Localization/`)

| Interface | Avalonia impl |
|---|---|
| `IFilePickerService` | `AvaloniaFilePickerService` (`IStorageProvider`) |
| `INavigationService` | `AvaloniaNavigationService` (drives `MainWindowViewModel` + a modal `Window`) |
| `IDialogService` | `AvaloniaDialogService` (`MessageDialog` window) |
| `IScreenReaderAnnouncer` | `AvaloniaScreenReaderAnnouncer` (hidden live-region `TextBlock`) |
| `IAppVersionInfo` | `AvaloniaAppVersionInfo` |
| `ILanguageStore`, `IDisclaimerGate` | `JsonSettingsStore` (`~/.config/PCEdit/settings.json`) |

The head wires these in its composition root (`PCEdit.Desktop/App.axaml.cs`), registering the Core
services as singletons and page ViewModels per the pattern above. The interfaces stay UI-agnostic so
`PCEdit.App.Core` never takes an Avalonia dependency.

`Presentation/VitalStatus` + `Presentation/StatusPalette` hold the classify-a-value and
value→resource-key logic; each head has a thin `IValueConverter` wrapper over them.

## Architecture: the Avalonia head (`PCEdit.Desktop`)

`Program.cs` → `App.axaml(.cs)` builds an `IServiceProvider`, applies the stored culture, then shows
`MainWindow`. `ViewLocator` maps `PCEdit.App.Core.ViewModels.XxxViewModel`
→ `PCEdit.Desktop.Views.XxxView` and **caches the view per (singleton) ViewModel instance** so
re-navigation doesn't rebuild the visual tree.

`MainWindow.axaml` is a `SplitView`: nav pane (`ListBox Classes="nav"` bound to
`MainWindowViewModel.SelectedNavItem`, `TwoWay`; the selected row shows the `TerraformSpectrum`
gradient on its leading edge over a faint tint), a 15-locale language `ComboBox`, and a footer with
the loaded-file path. `SplitView.Content` is a `Grid`: row 0 a **header bar** (current page title +
the primary **Save** button with `HotKey="Ctrl+S"` + save/dirty state), row 1 a `ContentControl`
bound to `CurrentPage`. `MainWindow.axaml.cs` handles `Closing`: if `Workspace.IsDirty` it cancels
the close and shows the `Quit_Discard*` confirm dialog. On first open the disclaimer dialog shows if
`!IDisclaimerGate.HasAcknowledged`; the other page views are pre-built at `Background` priority so
first navigation isn't a stall.

Palette / theme: `App.axaml` defines colour tokens (`SurfacePage`, `SurfaceCard`, `BrandFill`,
`Status*Text`, …) in `ResourceDictionary.ThemeDictionaries` (`Light` + `Dark`) — a Planet-Crafter
terraforming palette (rust world → blue sky → green biosphere; brand accent is a muted biosphere
green). `RequestedThemeVariant="Default"` so the desktop head follows the OS theme. The token **keys**
are referenced from `PCEdit.App.Core` (`StatusPalette` / the value converters) — keep them stable.
`Styles/Controls.axaml` holds the type scale (`h1` hero / `pageHeading` / `h2` section / `caption` /
`micro`) and `Border.headingRule` (the green bar left of every section heading). Inter is the
app-wide font (`Program.cs` `WithInterFont()`).

XAML notes:
- Avalonia 12 uses `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` (**not** `2009`).
- Compiled bindings are **off** (`AvaloniaUseCompiledBindingsByDefault=false`) — reflection bindings.
- `<AssemblyName>PCEdit</AssemblyName>`, so asset URIs are `avares://PCEdit/...` and the published
  executable is `PCEdit` (which is what the AppImage packaging expects).
- The Inventories list virtualizes (a `ListBox`, **not** wrapped in a `ScrollViewer`) — a wrapping
  scroll viewer gives it infinite height and defeats virtualization. The page has a search box +
  type-filter radio group above it; the VM (`InventoriesViewModel`) filters a prebuilt
  `_allGroups` into `Groups` in memory (`InventoryGroup.Matches` / `.Kind`).
- `TextBox` placeholder is `PlaceholderText` (not the obsolete `Watermark`).

## Localization

`PCEdit.App.Core/Resources/Strings.resx` is the **source of truth** for keys, order, and canonical
**en-US** text. The 14 satellite `Strings.<culture>.resx` are **generated** — never hand-edit them.
`ILocalizer` (`Localization/Localizer.cs`) exposes `this[key]` + `Format(key, args)` and raises
`CultureChanged` on `SetCulture`. `Loc.Instance` is the DI singleton, also the source for the Avalonia
XAML markup extensions `{m:Loc}` / `{m:LocFormat}`. They bind `ILocalizer.Current` with a converter
(**not** the string indexer — Avalonia's reflection binding does not re-read an `[key]` path on an
`Item[]` notification), so text re-reads live on a language change. Status strings held in ViewModels
are stored as a *key* + args and re-formatted on `CultureChanged`.

To add or change a UI string:

1. Add/change the key in `Strings.resx`.
2. Add/change the same key in **every** `tools/i18n/<culture>.json` (14 files).
3. `python tools/i18n/gen_satellites.py` — regenerates the satellite `.resx`.
4. `python tools/i18n/gen_metainfo.py` — if the key feeds the store listing
   (`Shell_Title`, `About_Tagline`, `OpenFile_Intro`, `Disclaimer_Body`).
5. `dotnet test PCEdit.App.Core.Tests` — `LocalizationCatalogTests` enforce key parity, non-empty
   values, matching `{0}` placeholders, and that every `LocKeys` constant exists. CI also fails if
   the generated files are not committed in sync.

Keys referenced from C# are constants in `Localization/LocKeys.cs`. See
`PCEdit.App.Core/Resources/TRANSLATIONS.md` for the review-status table.

The disclaimer ("use at your own risk / back up your saves") has one source — `DISCLAIMER.md` at the
repo root — mirrored by `Disclaimer_Body` in the catalog and the AppStream `<description>`.

## Packaging

`deploy/` holds one build script per platform, all self-contained and all defaulting the version to
`Directory.Build.props` `<VersionPrefix>`:

- `build-appimage.sh` — Linux x86_64 AppImage via PupNet Deploy. `deploy/README.md` has the
  **glibc / oldest-practical-base** rule, the CJK-font (OS-fallback) note, and the WSL distro-matrix
  portability procedure. `deploy/*.metainfo.xml` is generated by `tools/i18n/gen_metainfo.py`.
- `build-windows.ps1` — `win-x64` `dotnet publish` zipped with `Compress-Archive`. Unsigned.
- `build-macos.sh <rid>` — assembles an unsigned `PCEdit.app` (generated `Info.plist`; `.icns` from
  `deploy/icon/` via `sips`+`iconutil` on macOS only) and zips it with `ditto`.
- `verify-app-local-icu.sh <dir>` — asserts a Linux build carries its own ICU.

**libicu is bundled on Linux, and only on Linux.** A self-contained publish does *not*
include it — .NET `dlopen()`s the system copy and FailFasts at startup on a distro that has
none (openSUSE Tumbleweed), which made the AppImage unlaunchable there. So
`PCEdit.Desktop.csproj` references `Microsoft.ICU.ICU4C.Runtime` and sets
`System.Globalization.AppLocalIcu` on `linux-*` RIDs only; **the package version
(`<AppLocalIcuVersion>`) and the switch value must stay identical** — the switch *is* the
`libicu*.so.<version>` filename suffix. Neither is visible to `ldd` (it is a `dlopen`), so
`verify-app-local-icu.sh` guards both from `build-appimage.sh` and from CI. Do not add the
package to the Windows or macOS publish: they use the OS ICU, and it has no `osx` RID.

Release automation and the versioning rules live in `RELEASING.md`.

## Git commit conventions
- **Never add `Co-Authored-By: Claude ...` (or any AI assistant) trailer to commit messages.**
