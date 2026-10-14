namespace NoCheatItems
{
    // Counts are stacks/attachments, not the sum of stack quantities.
    internal sealed class SweepStats
    {
        internal int PlayersFound;
        internal int PlayersScanned;
        internal int ContainersFound;
        internal int ContainersScanned;
        internal int ContainersSkipped;
        internal int ContainersOwned;
        internal int InventoriesScanned;
        internal int MissingInventories;
        internal int InventoryStacks;
        internal int FlaggedInventoryStacks;
        internal int InventoryMatches;
        internal int InventoryRemovals;
        internal int WorldDropsFound;
        internal int WorldDropsScanned;
        internal int WorldDropsSkipped;
        internal int FlaggedWorldDrops;
        internal int WorldDropMatches;
        internal int ItemStandsFound;
        internal int ArmorStandsFound;
        internal int StandsScanned;
        internal int StandsSkipped;
        internal int StandAttachments;
        internal int StandMatches;

        internal void LogSummary(string context, bool failed)
        {
            CheatCleaner.DebugLog($"sweep ({context}) complete: failed={failed}; " +
                $"playersFound={PlayersFound}, playersScanned={PlayersScanned}; " +
                $"containersFound={ContainersFound}, containersScanned={ContainersScanned}, containersSkipped={ContainersSkipped}, containersOwned={ContainersOwned}; " +
                $"inventoriesScanned={InventoriesScanned}, missingInventories={MissingInventories}, inventoryStacks={InventoryStacks}, flaggedInventoryStacks={FlaggedInventoryStacks}, inventoryMatches={InventoryMatches}, inventoryRemovalsOrDryRun={InventoryRemovals}; " +
                $"worldDropsFound={WorldDropsFound}, worldDropsScanned={WorldDropsScanned}, worldDropsSkipped={WorldDropsSkipped}, flaggedWorldDrops={FlaggedWorldDrops}, worldDropMatches={WorldDropMatches}; " +
                $"itemStandsFound={ItemStandsFound}, armorStandsFound={ArmorStandsFound}, standsScanned={StandsScanned}, standsSkipped={StandsSkipped}, standAttachments={StandAttachments}, standMatches={StandMatches}; " +
                $"DryRun={ModConfig.DryRun.Value}");
        }
    }
}
