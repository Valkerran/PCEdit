#!/usr/bin/env bash
# Build a self-contained macOS .app bundle for the PCEdit desktop head and zip it.
#
# Usage:
#   deploy/build-macos.sh <rid> [version]
#     rid      osx-x64 | osx-arm64
#     version  dotted X.Y.Z (default: <VersionPrefix> from Directory.Build.props)
#
# Output: artifacts/PCEdit-<version>-macos-<arch>.zip   (contains PCEdit.app)
#
# Requirements: .NET 10 SDK. On macOS, `sips` + `iconutil` (always present) turn the
# PNG icon set into PCEdit.app/Contents/Resources/PCEdit.icns. On Linux the bundle is
# still produced, just without the .icns.
#
# The bundle is NOT code-signed or notarised - first launch needs a right-click ->
# Open, or `xattr -dr com.apple.quarantine PCEdit.app`. This is noted in the release.
set -euo pipefail

RID="${1:?usage: build-macos.sh <osx-x64|osx-arm64> [version]}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS="$REPO_ROOT/artifacts"

VERSION="${2:-$(sed -n 's|.*<VersionPrefix>\([^<]*\)</VersionPrefix>.*|\1|p' "$REPO_ROOT/Directory.Build.props")}"
[ -n "$VERSION" ] || { echo "!! Could not determine version" >&2; exit 1; }
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "!! Version '$VERSION' is not X.Y.Z" >&2; exit 1; }

case "$RID" in
    osx-x64)   ARCH=x64 ;;
    osx-arm64) ARCH=arm64 ;;
    *) echo "!! Unknown RID: $RID (expected osx-x64 or osx-arm64)" >&2; exit 1 ;;
esac

APP="$SCRIPT_DIR/OUT/$RID/PCEdit.app"
PUBLISH="$SCRIPT_DIR/OUT/$RID/publish"
rm -rf "$SCRIPT_DIR/OUT/$RID"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

echo ">> dotnet publish $RID (self-contained) $VERSION"
dotnet publish "$REPO_ROOT/PCEdit.Desktop/PCEdit.Desktop.csproj" \
    -c Release -r "$RID" --self-contained true \
    -p:Version="$VERSION" -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false \
    -o "$PUBLISH"

cp -R "$PUBLISH"/. "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/PCEdit"

ICON_PLIST=""
if command -v iconutil >/dev/null 2>&1 && command -v sips >/dev/null 2>&1; then
    echo ">> Building PCEdit.icns from deploy/icon/pcedit.512x512.png"
    ICONSET="$SCRIPT_DIR/OUT/$RID/PCEdit.iconset"
    rm -rf "$ICONSET"; mkdir -p "$ICONSET"
    src="$SCRIPT_DIR/icon/pcedit.512x512.png"
    for s in 16 32 128 256 512; do
        sips -z "$s" "$s"       "$src" --out "$ICONSET/icon_${s}x${s}.png"    >/dev/null
        sips -z "$((s*2))" "$((s*2))" "$src" --out "$ICONSET/icon_${s}x${s}@2x.png" >/dev/null
    done
    iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/PCEdit.icns"
    rm -rf "$ICONSET"
    ICON_PLIST=$'\n    <key>CFBundleIconFile</key><string>PCEdit</string>'
else
    echo "!! sips/iconutil not found - bundling without an .icns"
fi

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>PCEdit</string>
    <key>CFBundleDisplayName</key><string>PCEdit</string>
    <key>CFBundleIdentifier</key><string>com.valkerran.pcedit</string>
    <key>CFBundleExecutable</key><string>PCEdit</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>${VERSION}</string>
    <key>CFBundleVersion</key><string>${VERSION}.${GITHUB_RUN_NUMBER:-0}</string>${ICON_PLIST}
    <key>LSMinimumSystemVersion</key><string>11.0</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>LSApplicationCategoryType</key><string>public.app-category.utilities</string>
</dict>
</plist>
PLIST

mkdir -p "$ARTIFACTS"
ZIP="$ARTIFACTS/PCEdit-${VERSION}-macos-${ARCH}.zip"
rm -f "$ZIP"
if command -v ditto >/dev/null 2>&1; then
    ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
else
    ( cd "$SCRIPT_DIR/OUT/$RID" && zip -qy -r "$ZIP" PCEdit.app )
fi
echo ">> $ZIP"
