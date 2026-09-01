#!/usr/bin/env bash
# Assert that a Linux build of PCEdit carries its own ICU (GitHub issue #4).
#
# Usage:
#   deploy/verify-app-local-icu.sh <directory>
#
# <directory> is either a `dotnet publish -r linux-x64` output folder or an extracted
# AppImage (squashfs-root); the files are located by search, so either shape works.
#
# A self-contained publish does not bundle libicu - the runtime dlopen()s the system copy
# and FailFasts at startup on a distro that ships without one (openSUSE Tumbleweed). The
# Linux publish therefore ships libicu{uc,i18n,data}.so.<version> next to the app and sets
# the System.Globalization.AppLocalIcu switch to that same version (see PCEdit.Desktop.csproj).
# Neither half is visible to `ldd` - the load is a dlopen - so it gets checked here instead,
# from both deploy/build-appimage.sh and the CI workflow.
set -euo pipefail

DIR="${1:-}"
if [ -z "$DIR" ] || [ ! -d "$DIR" ]; then
    echo "usage: $0 <publish-or-extracted-appimage-directory>" >&2
    exit 2
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$SCRIPT_DIR/../PCEdit.Desktop/PCEdit.Desktop.csproj"

ICU="$(sed -n 's|.*<AppLocalIcuVersion>\([^<]*\)</AppLocalIcuVersion>.*|\1|p' "$CSPROJ")"
if [ -z "$ICU" ]; then
    echo "!! <AppLocalIcuVersion> not found in $CSPROJ" >&2
    exit 1
fi

fail=0

for lib in libicuuc libicui18n libicudata; do
    if [ -z "$(find "$DIR" -name "$lib.so.$ICU" -print -quit)" ]; then
        echo "!! $lib.so.$ICU is missing from $DIR" >&2
        fail=1
    fi
done

RUNTIMECONFIG="$(find "$DIR" -name PCEdit.runtimeconfig.json -print -quit)"
if [ -z "$RUNTIMECONFIG" ]; then
    echo "!! PCEdit.runtimeconfig.json is missing from $DIR" >&2
    fail=1
elif ! grep -q "\"System.Globalization.AppLocalIcu\"[[:space:]]*:[[:space:]]*\"$ICU\"" "$RUNTIMECONFIG"; then
    echo "!! System.Globalization.AppLocalIcu is not set to $ICU in $RUNTIMECONFIG" >&2
    fail=1
fi

if [ "$fail" -ne 0 ]; then
    echo "!! App-local ICU is not wired up - this build would not start on a distro without libicu." >&2
    exit 1
fi

echo ">> App-local ICU $ICU verified in $DIR"
