using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace TerreZero.Chimeres
{
    public sealed class ChimereSaveEntry
    {
        public string SpeciesId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int MaxStability { get; set; }
        public int Bond { get; set; }
        public int Training { get; set; }
        public int EvolutionStage { get; set; }
    }

    public sealed class ChimereSaveData
    {
        public int CaptureModules { get; set; }
        public int TrainingPoints { get; set; }
        public List<ChimereSaveEntry> Team { get; set; } = new();
        public List<ChimereSaveEntry> Reserve { get; set; } = new();
    }

    public static class ChimereSaveStore
    {
        private const string SaveFile = "user://chimeres.json";

        public static void Save()
        {
            var data = new ChimereSaveData
            {
                CaptureModules = ChimereGameState.CaptureModules,
                TrainingPoints = ChimereGameState.TrainingPoints
            };

            foreach (var chimere in ChimereGameState.Roster.Team)
                data.Team.Add(ToEntry(chimere));
            foreach (var chimere in ChimereGameState.Roster.Reserve)
                data.Reserve.Add(ToEntry(chimere));

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            );

            string path = ProjectSettings.GlobalizePath(SaveFile);
            File.WriteAllText(path, json);
        }

        public static bool Load()
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);
            if (!File.Exists(path))
                return false;

            try
            {
                var data = JsonSerializer.Deserialize<ChimereSaveData>(
                    File.ReadAllText(path)
                );
                if (data == null)
                    return false;

                ChimereGameState.Roster.ReplaceAll(
                    RestoreList(data.Team),
                    RestoreList(data.Reserve)
                );
                ChimereGameState.CaptureModules = Math.Max(0, data.CaptureModules);
                ChimereGameState.TrainingPoints = Math.Max(0, data.TrainingPoints);
                return true;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[CHIMÈRES] sauvegarde illisible : {ex.Message}");
                return false;
            }
        }

        private static List<ChimereCombatant> RestoreList(List<ChimereSaveEntry> entries)
        {
            var result = new List<ChimereCombatant>();
            foreach (var entry in entries ?? new List<ChimereSaveEntry>())
            {
                var chimere = ChimereSpeciesCatalog.CreateBySpecies(
                    entry.SpeciesId,
                    Math.Max(1, entry.Level)
                );

                chimere.Name = string.IsNullOrWhiteSpace(entry.Name)
                    ? chimere.Name
                    : entry.Name;
                chimere.Experience = Math.Max(0, entry.Experience);
                chimere.MaxHp = Math.Max(1, entry.MaxHp);
                chimere.CurrentHp = chimere.MaxHp;
                chimere.Attack = Math.Max(1, entry.Attack);
                chimere.Defense = Math.Max(0, entry.Defense);
                chimere.Speed = Math.Max(1, entry.Speed);
                chimere.MaxStability = Math.Max(1, entry.MaxStability);
                chimere.Stability = chimere.MaxStability;
                chimere.Bond = Math.Clamp(entry.Bond, 0, 100);
                chimere.Training = Math.Max(0, entry.Training);
                chimere.EvolutionStage = Math.Clamp(entry.EvolutionStage, 0, 2);
                result.Add(chimere);
            }
            return result;
        }

        private static ChimereSaveEntry ToEntry(ChimereCombatant chimere) =>
            new()
            {
                SpeciesId = chimere.SpeciesId,
                Name = chimere.Name,
                Level = chimere.Level,
                Experience = chimere.Experience,
                MaxHp = chimere.MaxHp,
                Attack = chimere.Attack,
                Defense = chimere.Defense,
                Speed = chimere.Speed,
                MaxStability = chimere.MaxStability,
                Bond = chimere.Bond,
                Training = chimere.Training,
                EvolutionStage = chimere.EvolutionStage
            };
    }
}
