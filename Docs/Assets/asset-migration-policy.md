# Asset Migration Policy

`Tools/AssetMigration/asset-migration-manifest.json` is the runnable allowlist
for Flutter-to-Unity asset migration. Run `Sync-FlutterAssets.ps1` with an
explicit source root, Unity root, and manifest; it hashes every source, copies
only changed approved files, and never deletes Unity files.

Only assets with `licenseStatus: approved` may enter the runnable manifest.
The first slice imports the approved player, lobby character, boss, Hwando VFX,
and the two source rights ledgers. The ledgers are copied as raw audit records.

The following requested mappings are blocked because the source ledger does not
mark them approved:

- `assets/images/monsters/plague_rat_swarm_128.png`
- `assets/images/monsters/bandit_128.png`
- `assets/images/monsters/vengeful_spirit_128.png`
- `assets/images/monsters/dokkaebi_128.png`
- `assets/images/tiles/moonlit_office_tiles_128.png`
- `assets/images/props/moonlit_office_props_128.png`
- all listed music, SFX, and UI audio (`audio-rights-ledger.csv` marks them `temporary`)
- `SongMyung-Regular.ttf`, `GowunBatang-Regular.ttf`, `GowunBatang-Bold.ttf`, and their OFL text files, pending a source-ledger approval record

When a source-ledger status changes to `approved`, add the exact mapping with
the prescribed profile (`pixel`, `ui`, `music`, `sfx`, or `raw`) and rerun the
test plus a dry run before copying. Unity import settings and generated `.meta`
files remain Unity-owned and are reviewed after each actual sync.
