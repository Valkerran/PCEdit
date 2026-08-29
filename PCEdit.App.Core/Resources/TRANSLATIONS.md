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

The logistics-editor pass (editing a container's demand / supply groups + priority on the
Inventories page) added the `Logistics_*` keys and `Inventories_{LogisticsSummary,EditLogistics,
EditLogisticsA11y}` — all machine-translated.

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
