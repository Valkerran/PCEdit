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

A Planet Crafter save file is a single UTF-8-with-BOM text file, not JSON overall. The framing (see
`PlanetCrafterSaveFileSerializer`): a leading `\r`, then the 10 sections (index 0–9) joined by
`\r@\r`, then a trailing `\r@`. A section holds either one JSON object or a list of JSON objects
joined by `|\n`. The store writes the BOM (`PlanetCrafterSaveFileStore` uses a BOM-emitting UTF-8
encoding); `Serialize` returns everything after it. Reproduce this exactly — an unedited
load→save is asserted byte-identical, so a diff after an edit shows only the edit.
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
`linkedWo`, a labelled container `text`, a logistics container, a deliberately-unknown key); both
test projects link it into `TestData/`. Regenerate it with a byte-writing script, not an editor —
it has no trailing newline and must keep its exact bytes (`.gitattributes` marks it `-text`).

Layering, each with a small interface for testability/DI:

- `IJsonRecordSerializer` / `JsonRecordSerializer` — wraps `System.Text.Json` with camelCase naming and `WhenWritingNull` ignore semantics; deserializes/serializes a single record (section or list item) and wraps `JsonException` in `InvalidDataException` with the offending section index. Also holds `GameDecimalConverter` (the game writes every decimal with a fractional part — force `N.0`, never `N`). Every leaf model has a `[JsonExtensionData] ExtensionData` dictionary, so a JSON key no model names is **preserved** across a round-trip, not silently dropped. `WorldObject` goes further: the game's world-object key order is not stable (proven — the same pair of keys appears in both orders across records), so `WorldObjectConverter` records each record's key order on read and replays it on write, keeping unknown keys in place too. Tests: `RoundTrip_RealSampleSaveFile_ReserializesCharacterForCharacter` (whole-file, string-exact) and `PlanetCrafterSaveFileStoreTests.SaveThenLoad_...IsByteIdenticalOnDisk` (with BOM); `WorldObjectConverterTests` / `GameDecimalFormatTests` for the pieces; `GameFormatKeyMappingTests` pins the abbreviated-key spellings a symmetric model round-trip can't catch.
- `IPlanetCrafterSaveFileSerializer` / `PlanetCrafterSaveFileSerializer` — splits/joins the 10 sections (framing above), delegating record-level (de)serialization to `IJsonRecordSerializer`. `Deserialize` is lenient about whitespace/line-endings; `Serialize` reproduces the game framing exactly.
- `IPlanetCrafterSaveFileStore` / `PlanetCrafterSaveFileStore` — file I/O (`Load`/`Save` by path). `Load` uses `File.ReadAllText` (strips the BOM); `Save` writes UTF-8 **with** a BOM to match the game.

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
filter. `Services/PositionCodec` handles the `"x,y,z"` string shared by
`PlayerData.PlayerPosition` / `WorldObject.Position`.

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

Release automation and the versioning rules live in `RELEASING.md`.

## Git commit conventions
- **Never add `Co-Authored-By: Claude ...` (or any AI assistant) trailer to commit messages.**
