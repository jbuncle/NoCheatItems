# NoCheatItems

Server-side BepInEx plugin for Valheim 1.0. It deletes items that the game itself has marked as cheated, plus the old debug weapons, from the **server's** loaded world and from **online** player inventories.

Clients do not need this mod. Install it on the dedicated server (or the listen-server host).

## What it removes

Valheim 1.0 stamps spawned items with a persistent flag (`ItemData.m_cheated`). Those stacks show a grey italic tooltip:

> *This item was summoned through cheating means.*

If a clean character picks one up, the game puts them in a **temporary cheat state** until every flagged item is gone. This plugin deletes those items so they never sit in chests, on the ground, or in a player's bag on your server.

By default it also deletes the vanilla debug prefabs:

- `SwordCheat`
- `SledgeCheat`

Optional (off by default): stacks above max stack size, quality above max quality, and extra prefab names you list in the config.

## What it cannot do

Valheim keeps character files on the **client**. The dedicated server only sees a player's inventory while they are connected. Offline bags are cleaned the next time that player joins.

Unloaded zones are cleaned when they stream in (a chest or dropped pile is scanned as soon as that area is loaded). Vanilla can stamp creature loot with the same cheated flag (the boar drop you saw); those piles are deleted on the dedicated server. Clients still do not need this mod for that.

This does not un-flag a character or world that already used `devcommands`. It only destroys cheated **items**.

## Install

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) on the dedicated server.
2. Copy `NoCheatItems.dll` into `BepInEx/plugins/`.
3. Start the server once to generate `BepInEx/config/ageous27.nocheatitems.cfg`.

## Console

On the server console:

```
nocheatitems_sweep
```

Sweeps every loaded player, container, world drop, and item stand immediately.

## Config

| Setting | Default | Meaning |
| --- | --- | --- |
| `Enabled` | true | Master switch |
| `RemoveFlaggedItems` | true | Delete the 1.0 cheated flag |
| `RemoveKnownCheatPrefabs` | true | Delete SwordCheat / SledgeCheat |
| `ExtraPrefabNames` | empty | Extra prefab names, comma-separated |
| `RemoveOverstacked` | false | Delete illegal stack sizes |
| `RemoveOverquality` | false | Delete illegal upgrade levels |
| `ScanPlayers` | true | Online inventories |
| `ScanWorldDrops` | true | Items on the ground |
| `ScanContainers` | true | Chests, carts, ships, tombstones, Obliterator |
| `ScanItemStands` | true | Item stands and armor stands |
| `ScanIntervalSeconds` | 8 | Safety-net sweep while the world is loaded |
| `NotifyPlayers` | true | Center-screen message when their items are taken |
| `LogDeletions` | true | BepInEx log line per item |
| `DryRun` | false | Log only, do not delete |

Pickup and inventory changes are cleaned immediately; the interval is a backup for stands and anything a patch missed.

## Build

Requires a .NET 8 SDK or newer and a current Valheim 1.0 client or dedicated-server installation so the project can reference the game's assemblies. Version 1.0.3 resolves `Inventory.Changed` at runtime rather than requiring the 1.0.14 parameter signature, to accommodate 1.0.17 and later compatible updates. This does not guarantee compatibility with future game API changes; verify the plugin on your server after updating.

```bat
dotnet build -c Release
```

If Valheim is not at `D:\Program Files\Steam\steamapps\common\Valheim`, pass `/p:ValheimPath="C:\path\to\Valheim"`.

Output: `bin\Release\NoCheatItems.dll`.

For a dedicated-server install, use the same `ValheimPath` option; the project detects `valheim_server_Data/Managed`. You can also point directly at either installation's assemblies with `-p:ValheimManagedPath="C:\path\to\Managed"`.

### GitHub Actions

GitHub Actions is optional if you can build locally. The included **Build** workflow runs on pushes and pull requests, or manually from **Actions → Build → Run workflow**. It downloads the current public Valheim dedicated server through SteamCMD (anonymous login), builds the plugin, and uploads a **NoCheatItems** artifact containing the DLL and a Thunderstore package ZIP. No Steam credentials are required. Game assemblies are excluded from the package.

Push these changes to GitHub, wait for the workflow to succeed, and download the artifact from its run page. Extract `NoCheatItems.dll` and install it as described above. The workflow builds against the latest public server, rather than pinning 1.0.17; a successful compile still needs an in-game check of plugin startup, inventory cleanup, and world-drop cleanup. Use `DryRun = true` for the initial check.

Pushing a version tag such as `v1.0.3+jbuncle.1` also publishes a GitHub Release after the build succeeds, with `NoCheatItems.dll` and `NoCheatItems-1.0.3.zip` attached automatically. The part before `+` must match `v` plus the manifest version; the suffix identifies the fork build. Release notes are read from `.github/releases/<package-version>.md`. Update these notes and the release title when preparing later versions. Branch and pull-request builds only upload Actions artifacts.
