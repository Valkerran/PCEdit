# UI string catalog & translations

`Strings.resx` is the source of truth for keys, order, and the canonical **en-US** text.
The 14 satellite `Strings.<culture>.resx` files are **generated** — do not edit them by hand.

## Workflow

1. Add / change a key in `Strings.resx`.
2. Add / change the same key in **every** `tools/i18n/<culture>.json`.
3. Regenerate: `python tools/i18n/gen_satellites.py` (run from the repo root).
4. `dotnet test PCEdit.App.Core.Tests` — the `LocalizationCatalogTests` enforce:
   - every satellite has exactly the neutral key set, no empty values;
   - `{0}`, `{1}`, … placeholders match the neutral string;
   - every `LocKeys` constant exists in the neutral catalog.

Keys referenced from C# live in `Localization/LocKeys.cs`. XAML uses the key literally via each
head's `Translate` / `TranslateFormat` markup extension.

## Review status

Initial translations were produced by a machine-translation pass and need a native-speaker review
pass. Mark a locale reviewed once a fluent speaker has checked it in-app.

The UI-usability pass (Inventories search/filter, empty-state prompts, unsaved-close guard,
teleport "reset to current position", single-sentence Overview location/progress lines) added a
batch of keys — `Common_OpenSaveFile`, `Common_CloseWithoutSaving`, `Common_KeepEditing`,
`Quit_Discard*`, `Overview_Player{Location,Progress}`, `Inventories_{Search*,NoMatch,Filter*}`,
`SelectInv_Search`, `Teleport_{UseCurrentPosition,PositionReset}` — all still machine-translated.

The accessibility pass (screen-reader names for the nav list and previously-unlabelled controls)
added `Shell_NavA11y` — also machine-translated. The nav list's name is spoken on focus; the
destination page name is announced via the live region on every navigation.

The inventory/world-matching pass (filter inventories and teleport landmarks by planet) added
`Inventories_{FilterWorldA11y,WorldAll,WorldUnknown}` and
`Teleport_{LandmarkAllWorlds,AimedAtWorld,MultiplayerNote}` — all machine-translated. "World" is
the game's term for a planet. `Teleport_AimedAtWorld` is shown when picking a destination world
auto-fills X/Y/Z from that world's arrival point; `Teleport_MultiplayerNote` is the per-player /
host-only caption shown only on multiplayer saves. The same pass added
`About_SaveLocations{Heading,Note}` for the About page's save-folder card (the paths and platform
names themselves are literal, not catalog keys).

The logistics-editor pass (editing a container's demand / supply groups + priority on the
Inventories page) added the `Logistics_*` keys and `Inventories_{LogisticsSummary,EditLogistics,
EditLogisticsA11y}` — all machine-translated. `Logistics_Priority{Lowest…Highest}` are the game's
7 named priority levels; the romance-language forms agree with "priority" (feminine).
`Logistics_{SelectAll,Clear,Everything}` cover the demand/supply bulk controls (a list holding
every known group collapses to "Everything"). `Logistics_PriorityUnknown` ("Unknown ({0})") labels
a saved priority outside the game's -3..3 range — carried through an edit untouched.

The inventory-ids pass (issue #3: show and search inventory / item ids) added
`Inventories_ItemId`, `Inventories_ShowingSome` (the "Showing {0} of {1} items" caption on a card
a search has narrowed to some of its contents) and reworded `Inventories_{Search,SearchA11y,MoveA11y}` and
`SelectInv_Search` — all machine-translated. `Inventories_ItemId` is the item's number label, so
it follows each locale's existing number sign from `Inv_Fallback` (`#`, `nº`, `n.º`, `Nr.`, `nr`,
`n.`, `№`) rather than a literal `#`; `Inventories_MoveA11y` gained a `{1}` (the item id) so a
screen reader can tell two identical items apart. The inventory-id caption on the Inventories
cards and the Move dialog's destinations reuses `Inv_Fallback` itself.

The over-full warning pass (issue #64, phase 1: flag inventories holding more items than slots,
as an inventory-stacking mod leaves them) added `Inventories_FilterAttention` (the "Needs
attention ({0})" filter chip, `{0}` = flagged inventory count), `Inventories_Fill{OverFull,
NearLoadLimit}` (the word shown in a card's capacity badge) and `Inventories_Fill{OverFull,
NearLoadLimit}Help` (the badge tooltip, also its screen-reader help text) — all
machine-translated. The 8,000 in `Inventories_FillNearLoadLimitHelp` is the game's per-inventory
load cap, written in each locale's own digit grouping.

The stack-rows pass (issue #64, phase 2: one row per stack of identical items on an Inventories
card) added `Inventories_StackCount` ("× {0}", the count beside a stacked row) and
`Inventories_MoveStackA11y` (the Move button's screen-reader name on a stack, `{0}` = item name,
`{1}` = stack size) — machine-translated. A one-item row keeps `Inventories_ItemId` /
`Inventories_MoveA11y`.

The Overview attention banner (issue #64: a summary on the landing page when a save holds
over-full containers) added `Overview_Attention{Heading,OverCapacity,NearLoadLimit,Show,ShowA11y}`
— machine-translated. The two count lines put the number after a colon ("…: {0}.") so no locale
needs plural forms.

The partial-stack move (issue #64, phase 3: choose how many of a stack the Move dialog moves)
added `Inv_DestNoRoom` ("…: {0} free, {1} to move."), `SelectInv_{Quantity,QuantityA11y,All,AllA11y}`
(`{0}` = the stack size) and `SelectInv_MovedMany` ("Items moved to {1}: {0}.") — all
machine-translated. Full and over-full destinations are not offered at all; a disabled one's
screen-reader name ("…, room for {N}, …") is still built in English in
`InventoryOptionView.AccessibleLabel` (see *Not yet localized*).

The repair (issue #64, phase 6: trim over-full containers into a new copy of the save) added
`Overview_AttentionRepair{,A11y}` and `Repair_*` — all machine-translated. `Repair_Preview` and
`Repair_Done` put counts after a colon, like the banner, so no locale needs plural forms;
`Repair_TypeCount` is the "{0}: {1}" line per item type (French and the CJK locales use their own
colon). `Repair_KeepHint` names the 7,200-item ceiling in each locale's own digit grouping.

The identical-items setting (issue #64, phase 2b: one row per item or per stack on the Inventories
page) added `Inventories_Stacking{Label,A11y,Auto,AutoHint,Always,Never}` and
`Inventories_GroupedForSize` — machine-translated. "Grouped" / "One by one" are the chip labels
for always grouping and never grouping; `Inventories_StackingAutoHint` is the Automatic chip's
tooltip and screen-reader help text.

The move-first repair (issue #64, phase 6b: move chosen item types into free storage before the
repair removes anything) added `Repair_{MoveFirstHeading,MoveFirstHint,FreeSlots,PreviewMoved,
TypeChoiceA11y,MoveUp,MoveDown,MoveUpA11y,MoveDownA11y,DoneMoved}` and dropped `Repair_MostRemoved`
(the checklist replaced that list) — machine-translated. "Storage Crates and Lockers" in
`Repair_MoveFirstHint` means the game's general-purpose storage; use the game's own names where
the locale has them.

The origin markers (issue #64: tag each Inventories card with who placed its container) added
`Inventories_Origin{Built,Map,Wreck}` (the tag), `Inventories_Origin{Built,Map,Wreck}Help` (its
tooltip and screen-reader help text) and `Inventories_FilterBuilt` (the filter chip) —
machine-translated. "Map" means placed in the game's map by the developers, not a planet: the
page's world filter already uses "world" for planets, so keep the two words distinct.

| Culture | Language | Reviewed |
|---|---|---|
| en-US | English (United States) | n/a (source) |
| en-GB | English (United Kingdom) | n/a (copy of en-US; adjust only where usage differs) |
| fr | French | ☐ |
| de | German | ☐ |
| es-ES | Spanish (Spain) | ☐ |
| zh-Hans | Chinese (Simplified) | ☐ |
| ru | Russian | ☐ |
| pl | Polish | ☐ |
| pt-PT | Portuguese (Portugal) | ☐ |
| ko | Korean | ☐ |
| ja | Japanese | ☐ |
| pt-BR | Portuguese (Brazil) | ☐ |
| it | Italian | ☐ |
| zh-Hant | Chinese (Traditional) | ☐ |
| tr | Turkish | ☐ |

## Not yet localized

- **Item catalog display names** (`Data/ItemCatalog.json`) — ~1000 in-game item names stay English
  for now. Localizing them is a separate data effort (per-locale column in
  `tools/item-catalog/gen_catalog.py`).
- A few accessibility labels on data-template items where the visible text is already localized.
