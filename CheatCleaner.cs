using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoCheatItems
{
    internal static class CheatCleaner
    {
        internal static readonly string[] DefaultCheatPrefabs =
        {
            "SwordCheat",
            "SledgeCheat"
        };

        private static bool _sweeping;
        private static bool _reloadingDrop;
        private static int _inventoryChangedHooks;
        private static int _guardedInventorySweeps;

        internal static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                // Info is intentional: BepInEx's default log filter hides LogDebug messages.
                NoCheatItemsPlugin.Log.LogInfo("[debug] " + message);
            }
        }

        internal static void RecordInventoryChangedHook()
        {
            if (ModConfig.DebugLogging.Value)
            {
                _inventoryChangedHooks++;
            }
        }

        internal static void LogDebugHeartbeat(string context)
        {
            if (!ModConfig.DebugLogging.Value)
            {
                return;
            }

            DebugLog($"heartbeat ({context}): ZNetPresent={ZNet.instance != null}, IsServer={IsServer()}, Enabled={ModConfig.Enabled.Value}, Sweeping={_sweeping}; " +
                $"inventoryChangedHooks={_inventoryChangedHooks}, guardedInventorySweeps={_guardedInventorySweeps} since previous heartbeat; " +
                $"ScanPlayers={ModConfig.ScanPlayers.Value}, ScanContainers={ModConfig.ScanContainers.Value}, ScanWorldDrops={ModConfig.ScanWorldDrops.Value}, ScanItemStands={ModConfig.ScanItemStands.Value}; " +
                $"RemoveFlaggedItems={ModConfig.RemoveFlaggedItems.Value}, DryRun={ModConfig.DryRun.Value}, intervalSeconds={ModConfig.ScanIntervalSeconds.Value}, timeScale={Time.timeScale}, deltaTime={Time.deltaTime}");
            _inventoryChangedHooks = 0;
            _guardedInventorySweeps = 0;
        }

        internal static bool IsServer()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        internal static bool IsCheatItem(ItemDrop.ItemData item, out string reason)
        {
            reason = null;
            if (item == null)
            {
                return false;
            }

            if (ModConfig.RemoveFlaggedItems.Value && item.m_cheated)
            {
                reason = "1.0 cheated flag";
                return true;
            }

            string prefab = GetPrefabName(item);
            if (IsBannedPrefab(prefab))
            {
                reason = "cheat prefab " + prefab;
                return true;
            }

            var shared = item.m_shared;
            if (shared != null)
            {
                if (ModConfig.RemoveOverstacked.Value && shared.m_maxStackSize > 0 && item.m_stack > shared.m_maxStackSize)
                {
                    reason = $"overstacked {item.m_stack}/{shared.m_maxStackSize}";
                    return true;
                }

                if (ModConfig.RemoveOverquality.Value && shared.m_maxQuality > 0 && item.m_quality > shared.m_maxQuality)
                {
                    reason = $"overquality {item.m_quality}/{shared.m_maxQuality}";
                    return true;
                }
            }

            return false;
        }

        internal static bool IsBannedPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
            {
                return false;
            }

            if (ModConfig.RemoveKnownCheatPrefabs.Value)
            {
                foreach (string name in DefaultCheatPrefabs)
                {
                    if (prefab.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            string extra = ModConfig.ExtraPrefabNames.Value;
            if (string.IsNullOrWhiteSpace(extra))
            {
                return false;
            }

            foreach (string token in extra.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (prefab.Equals(token.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string GetPrefabName(ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab != null)
            {
                return StripClone(item.m_dropPrefab.name);
            }

            return item?.m_shared != null ? item.m_shared.m_name : "";
        }

        internal static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            int index = name.IndexOf('(');
            return index > 0 ? name.Substring(0, index).Trim() : name;
        }

        internal static int SweepInventory(Inventory inventory, string context, Player owner = null, SweepStats stats = null)
        {
            if (inventory == null || !ModConfig.Enabled.Value)
            {
                if (stats != null && inventory == null) stats.MissingInventories++;
                if (ModConfig.DebugLogging.Value)
                {
                    DebugLog($"inventory ({context}) skipped: inventoryPresent={inventory != null}, Enabled={ModConfig.Enabled.Value}");
                }
                return 0;
            }

            bool debug = ModConfig.DebugLogging.Value;
            List<ItemDrop.ItemData> all = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            if (!debug && all.Count == 0)
            {
                return 0;
            }
            var doomed = new List<ItemDrop.ItemData>();
            var reasons = new List<string>();
            int flagged = 0;
            foreach (ItemDrop.ItemData item in all)
            {
                if (debug && item != null && item.m_cheated) flagged++;
                if (IsCheatItem(item, out string reason))
                {
                    doomed.Add(item);
                    reasons.Add(reason);
                }
            }

            if (stats != null)
            {
                stats.InventoriesScanned++;
                stats.InventoryStacks += all.Count;
                stats.FlaggedInventoryStacks += flagged;
                stats.InventoryMatches += doomed.Count;
            }
            if (debug)
            {
                DebugLog($"inventory ({context}): stacks={all.Count}, rawCheatedFlags={flagged}, matches={doomed.Count}, DryRun={ModConfig.DryRun.Value}");
            }

            if (doomed.Count == 0)
            {
                return 0;
            }

            if (owner == null)
            {
                owner = FindPlayer(inventory);
            }

            int removed = 0;
            for (int i = 0; i < doomed.Count; i++)
            {
                ItemDrop.ItemData item = doomed[i];
                string reason = reasons[i];
                string label = Describe(item);

                if (ModConfig.LogDeletions.Value)
                {
                    string who = owner != null ? owner.GetPlayerName() : context;
                    NoCheatItemsPlugin.Log.LogInfo($"[{(ModConfig.DryRun.Value ? "dry-run" : "delete")}] {label} ({reason}) from {who}");
                }

                if (ModConfig.DryRun.Value)
                {
                    removed++;
                    continue;
                }

                if (owner != null && item.m_equipped)
                {
                    owner.UnequipItem(item, triggerEquipEffects: false);
                }

                inventory.RemoveItem(item);
                removed++;
            }

            if (removed > 0 && owner != null && ModConfig.NotifyPlayers.Value && !ModConfig.DryRun.Value)
            {
                owner.Message(MessageHud.MessageType.Center, removed == 1
                    ? "Cheated item removed."
                    : $"{removed} cheated items removed.");
            }

            return removed;
        }

        internal static void QueueWorldDropCheck(ItemDrop drop)
        {
            if (!ModConfig.Enabled.Value || !ModConfig.ScanWorldDrops.Value || !IsServer() || drop == null)
            {
                return;
            }

            if (NoCheatItemsPlugin.Instance != null)
            {
                NoCheatItemsPlugin.Instance.StartCoroutine(WorldDropNextFrame(drop));
                return;
            }

            HandleWorldDrop(drop);
        }

        private static IEnumerator WorldDropNextFrame(ItemDrop drop)
        {
            yield return null;
            if (drop != null)
            {
                HandleWorldDrop(drop);
            }
        }

        internal static void HandleWorldDrop(ItemDrop drop, SweepStats stats = null)
        {
            if (!ModConfig.Enabled.Value || !ModConfig.ScanWorldDrops.Value || !IsServer() || drop == null)
            {
                return;
            }

            if (!HasValidNetView(drop))
            {
                if (stats != null) stats.WorldDropsSkipped++;
                return;
            }

            if (stats != null) stats.WorldDropsScanned++;

            // Creature loot sets m_cheated after Instantiate/Awake. Do not Load() first or a
            // stale ZDO can overwrite the in-memory flag. Reload only if we do not already see it.
            if (!IsCheatItem(drop.m_itemData, out string reason))
            {
                TryReloadDrop(drop);
            }

            if (stats != null && drop.m_itemData != null && drop.m_itemData.m_cheated) stats.FlaggedWorldDrops++;

            if (!IsCheatItem(drop.m_itemData, out reason) && !IsBannedPrefab(StripClone(drop.gameObject.name)))
            {
                return;
            }

            if (stats != null) stats.WorldDropMatches++;
            string label = Describe(drop.m_itemData);
            if (string.IsNullOrEmpty(label))
            {
                label = StripClone(drop.gameObject.name);
            }

            if (ModConfig.LogDeletions.Value)
            {
                Vector3 pos = drop.transform.position;
                NoCheatItemsPlugin.Log.LogInfo($"[{(ModConfig.DryRun.Value ? "dry-run" : "delete")}] world drop {label} ({reason ?? "cheat prefab"}) at {pos.x:F0},{pos.y:F0},{pos.z:F0}");
            }

            if (ModConfig.DryRun.Value)
            {
                return;
            }

            DestroyNetworked(drop.gameObject);
        }

        internal static void SweepLoadedWorld(string context)
        {
            bool isServer = IsServer();
            if (_sweeping || !ModConfig.Enabled.Value || !isServer)
            {
                if (ModConfig.DebugLogging.Value)
                {
                    DebugLog($"sweep ({context}) skipped: Sweeping={_sweeping}, Enabled={ModConfig.Enabled.Value}, ZNetPresent={ZNet.instance != null}, IsServer={isServer}");
                }
                return;
            }

            SweepStats stats = ModConfig.DebugLogging.Value ? new SweepStats() : null;
            bool failed = false;
            if (stats != null) DebugLog($"sweep ({context}) start: IsServer={isServer}, DryRun={ModConfig.DryRun.Value}");
            _sweeping = true;
            try
            {
                if (ModConfig.ScanPlayers.Value)
                {
                    List<Player> players = Player.GetAllPlayers();
                    if (stats != null) stats.PlayersFound = players?.Count ?? 0;
                    if (players != null)
                    {
                        foreach (Player player in players)
                        {
                            if (player == null)
                            {
                                continue;
                            }

                            if (stats != null) stats.PlayersScanned++;
                            int removed = SweepInventory(player.GetInventory(), "player " + player.GetPlayerName(), player, stats);
                            if (stats != null) stats.InventoryRemovals += removed;
                        }
                    }
                }

                if (ModConfig.ScanContainers.Value)
                {
                    Container[] containers = FindLoaded<Container>();
                    if (stats != null) stats.ContainersFound = containers.Length;
                    foreach (Container container in containers)
                    {
                        if (container == null || !HasValidNetView(container))
                        {
                            if (stats != null) stats.ContainersSkipped++;
                            continue;
                        }

                        if (stats != null)
                        {
                            stats.ContainersScanned++;
                            if (container.GetComponent<ZNetView>().IsOwner()) stats.ContainersOwned++;
                        }
                        int removed = SweepInventory(container.GetInventory(), DescribeContainer(container), stats: stats);
                        if (stats != null) stats.InventoryRemovals += removed;
                    }
                }

                if (ModConfig.ScanWorldDrops.Value)
                {
                    SweepWorldDrops(stats);
                }

                if (ModConfig.ScanItemStands.Value)
                {
                    SweepStands(stats);
                }
            }
            catch (Exception ex)
            {
                failed = true;
                NoCheatItemsPlugin.Log.LogError($"Sweep ({context}) failed: {ex}");
            }
            finally
            {
                _sweeping = false;
                stats?.LogSummary(context, failed);
            }
        }

        internal static bool Sweeping => _sweeping;

        internal static void SweepWorldDrops(SweepStats stats = null)
        {
            ItemDrop[] drops = FindLoaded<ItemDrop>();
            if (stats != null) stats.WorldDropsFound = drops.Length;
            foreach (ItemDrop drop in drops)
            {
                if (drop == null)
                {
                    if (stats != null) stats.WorldDropsSkipped++;
                    continue;
                }
                HandleWorldDrop(drop, stats);
            }
        }

        private static void TryReloadDrop(ItemDrop drop)
        {
            if (_reloadingDrop || drop == null)
            {
                return;
            }

            _reloadingDrop = true;
            try
            {
                drop.Load();
            }
            finally
            {
                _reloadingDrop = false;
            }
        }

        internal static void RunGuardedInventorySweep(Inventory inventory)
        {
            if (_sweeping || inventory == null || !ModConfig.Enabled.Value || !IsServer())
            {
                return;
            }

            bool scanPlayers = ModConfig.ScanPlayers.Value;
            bool scanContainers = ModConfig.ScanContainers.Value;
            if (!scanPlayers && !scanContainers)
            {
                return;
            }

            Player owner = FindPlayer(inventory);
            if (owner != null)
            {
                if (!scanPlayers)
                {
                    return;
                }
            }
            else if (!scanContainers)
            {
                return;
            }

            _sweeping = true;
            try
            {
                if (ModConfig.DebugLogging.Value) _guardedInventorySweeps++;
                SweepInventory(inventory, owner != null ? "player " + owner.GetPlayerName() : "container", owner);
            }
            finally
            {
                _sweeping = false;
            }
        }

        private static void SweepStands(SweepStats stats = null)
        {
            ItemStand[] itemStands = FindLoaded<ItemStand>();
            if (stats != null) stats.ItemStandsFound = itemStands.Length;
            foreach (ItemStand stand in itemStands)
            {
                if (stand == null || !HasValidNetView(stand))
                {
                    if (stats != null) stats.StandsSkipped++;
                    continue;
                }

                if (stats != null) stats.StandsScanned++;
                if (!stand.HaveAttachment()) continue;
                if (stats != null) stats.StandAttachments++;

                string attached = PrefabNameFromHash(stand.GetAttachedItem());
                if (string.IsNullOrEmpty(attached))
                {
                    attached = stand.m_currentItemName;
                }

                if (string.IsNullOrEmpty(attached) || !IsBannedPrefab(StripClone(attached)))
                {
                    continue;
                }

                LogStand("item stand", attached, stand.transform.position);
                if (stats != null) stats.StandMatches++;
                if (!ModConfig.DryRun.Value)
                {
                    stand.DestroyAttachment();
                }
            }

            ArmorStand[] armorStands = FindLoaded<ArmorStand>();
            if (stats != null) stats.ArmorStandsFound = armorStands.Length;
            foreach (ArmorStand stand in armorStands)
            {
                if (stand == null || !HasValidNetView(stand))
                {
                    if (stats != null) stats.StandsSkipped++;
                    continue;
                }

                if (stats != null) stats.StandsScanned++;

                for (int i = 0; i < 16; i++)
                {
                    if (!stand.HaveAttachment(i))
                    {
                        continue;
                    }

                    if (stats != null) stats.StandAttachments++;

                    string attached = PrefabNameFromHash(stand.GetAttachedItem(i));
                    if (string.IsNullOrEmpty(attached) || !IsBannedPrefab(StripClone(attached)))
                    {
                        continue;
                    }

                    LogStand("armor stand", attached, stand.transform.position);
                    if (stats != null) stats.StandMatches++;
                    if (!ModConfig.DryRun.Value)
                    {
                        stand.DestroyAttachment(i);
                    }
                }
            }
        }

        private static void LogStand(string kind, string prefab, Vector3 pos)
        {
            if (!ModConfig.LogDeletions.Value)
            {
                return;
            }

            NoCheatItemsPlugin.Log.LogInfo($"[{(ModConfig.DryRun.Value ? "dry-run" : "delete")}] {kind} {prefab} at {pos.x:F0},{pos.y:F0},{pos.z:F0}");
        }

        private static T[] FindLoaded<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private static string PrefabNameFromHash(int hash)
        {
            if (hash == 0 || ObjectDB.instance == null)
            {
                return "";
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(hash);
            return prefab != null ? StripClone(prefab.name) : "";
        }

        private static bool HasValidNetView(Component component)
        {
            if (component == null)
            {
                return false;
            }

            ZNetView view = component.GetComponent<ZNetView>();
            return view != null && view.IsValid();
        }

        private static void DestroyNetworked(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(go);
            }
            else
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private static Player FindPlayer(Inventory inventory)
        {
            List<Player> players = Player.GetAllPlayers();
            if (players == null)
            {
                return null;
            }

            foreach (Player player in players)
            {
                if (player != null && player.GetInventory() == inventory)
                {
                    return player;
                }
            }

            return null;
        }

        private static string Describe(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return "item";
            }

            string prefab = GetPrefabName(item);
            string loc = item.m_shared != null ? item.m_shared.m_name : "";
            int stack = item.m_stack;
            if (!string.IsNullOrEmpty(prefab) && prefab != loc)
            {
                return $"{prefab} x{stack} ({loc})";
            }

            return string.IsNullOrEmpty(prefab) ? $"stack {stack}" : $"{prefab} x{stack}";
        }

        private static string DescribeContainer(Container container)
        {
            string name = !string.IsNullOrEmpty(container.m_name) ? container.m_name : StripClone(container.gameObject.name);
            Vector3 pos = container.transform.position;
            return $"{name} at {pos.x:F0},{pos.y:F0},{pos.z:F0}";
        }
    }
}
