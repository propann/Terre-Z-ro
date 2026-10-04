using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace TerreZero.Bunker
{
    public static class BunkerSaveStore
    {
        private const string SaveFile = "user://bunker.json";

        public static void Save()
        {
            string json = JsonSerializer.Serialize(
                BunkerService.State,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(
                ProjectSettings.GlobalizePath(SaveFile),
                json
            );
        }

        public static bool Load()
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);
            if (!File.Exists(path))
                return false;

            try
            {
                BunkerState data = JsonSerializer.Deserialize<BunkerState>(
                    File.ReadAllText(path)
                );

                if (data == null)
                    return false;

                BunkerService.State.Level = data.Level;
                BunkerService.State.Energy = data.Energy;
                BunkerService.State.Defense = data.Defense;
                BunkerService.State.StorageBonusKg = data.StorageBonusKg;
                BunkerService.State.RefineryBuilt = data.RefineryBuilt;
                BunkerService.State.TrainingBayBuilt = data.TrainingBayBuilt;
                BunkerService.State.MedicalBayBuilt = data.MedicalBayBuilt;
                return true;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[BUNKER] sauvegarde illisible : {ex.Message}");
                return false;
            }
        }
    }
}
