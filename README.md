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

If your distro has no FUSE or libicu, either install fuse and libicu for your distro or run `./PCEdit-*-x86_64.AppImage --appimage-extract-and-run`.
For CJK menus, install your distro's Noto CJK font package.

**Windows:** Just grab the appropriate zip file, extract and run PCEdit.exe

**MacOs:** This is untested at the moment as I do not have access to a Mac.

The UI ships in 15 languages (English (US/UK), French, German, Spanish, Simplified &
Traditional Chinese, Russian, Polish, Portuguese (PT/BR), Korean, Japanese, Italian,
Turkish); it follows the OS language on first run and can be switched in-app.

**Note** - all non-english languages are AI translated. If anyone spots any translation issues, raise an issue request.

## Changelog

Full history and downloads are on the
[Releases page](https://github.com/Valkerran/PCEdit/releases).

### v1.1.0

- **Inventories — filter by world.** On a save that spans more than one planet, the Inventories
  page gains a **World** filter. Inventories that can't be matched to a planet (drones, vehicles,
  rockets, unplaced buffers) group under *Unknown world*.
- **Teleport — worlds.** Each landmark shows the planet it sits on and the list filters to the
  chosen destination world. Picking a destination world different from where the player stands
  fills X / Y / Z from that world's arrival point, and *Use current position* now restores the
  player's world as well as their coordinates.
- **Teleport — multiplayer note** about non-host players (see [Using the editor](#teleport)).
- **About page** now lists the game's default save-file locations per platform.
- Fixes: *Use current position* no longer leaves a stale world selected.

## Using the editor

### Worlds (planets)

Saves from the interplanetary update span more than one planet (Prime, Aqualis, Selenea, …).
Where PCEdit can work out which world something belongs to, it shows it:

- **Inventories** — on a multi-world save a **World** filter appears; it narrows the list to one
  planet. Player and equipment inventories are placed by their owner's current planet; containers
  by the placed object they belong to. Inventories with no owner (drones, vehicles, rockets,
  unplaced buffers) can't be matched and fall under **Unknown world** — they're still shown under
  *All worlds*.
- **Teleport** — each landmark shows the planet it sits on, and the landmark list is trimmed to
  your chosen destination world (tick *Show landmarks from all worlds* to see every one).

### Teleport

Teleport edits **only the player selected in the dropdown**. It writes that player's position and
their current planet — no other player is touched. Switching the selected player reloads the whole
form (world + X/Y/Z) from that player.

- Picking a destination world **different** from where the player currently stands fills X/Y/Z from
  that world's arrival point (its interplanetary escape pod, or a teleporter) so you land somewhere
  valid. Pick a landmark or type coordinates for a precise spot. Picking the player's own world
  again restores their real position.
- Coordinates from one planet are meaningless on another — if you change worlds, use the
  auto-filled arrival point or a landmark rather than leaving stale coordinates, or the game may
  drop the player out of bounds and relocate them.

> [!NOTE]
> **Multiplayer:** PCEdit writes the save correctly for any player, but only the **host** player's
> position and planet are reliably applied when the save loads. A non-host player's position is
> managed by their own game client and synced when they connect — edit a non-host player while
> they are offline, and confirm the result in-game.

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

## Game Save Locations

Below are the expected default paths for save files

- **Windows – Steam** – `%UserProfile%\AppData\LocalLow\MijuGames\Planet Crafter`

- **Windows – Xbox** – `%LocalAppData%\Packages\MijuGames.ThePlanetCrafter_ta6nvwnbx9v7t\SystemAppData\wgs`

- **Linux – Steam (Proton)** – `~/.steam/steam/steamapps/compatdata/1284190/pfx/drive_c/users/steamuser/AppData/LocalLow/MijuGames/Planet Crafter/`

- **macOS** – `~/Library/Application Support/MijuGames/Planet Crafter`

These are also listed on the **About** page in the app.

### Notes

The xbox save files do not use world names for the file name. Instead they use a random alpha numeric name that is regenerated with a new name each time it is saved.

Linux Steam locations may depend on the drive you have installed the game to. Replace "~/.steam/steam" with the folder you installed your steam library to.

