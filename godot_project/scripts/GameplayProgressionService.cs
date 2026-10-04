using System;
using TerreZero.Chimeres;

namespace TerreZero.Gameplay
{
    public static class GameplayProgressionService
    {
        public static void RegisterCapture(ChimereCombatant chimere)
        {
            GameState.Player.ChimeresCaptured++;
            GameState.Player.AddExperience(
                chimere.Rarity switch
                {
                    ChimereRarity.Alpha => 70,
                    ChimereRarity.Rare => 40,
                    _ => 22
                }
            );

            GameState.Quests.RegisterProgress(
                QuestObjectiveType.CaptureChimere,
                chimere.SpeciesId,
                1
            );
        }

        public static LootBundle RegisterBattleVictory(
            ChimereCombatant chimere,
            string context,
            int seed)
        {
            GameState.Player.BattlesWon++;
            GameState.Player.AddExperience(
                16 + chimere.Level * 4 +
                (chimere.Rarity == ChimereRarity.Alpha ? 30 : 0)
            );

            GameState.Quests.RegisterProgress(
                QuestObjectiveType.WinBattle,
                chimere.SpeciesId,
                1
            );

            LootBundle loot = LootDirector.Generate(
                $"chimere:{chimere.SpeciesId}:{chimere.Id}",
                context,
                seed
            );

            ApplyLoot(loot);
            return loot;
        }

        public static void RegisterBuildingExplored(string buildingType)
        {
            GameState.Player.BuildingsExplored++;
            GameState.Quests.RegisterProgress(
                QuestObjectiveType.ExploreBuilding,
                buildingType ?? string.Empty,
                1
            );
        }

        public static void RegisterDistance(double kilometers)
        {
            if (kilometers <= 0)
                return;

            GameState.Player.DistanceWalkedKm += kilometers;

            int meters = (int)Math.Round(kilometers * 1000.0);
            GameState.Quests.RegisterProgress(
                QuestObjectiveType.ReachDistance,
                string.Empty,
                meters
            );
        }

        public static int ApplyLoot(LootBundle bundle)
        {
            int totalAdded = 0;

            foreach (var entry in bundle.Entries)
            {
                int added = GameState.Inventory.Add(
                    entry.ItemId,
                    entry.Quantity
                );

                if (added <= 0)
                    continue;

                totalAdded += added;
                GameState.Quests.RegisterProgress(
                    QuestObjectiveType.CollectItem,
                    entry.ItemId,
                    added
                );
            }

            return totalAdded;
        }

        public static bool TryUseMedGel()
        {
            if (!GameState.Inventory.Remove("med_gel", 1))
                return false;

            ItemDefinition gel = ItemCatalog.Get("med_gel");
            GameState.Player.Heal(gel?.HealAmount ?? 25);
            return true;
        }
    }
}
