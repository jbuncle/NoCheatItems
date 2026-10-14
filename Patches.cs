using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace NoCheatItems
{
    [HarmonyPatch]
    internal static class InventoryChangedPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Changed's parameters vary between game updates; the postfix only needs the inventory.
            var targets = new List<MethodBase>();
            foreach (MethodInfo method in typeof(Inventory).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method.Name == "Changed" && method.ReturnType == typeof(void) && !method.ContainsGenericParameters)
                {
                    targets.Add(method);
                }
            }

            if (targets.Count == 0)
            {
                throw new MissingMethodException("NoCheatItems could not find Inventory.Changed in this Valheim version.");
            }

            return targets;
        }

        private static void Postfix(Inventory __instance)
        {
            CheatCleaner.RunGuardedInventorySweep(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    internal static class ItemDropAwakePatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            // DropItems assigns m_cheated after Instantiate, so wait one frame.
            CheatCleaner.QueueWorldDropCheck(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.DropItems))]
    internal static class CharacterDropItemsPatch
    {
        private static void Postfix()
        {
            if (!ModConfig.Enabled.Value || !ModConfig.ScanWorldDrops.Value || !CheatCleaner.IsServer())
            {
                return;
            }

            CheatCleaner.SweepWorldDrops();
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Load))]
    internal static class ItemDropLoadPatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            CheatCleaner.HandleWorldDrop(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class PlayerSpawnedPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!ModConfig.Enabled.Value || !ModConfig.ScanPlayers.Value || !CheatCleaner.IsServer())
            {
                return;
            }

            CheatCleaner.RunGuardedInventorySweep(__instance.GetInventory());
        }
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class TerminalInitPatch
    {
        private static void Postfix()
        {
            new Terminal.ConsoleCommand("nocheatitems_sweep", "Sweep loaded cheated items now", args =>
            {
                if (!CheatCleaner.IsServer())
                {
                    args.Context.AddString("NoCheatItems only runs on the server.");
                    return;
                }

                CheatCleaner.SweepLoadedWorld("console");
                args.Context.AddString("NoCheatItems sweep complete. See BepInEx log for details.");
            }, isCheat: false, isNetwork: false, onlyServer: true);
        }
    }
}
