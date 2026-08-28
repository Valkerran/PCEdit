# Packaging

The Avalonia desktop head (`PCEdit.Desktop`) ships **self-contained** on all three
platforms — the .NET runtime is bundled, so no .NET install is needed on the target.

| Platform | Output | Script |
|---|---|---|
| Linux | `artifacts/PCEdit-<version>-x86_64.AppImage` | `build-appimage.sh` |
| Windows | `artifacts/PCEdit-<version>-win-x64.zip` | `build-windows.ps1` |
| macOS | `artifacts/PCEdit-<version>-macos-{x64,arm64}.zip` (`PCEdit.app`) | `build-macos.sh <rid>` |

The version defaults to `<VersionPrefix>` in the repo-root `Directory.Build.props` for all
three; release automation and the versioning rules are in [`../RELEASING.md`](../RELEASING.md).
The GitHub **Release** workflow (`.github/workflows/release.yml`) runs all three on tag push
or manual dispatch and attaches the artifacts + `SHA256SUMS.txt` to a GitHub Release.

| File                                | Purpose |
|-------------------------------------|---|
| `pcedit.pupnet.conf`                | PupNet configuration (identity, publish args, package output) |
| `pcedit.desktop`                    | `.desktop` entry template (`Categories=Utility;Game;`) |
| `com.valkerran.pcedit.metainfo.xml` | AppStream metadata — **generated**, do not hand-edit |
| `icon/`                             | App icon: scalable `pcedit.svg` + rasterised PNGs (16–512 px) |
| `build-appimage.sh`                 | Linux AppImage build → `artifacts/` |
| `build-windows.ps1`                 | Windows `win-x64` zip → `artifacts/` |
| `build-macos.sh`                    | macOS `.app` bundle zip (per RID) → `artifacts/` |

## Linux AppImage

Shipped as a **self-contained x86_64 AppImage**, driven by
[PupNet Deploy](https://github.com/kuiperzone/PupNet-Deploy).

The AppStream file is regenerated from the string catalog by
`tools/i18n/gen_metainfo.py` (summary + description come from the same
`tools/i18n/*.json` translations that feed the satellite assemblies, so the store
listing stays in sync across all 15 locales). `build-appimage.sh` runs it automatically.

## Build

```bash
deploy/build-appimage.sh            # version from pcedit.pupnet.conf
deploy/build-appimage.sh 1.2.0[1]   # explicit version[release]
```

Requirements: .NET 10 SDK, Python 3. The `KuiperZone.PupNet` global tool and
`appimagetool` are downloaded by the script if missing. FUSE is **not** needed at build
time — the script exports `APPIMAGE_EXTRACT_AND_RUN=1`.

If the .NET SDK was installed via `dotnet-install.sh` to `~/.dotnet`, export
`DOTNET_ROOT=$HOME/.dotnet` and put `$HOME/.dotnet` on `PATH` first (the script also sets
`DOTNET_ROOT` itself when it can work it out). PupNet targets `net8.0`, so a .NET 8
runtime alongside the 10 SDK avoids a roll-forward:
`dotnet-install.sh --channel 8.0 --runtime dotnet --install-dir $HOME/.dotnet`.

### glibc / base distro — important

An AppImage does **not** bundle glibc. Build on the **oldest practical base** so the
bundled runtime links against an old glibc and runs on newer distros:

- **Release artifacts** come from the CI job / a container on **Ubuntu 22.04** (or 20.04).
- A build from a bleeding-edge distro (or WSL Ubuntu 24.04) silently raises the glibc
  floor and breaks older targets — use those only to *build for testing*, never to ship.

### Runtime libraries on the target

The runtime is bundled; only GUI/system libraries are expected, and they are present on
every desktop Linux (X11 or XWayland): `libX11`, `libICE`, `libSM`, `fontconfig`,
`libGL`. No bundling required.

### CJK fonts

The bundled UI fonts (Inter / Open Sans) have **no CJK glyphs**. For `zh-Hans` /
`zh-Hant` / `ja` / `ko`, PCEdit relies on the OS fallback font via fontconfig — install
the distro's Noto CJK package (`fonts-noto-cjk`, `google-noto-sans-cjk-fonts`,
`noto-sans-cjk-fonts`, …) if CJK text shows as tofu. Noto CJK is ~40 MB+ per weight and
is deliberately **not** bundled to keep the AppImage small.

## Run / verify (WSL dev check)

WSLg gives WSL2 a working Wayland + X11 display, so the AppImage opens a GUI there.

```bash
cd artifacts
chmod +x PCEdit-*-x86_64.AppImage
./PCEdit-*-x86_64.AppImage --appimage-extract-and-run            # or: sudo apt install libfuse2
WAYLAND_DISPLAY= ./PCEdit-*-x86_64.AppImage --appimage-extract-and-run   # force the X11 path
LANG=ja_JP.UTF-8 ./PCEdit-*-x86_64.AppImage --appimage-extract-and-run   # locale auto-select + CJK
```

Per run, confirm: the window renders; `PCEdit.SaveFileHandler/Standard-2.json` loads,
edits, saves and round-trips; the disclaimer shows once and the acknowledgement persists
(`~/.config/PCEdit/settings.json`); no missing-glyph boxes in a CJK locale.

## Pre-release portability testing — WSL distro matrix

A single distro only proves "runs on that distro". Portability is validated by installing
several WSL2 distros side by side on one Windows box. **WSLg is shared to every WSL2
distro automatically**, so each gets a working Wayland + X11 display with no setup — only
the client X/GL/font libraries are installed per distro. Copy in the **CI-built** AppImage
(the one from `ubuntu-22.04`); do **not** rebuild per distro.

### 0. One-time
```powershell
wsl --update
wsl --version            # confirm WSL2 + WSLg
wsl --list --online
```

### 1. Distros (and why each)
| Distro | Purpose | Install |
|---|---|---|
| Ubuntu 22.04 | matches the CI/build base — baseline | `wsl --install -d Ubuntu-22.04` |
| Ubuntu 24.04 | newer glibc / GTK stack | `wsl --install -d Ubuntu-24.04` |
| Debian | conservative upstream libs | `wsl --install -d Debian` |
| Fedora | RPM, recent glibc, SELinux | `wsl --install -d FedoraLinux-42` |
| openSUSE Tumbleweed | rolling, bleeding-edge libs | `wsl --install -d openSUSE-Tumbleweed` |
| Arch | rolling, minimal — `wsl --import` an `archlinux` container rootfs | (see PupNet-Deploy / Arch docs) |
| Ubuntu 20.04 *(optional)* | oldest realistic glibc floor | `wsl --install -d Ubuntu-20.04` |

### 2. Per-distro runtime dependencies
FUSE is optional if you always pass `--appimage-extract-and-run`.
```bash
# Debian / Ubuntu
sudo apt install -y libx11-6 libice6 libsm6 libfontconfig1 libgl1 libglu1-mesa libfuse2 \
    fonts-noto-cjk fonts-noto-color-emoji
# Fedora
sudo dnf install -y libX11 libICE libSM fontconfig mesa-libGL fuse-libs \
    google-noto-sans-cjk-fonts google-noto-color-emoji-fonts
# openSUSE Tumbleweed
sudo zypper install -y libX11-6 libICE6 libSM6 fontconfig Mesa-libGL1 fuse \
    noto-sans-cjk-fonts noto-coloremoji-fonts
# Arch
sudo pacman -S --noconfirm libx11 libice libsm fontconfig mesa fuse2 noto-fonts-cjk noto-fonts-emoji
```

### 3. Smoke test per distro
```bash
cd /mnt/c/Users/<you>/…/PCEdit/artifacts
chmod +x PCEdit-*-x86_64.AppImage
./PCEdit-*-x86_64.AppImage --appimage-extract-and-run                       # opens under WSLg
WAYLAND_DISPLAY= ./PCEdit-*-x86_64.AppImage --appimage-extract-and-run      # force X11
LANG=ja_JP.UTF-8 ./PCEdit-*-x86_64.AppImage --appimage-extract-and-run     # locale + CJK glyphs
./PCEdit-*-x86_64.AppImage --appimage-extract >/dev/null && \
  ldd squashfs-root/usr/bin/PCEdit | grep -i 'not found'                   # must print nothing
```
Per distro confirm: window renders; `Standard-2.json` loads/edits/saves/round-trips; the
disclaimer shows once and the ack persists (`~/.config/PCEdit/settings.json`); no
missing-glyph boxes in a CJK locale; no `not found` libs. Then `wsl --unregister <name>`.

Gate releases on the Ubuntu 22.04 / 24.04 / Fedora / Arch rows being green.

## Windows

```powershell
deploy/build-windows.ps1                 # version from Directory.Build.props
deploy/build-windows.ps1 -Version 1.2.0  # explicit
```

`dotnet publish -r win-x64 --self-contained`, then `Compress-Archive` the publish
folder to `artifacts/PCEdit-<version>-win-x64.zip`. The user unzips and runs
`PCEdit.exe` — no installer, no .NET prerequisite. DPI awareness comes from
`PCEdit.Desktop/app.manifest`. The build is **not** code-signed, so SmartScreen shows a
"Windows protected your PC" prompt on first run (**More info → Run anyway**).

## macOS

```bash
deploy/build-macos.sh osx-arm64          # Apple Silicon
deploy/build-macos.sh osx-x64 1.2.0      # Intel, explicit version
```

`dotnet publish -r <rid> --self-contained` into a hand-assembled `PCEdit.app`
(`Contents/MacOS/` = publish output, generated `Contents/Info.plist`,
`Contents/Resources/PCEdit.icns` built from `icon/pcedit.512x512.png` via `sips` +
`iconutil`). On a non-macOS host the `.icns` step is skipped. The bundle is zipped with
`ditto` (falls back to `zip`) to `artifacts/PCEdit-<version>-macos-<arch>.zip`.

`macos-latest` runners are Apple Silicon; the `osx-x64` build is a self-contained
cross-publish and runs on Intel Macs (and on Apple Silicon under Rosetta).

**Not signed or notarised.** Gatekeeper quarantines it — first launch needs a
right-click → **Open**, or:

```bash
xattr -dr com.apple.quarantine /Applications/PCEdit.app
```

This is called out in the Release body. `LSMinimumSystemVersion` is 11.0.

## Tested on

| Distro | glibc | Result | Date |
|---|---|---|---|
| Ubuntu 22.04 (WSL2, build host) | 2.35 | ✅ built (42.9 MB); `ldd` clean; GUI launches under WSLg | 2026-08-28 |
| Ubuntu 24.04 | | _pending_ | |
| Debian 12 | | _pending_ | |
| Fedora | | _pending_ | |
| Arch | | _pending_ | |
| openSUSE Tumbleweed | | _pending_ | |
