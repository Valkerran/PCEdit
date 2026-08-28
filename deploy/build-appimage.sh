#!/usr/bin/env bash
# Build the self-contained x86_64 AppImage for PCEdit (Avalonia desktop head).
#
# Usage:
#   deploy/build-appimage.sh [VERSION]
#
# VERSION is an optional "version[release]" string (e.g. 1.2.0[1]) passed to pupnet;
# if omitted, <VersionPrefix> from the repo-root Directory.Build.props is used (and
# failing that, AppVersionRelease from pcedit.pupnet.conf).
#
# Requirements: .NET 10 SDK, python3. The KuiperZone.PupNet global tool and appimagetool
# are downloaded by this script if missing. FUSE is not required at build time;
# APPIMAGE_EXTRACT_AND_RUN is exported for FUSE-less environments (WSL, CI).
#
# Build on the OLDEST practical glibc base (Ubuntu 22.04 / 20.04, or the CI container) so
# the bundled .NET runtime links against an old glibc and the AppImage runs on newer
# distros. Do NOT publish an artifact built on a bleeding-edge distro.
set -euo pipefail

PUPNET_VERSION="1.9.0"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CONF="$SCRIPT_DIR/pcedit.pupnet.conf"
ARTIFACTS="$REPO_ROOT/artifacts"

export APPIMAGE_EXTRACT_AND_RUN=1
export PATH="$PATH:$HOME/.dotnet/tools:$HOME/.local/bin"

# PupNet shells out to `appimagetool-x86_64.AppImage`; fetch it if it is not on PATH.
if ! command -v appimagetool-x86_64.AppImage >/dev/null 2>&1; then
    echo ">> Downloading appimagetool"
    mkdir -p "$HOME/.local/bin"
    _tool="$HOME/.local/bin/appimagetool-x86_64.AppImage"
    for _u in \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage" \
        "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"; do
        curl -fSL -o "$_tool" "$_u" && break || true
    done
    chmod +x "$_tool"
fi

# PupNet targets net8.0; let it run on a machine that only ships the .NET 10 runtime.
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"

# PupNet is a .NET tool; if the SDK was installed to a non-standard prefix (e.g. via
# dotnet-install.sh) its apphost needs DOTNET_ROOT to find the shared runtime.
if [ -z "${DOTNET_ROOT:-}" ] && command -v dotnet >/dev/null 2>&1; then
    _dotnet_dir="$(dirname "$(readlink -f "$(command -v dotnet)")")"
    [ -d "$_dotnet_dir/shared/Microsoft.NETCore.App" ] && export DOTNET_ROOT="$_dotnet_dir"
fi

if ! command -v pupnet >/dev/null 2>&1; then
    echo ">> Installing KuiperZone.PupNet $PUPNET_VERSION"
    dotnet tool install -g KuiperZone.PupNet --version "$PUPNET_VERSION"
fi

echo ">> pupnet $(pupnet --version | head -n1)"
echo ">> Regenerating AppStream metadata from the string catalog"
python3 "$REPO_ROOT/tools/i18n/gen_metainfo.py"

VERSION_ARG=()
if [ "${1:-}" != "" ]; then
    VERSION_ARG=(--app-version "$1")
else
    _v="$(sed -n 's|.*<VersionPrefix>\([^<]*\)</VersionPrefix>.*|\1|p' "$REPO_ROOT/Directory.Build.props")"
    [ -n "$_v" ] && VERSION_ARG=(--app-version "$_v")
fi

echo ">> Building AppImage (linux-x64, self-contained)"
pupnet "$CONF" \
    --kind appimage \
    --runtime linux-x64 \
    --build Release \
    --skip-yes \
    --verbose \
    "${VERSION_ARG[@]}"

mkdir -p "$ARTIFACTS"
found=0
for f in "$SCRIPT_DIR"/OUT/*.AppImage; do
    [ -e "$f" ] || continue
    cp -v "$f" "$ARTIFACTS/"
    found=1
done

if [ "$found" -eq 0 ]; then
    echo "!! No .AppImage produced in $SCRIPT_DIR/OUT" >&2
    exit 1
fi

echo ">> Done. Artifacts in $ARTIFACTS:"
ls -la "$ARTIFACTS"/*.AppImage
