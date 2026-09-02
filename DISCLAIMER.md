# Disclaimer — use at your own risk

PCEdit is a save-file editor for *The Planet Crafter*. It edits save files
directly.

**A bad edit can corrupt a save and lose progress.** PCEdit keeps a copy of your
save from before it first writes to it — see below — but treat that as a safety
net, not a substitute. **Keep your own backup as well.**

## The copy PCEdit keeps

The first time PCEdit saves a file after you open it, it copies the original
into its own folder first:

| Platform | Folder |
|---|---|
| Windows | `%LocalAppData%\PCEdit\backups` |
| Linux | `~/.local/share/PCEdit/backups` |
| macOS | `~/Library/Application Support/PCEdit/backups` |

The five most recent copies of each save file are kept, and the path is shown on
PCEdit's About page.

The copy is taken once per file you open rather than once per save, so it is the
file as it was before PCEdit touched it. It is made on a best-effort basis: if it
cannot be written, saving still goes ahead, so it is not a guarantee. PCEdit also
writes every save through a temporary file and swaps it in, so an interrupted
save cannot leave a half-written file behind — but neither measure protects you
from an edit you meant to make and later regret.

## Warranty

This software is provided "as is", without warranty of any kind, express or
implied, including but not limited to the warranties of merchantability, fitness
for a particular purpose, and noninfringement. In no event shall the authors be
liable for any claim, damages, or other liability arising from, out of, or in
connection with the software or its use.

PCEdit is an unofficial, fan-made tool. It is not affiliated with, endorsed by,
or supported by the developers or publishers of *The Planet Crafter*.

---

This text is the single source mirrored by the in-app disclaimer
(`Disclaimer_Body` in the string catalog), the AppStream `<description>` in
`deploy/com.valkerran.pcedit.metainfo.xml`, and the release notes.
