using System;
using TerreZero.Gameplay;

namespace TerreZero.Bunker
{
    public sealed class BunkerState
    {
        public int Level { get; set; } = 1;
        public int Energy { get; set; } = 40;
        public int Defense { get; set; } = 100;
        public int StorageBonusKg { get; set; }
        public bool RefineryBuilt { get; set; }
        public bool TrainingBayBuilt { get; set; }
        public bool MedicalBayBuilt { get; set; }
    }

    public static class BunkerService
    {
        public static BunkerState State { get; } = new();

        public static bool TryBuildRefinery(out string message)
        {
            if (State.RefineryBuilt)
            {
                message = "Raffinerie déjà construite.";
                return false;
            }

            if (!ConsumeResources(12, 6, 3))
            {
                message = "Ressources insuffisantes pour la raffinerie.";
                return false;
            }

            State.RefineryBuilt = true;
            State.Level++;
            State.Energy += 10;
            message = "Raffinerie opérationnelle.";
            GlobalSaveStore.SaveAll();
            return true;
        }

        public static bool TryBuildTrainingBay(out string message)
        {
            if (State.TrainingBayBuilt)
            {
                message = "Zone de dressage déjà construite.";
                return false;
            }

            if (!ConsumeResources(10, 3, 2))
            {
                message = "Ressources insuffisantes pour la zone de dressage.";
                return false;
            }

            State.TrainingBayBuilt = true;
            State.Level++;
            message = "Zone de dressage opérationnelle.";
            GlobalSaveStore.SaveAll();
            return true;
        }

        public static bool TryBuildMedicalBay(out string message)
        {
            if (State.MedicalBayBuilt)
            {
                message = "Infirmerie déjà construite.";
                return false;
            }

            if (!ConsumeResources(8, 4, 2))
            {
                message = "Ressources insuffisantes pour l'infirmerie.";
                return false;
            }

            State.MedicalBayBuilt = true;
            State.Level++;
            message = "Infirmerie opérationnelle.";
            GlobalSaveStore.SaveAll();
            return true;
        }

        public static bool TryExpandStorage(out string message)
        {
            if (!ConsumeResources(8, 2, 1))
            {
                message = "Ressources insuffisantes.";
                return false;
            }

            State.StorageBonusKg += 12;
            GameState.Inventory.MaxWeightKg += 12f;
            message = "Capacité de transport augmentée de 12 kg.";
            GlobalSaveStore.SaveAll();
            return true;
        }

        public static bool RefineScrap(int amount, out string message)
        {
            if (!State.RefineryBuilt)
            {
                message = "Construisez d'abord la raffinerie.";
                return false;
            }

            int use = Math.Max(4, amount);
            if (!GameState.Inventory.Has("scrap", use))
            {
                message = "Ferraille insuffisante.";
                return false;
            }

            GameState.Inventory.Remove("scrap", use);

            int copper = Math.Max(1, use / 4);
            int circuits = Math.Max(1, use / 8);
            GameState.Inventory.Add("copper", copper);
            GameState.Inventory.Add("circuit", circuits);

            message = $"Raffinage : -{use} ferraille, +{copper} cuivre, +{circuits} circuit(s).";
            GlobalSaveStore.SaveAll();
            return true;
        }

        private static bool ConsumeResources(
            int scrap,
            int copper,
            int circuits)
        {
            if (!GameState.Inventory.Has("scrap", scrap) ||
                !GameState.Inventory.Has("copper", copper) ||
                !GameState.Inventory.Has("circuit", circuits))
                return false;

            GameState.Inventory.Remove("scrap", scrap);
            GameState.Inventory.Remove("copper", copper);
            GameState.Inventory.Remove("circuit", circuits);
            return true;
        }
    }
}