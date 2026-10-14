# Changelog

## 1.0.4
- Add optional `DebugLogging` diagnostics at info level so they appear in the normal BepInEx log.
- Log startup and update heartbeats, including server status, scan settings, and inventory-hook counts, even when cleanup is disabled or the server guard blocks it.
- Log per-inventory stack counts and raw cheated flags, plus sweep totals for players, containers, world drops, and stands. Report container ownership and skipped invalid network views.
- This release adds diagnostics; it does not change item detection or deletion rules.

## 1.0.3
- Resolve inventory change hooks at runtime instead of requiring the 1.0.14 `(bool, bool)` signature. Intended for Valheim 1.0.17 and later compatible updates; game testing is still required.
- Support building against either client or dedicated-server assemblies, with a clear error when references are missing.
- Add a GitHub Actions build and installable package using the current public dedicated server.

## 1.0.2
- Delete cheated creature loot on the dedicated server. Vanilla sets `m_cheated` after `ItemDrop.Awake`, so the old check ran too early and never saw the flag. Also reloads ground piles from the ZDO on the periodic sweep.
- Inventory `Changed` patch matches the 1.0.14 `(bool, bool)` signature.

## 1.0.1
- Plugin GUID is now `ageous27.nocheatitems` (config file: `BepInEx/config/ageous27.nocheatitems.cfg`).

## 1.0.0
- Initial release for Valheim 1.0.
- Deletes items marked with the official cheated flag (grey italic tooltip: *This item was summoned through cheating means.*).
- Also removes vanilla debug weapons `SwordCheat` and `SledgeCheat`.
- Cleans online player inventories, loaded containers, world drops, and item/armor stands.
- Optional overstack / over-quality deletion and extra prefab names.
- Console command `nocheatitems_sweep`.
