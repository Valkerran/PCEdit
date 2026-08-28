# PCEdit

A save-file editor for the game *[The Planet Crafter](https://www.plaentcrafter.com/)*.
View and edit players, world terraforming, inventories, terra tokens, and teleport a
player between locations.

Unofficial fan tool — not affiliated with or endorsed by the developers of The Planet Crafter.

> [!WARNING]
> Editing a save can corrupt it. **Always back up your save file before editing.** See
> [DISCLAIMER.md](DISCLAIMER.md). Provided as is, without warranty of any kind.

## Download

**Linux:** grab `PCEdit-*-x86_64.AppImage` from the
[latest release](https://github.com/waldog78/PCEdit/releases/latest). It bundles the .NET
runtime — no install needed.

```bash
chmod +x PCEdit-*-x86_64.AppImage
./PCEdit-*-x86_64.AppImage
```

If your distro has no FUSE, run `./PCEdit-*-x86_64.AppImage --appimage-extract-and-run`.
For CJK menus, install your distro's Noto CJK font package.

**Windows / macOS:** no packaged build yet — run from source (below).

The UI ships in 15 languages (English (US/UK), French, German, Spanish, Simplified &
Traditional Chinese, Russian, Polish, Portuguese (PT/BR), Korean, Japanese, Italian,
Turkish); it follows the OS language on first run and can be switched in-app.

## Run from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project PCEdit.Desktop/PCEdit.Desktop.csproj
```

## Project layout

| | |
|---|---|
| `PCEdit.SaveFileHandler` | Save-file parse / edit / re-serialize (no dependencies) |
| `PCEdit.App.Core` | Portable app layer (ViewModels, services, localization) behind the UI head |
| `PCEdit.Desktop` | Avalonia UI head — Linux, Windows, macOS |
| `deploy/` | AppImage packaging ([details](deploy/README.md)) |

Development notes, architecture, and the localization workflow are in
[CLAUDE.md](CLAUDE.md).

## Building the AppImage

```bash
deploy/build-appimage.sh
```

Build on the oldest practical glibc base (Ubuntu 22.04) — see [deploy/README.md](deploy/README.md).

## License

PCEdit is free software: you can redistribute it and/or modify it under the terms of the
**GNU General Public License** as published by the Free Software Foundation, either version 3
of the License, or (at your option) any later version. See [LICENSE](LICENSE).

It is distributed in the hope that it will be useful, but **without any warranty** — see also
[DISCLAIMER.md](DISCLAIMER.md).

*The Planet Crafter* is a trademark of its respective owners; PCEdit is an unofficial fan tool
and is not affiliated with or endorsed by them.
