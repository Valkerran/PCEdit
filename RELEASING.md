# Releasing PCEdit

Each release publishes four self-contained builds of the desktop head (`PCEdit.Desktop`)
to a GitHub Release:

| Platform | Artifact | Built on |
|---|---|---|
| Linux | `PCEdit-<version>-<build>.x86_64.AppImage` | `ubuntu-22.04` (oldest practical glibc) |
| Windows | `PCEdit-<version>-win-x64.zip` | `windows-latest` |
| macOS (Intel) | `PCEdit-<version>-macos-x64.zip` | `macos-latest` |
| macOS (Apple Silicon) | `PCEdit-<version>-macos-arm64.zip` | `macos-latest` |

`<build>` is the Release workflow's run number (PupNet's release suffix), e.g.
`PCEdit-1.5.0-16.x86_64.AppImage`.

Plus `SHA256SUMS.txt`. The macOS `.zip` contains an **unsigned** `PCEdit.app` — first launch
needs a right-click → **Open** (or `xattr -dr com.apple.quarantine PCEdit.app`).

**What the checksums do and do not prove.** `SHA256SUMS.txt` is published in the same GitHub
Release as the files it describes, so it proves a download arrived intact — not who made it.
Anyone able to replace an artifact there could replace the checksum file too. None of the
artifacts is code-signed: the Windows zip is unsigned and the macOS app is unsigned and
un-notarised. Signing certificates cost real money and are a project decision rather than an
oversight; until then the trust anchor is this repository and its release workflow.

## Versioning — single source of truth

The version lives in **one place**: `<VersionPrefix>` in the repo-root
[`Directory.Build.props`](Directory.Build.props). Every project inherits it, every packaging
script reads it, and the Release workflow stamps it into the assemblies with `-p:Version=`.

Rules the pipelines enforce:

- **CI** (`version-guard` job) fails a PR if `<VersionPrefix>` is not `X.Y.Z`, and fails if it
  is *behind* the newest `vX.Y.Z` tag. It warns (does not fail) once `<VersionPrefix>` has
  caught up to the latest release tag — that warning is the reminder to bump before the next
  release.
- **Release** refuses to build if a pushed tag `vX.Y.Z` disagrees with `<VersionPrefix>`, or if
  a manual run targets a version whose tag already exists, or if a manual run is not on `main`.

## Cutting a release

1. **Bump the version.** Edit `<VersionPrefix>` in `Directory.Build.props` (semver: `MAJOR.MINOR.PATCH`).
   Open a PR, get CI green, merge to `main`.

2. **Trigger the Release workflow** — either:

   - **From GitHub (recommended):** Actions → **Release** → **Run workflow** → branch `main`.
     It reads `<VersionPrefix>`, creates the `vX.Y.Z` tag on the current `main`, builds all
     four artifacts, and publishes the Release with auto-generated notes.

   - **By tag:** create and push the tag yourself. It must match `<VersionPrefix>` exactly.

     ```bash
     git checkout main && git pull
     git tag "v$(sed -n 's|.*<VersionPrefix>\([^<]*\)</VersionPrefix>.*|\1|p' Directory.Build.props)"
     git push origin --tags
     ```

3. **Wait for the four build jobs**, then check the drafted-then-published Release. Edit the
   notes if needed.

4. **Add the macOS note to the Release body — manually.** The workflow's auto-generated notes
   only list the merged PRs; they do **not** mention that the macOS build is unsigned. Append
   this to every release body:

   ```markdown
   ---

   ### macOS

   The macOS `.zip` contains an **unsigned** `PCEdit.app`. On first launch, right-click the
   app and choose **Open** (or run `xattr -dr com.apple.quarantine PCEdit.app`).
   ```

   **And call out any user-visible packaging change in the same section.** The
   auto-generated notes list PR titles, which almost never convey that the download behaves
   differently — a notable size change, a new or dropped runtime dependency, a renamed or
   added artifact, a raised minimum OS. Say what changed, by how much, and why, and say who
   is *not* affected: a reader who sees "+13 MB" on Linux will wonder about their own
   platform. Also add the same entry to [`CHANGELOG.md`](CHANGELOG.md), which is what people
   read before downloading.

   v1.2.1 is the worked example — bundling ICU took the AppImage from 43.0 MB to 56.5 MB,
   so its notes lead with those numbers, give the reason (it would not start at all on a
   distro with no system `libicu`), and state that Windows and macOS are unchanged.

5. **Audit the documentation against the release.** Check the
   [wiki](https://github.com/Valkerran/PCEdit/wiki) and the repository docs — `README.md`,
   this file, `deploy/README.md`, the `tools/*/README.md` files, `CLAUDE.md` and
   `PCEdit.App.Core/Resources/TRANSLATIONS.md` — for anything the release made missing or
   wrong: new features, renamed buttons, changed counts, removed files, artifact names. Verify
   against the code and the *actual* release assets, not memory. Skipping this is how the wiki
   sat at v1.3.0 through five releases and how the AppImage file name was documented wrongly
   in every release until v1.5.0.

6. **Bump again for development** (optional but tidy): raise `<VersionPrefix>` to the next
   planned version on `main` so pre-release builds are not stamped with the shipped version.

## Local packaging (for testing — do not ship these)

```bash
deploy/build-appimage.sh                 # Linux AppImage  -> artifacts/
deploy/build-windows.ps1                 # Windows zip     -> artifacts/
deploy/build-macos.sh osx-arm64          # macOS .app zip  -> artifacts/
```

Each restores once and then publishes with `--no-restore`, because the committed
`packages.lock.json` files describe a restore with no RuntimeIdentifier (see
[`deploy/README.md`](deploy/README.md#nuget-lock-files)). All three default the version to
`<VersionPrefix>`; pass an explicit version as the last
argument (`-Version` for the PowerShell script) to override. Linux **release** artifacts must
come from the CI `ubuntu-22.04` job — see [`deploy/README.md`](deploy/README.md) for the
glibc rule and the WSL distro-matrix portability procedure.
