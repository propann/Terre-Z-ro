using System;
using System.IO;
using System.Text.Json;
using Godot;
using TerreZero.Chimeres;

namespace TerreZero.Gameplay
{
    public sealed class GlobalSaveData
    {
        public int SaveVersion { get; set; } = GameState.SaveVersion;
        public PlayerProfileState Player { get; set; } = new();
        public InventoryState Inventory { get; set; } = new();
        public QuestJournalState Quests { get; set; } = new();
    }

    public static class GlobalSaveStore
    {
        private const string SaveFile = "user://terre_zero_save.json";

        public static void SaveAll()
        {
            var data = new GlobalSaveData
            {
                SaveVersion = GameState.SaveVersion,
                Player = ClonePlayer(GameState.Player),
                Inventory = CloneInventory(GameState.Inventory),
                Quests = CloneQuests(GameState.Quests)
            };

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(
                ProjectSettings.GlobalizePath(SaveFile),
                json
            );

            ChimereSaveStore.Save();
        }

        public static bool LoadAll()
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);

            if (!File.Exists(path))
            {
                GameState.SeedNewGameInventory();
                GameState.Quests.SeedIntroQuests();
                ChimereSaveStore.Load();
                return false;
            }

            try
            {
                var data = JsonSerializer.Deserialize<GlobalSaveData>(
                    File.ReadAllText(path)
                );

                if (data == null)
                    return false;

                CopyPlayer(data.Player ?? new PlayerProfileState(), GameState.Player);
                CopyInventory(data.Inventory ?? new InventoryState(), GameState.Inventory);
                CopyQuests(data.Quests ?? new QuestJournalState(), GameState.Quests);

                GameState.Inventory.Normalize();
                GameState.SeedNewGameInventory();
                GameState.Quests.SeedIntroQuests();
                ChimereSaveStore.Load();
                return true;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[SAVE] sauvegarde globale illisible : {ex.Message}");
                GameState.SeedNewGameInventory();
                GameState.Quests.SeedIntroQuests();
                ChimereSaveStore.Load();
                return false;
            }
        }

        private static PlayerProfileState ClonePlayer(PlayerProfileState p) =>
            new()
            {
                Level = p.Level,
                Experience = p.Experience,
                MaxHealth = p.MaxHealth,
                Health = p.Health,
                MaxStamina = p.MaxStamina,
                Stamina = p.Stamina,
                Radiation = p.Radiation,
                Credits = p.Credits,
                MasteryPoints = p.MasteryPoints,
                DistanceWalkedKm = p.DistanceWalkedKm,
                ChimeresCaptured = p.ChimeresCaptured,
                BattlesWon = p.BattlesWon,
                BuildingsExplored = p.BuildingsExplored
            };

        private static InventoryState CloneInventory(InventoryState inventory) =>
            new()
            {
                MaxWeightKg = inventory.MaxWeightKg,
                Stacks = new System.Collections.Generic.List<InventoryStack>(
                    inventory.Stacks.ConvertAll(s => new InventoryStack
                    {
                        ItemId = s.ItemId,
                        Quantity = s.Quantity
                    })
                )
            };

        private static QuestJournalState CloneQuests(QuestJournalState journal)
        {
            string json = JsonSerializer.Serialize(journal);
            return JsonSerializer.Deserialize<QuestJournalState>(json)
                ?? new QuestJournalState();
        }

        private static void CopyPlayer(
            PlayerProfileState source,
            PlayerProfileState target)
        {
            target.Level = source.Level;
            target.Experience = source.Experience;
            target.MaxHealth = source.MaxHealth;
            target.Health = source.Health;
            target.MaxStamina = source.MaxStamina;
            target.Stamina = source.Stamina;
            target.Radiation = source.Radiation;
            target.Credits = source.Credits;
            target.MasteryPoints = source.MasteryPoints;
            target.DistanceWalkedKm = source.DistanceWalkedKm;
            target.ChimeresCaptured = source.ChimeresCaptured;
            target.BattlesWon = source.BattlesWon;
            target.BuildingsExplored = source.BuildingsExplored;
        }

        private static void CopyInventory(
            InventoryState source,
            InventoryState target)
        {
            target.MaxWeightKg = source.MaxWeightKg;
            target.Stacks.Clear();

            foreach (var stack in source.Stacks)
            {
                target.Stacks.Add(new InventoryStack
                {
                    ItemId = stack.ItemId,
                    Quantity = stack.Quantity
                });
            }
        }

        private static void CopyQuests(
            QuestJournalState source,
            QuestJournalState target)
        {
            target.Quests.Clear();
            target.Quests.AddRange(source.Quests);
        }
    }
}
