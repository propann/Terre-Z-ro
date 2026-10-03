// ChimereManager.cs - Système des Chimères (Godot 4 C#)
// Spécification Terre Zéro : Écologie OSM, anatomie voxel micro-ciblée,
// capture par module d'asservissement (Puce d'override / Collier neurotoxique) et utilité au bunker.

using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.Chimeres
{
    public enum ChimerePhylum
    {
        TechnoideLourd,      // industrial=*, railway=*, construction=*
        TechnoideReseau,     // telecom=*, shop=electronics, office=*
        BiomutantTerrestre,  // landuse=residential, amenity=waste_transfer
        BiomutantSylvestre   // natural=wood, leisure=park, waterway=*
    }

    public enum BunkerUtilityRole
    {
        None,
        LivingGenerator,    // Générateur vivant (+15 à +30 kW pour le raffinage)
        TerritorialSentry,  // Sentinelle territoriale (Garde le périmètre GPS de l'abri)
        PackMule,           // Bête de somme d'expédition (+10 à +20 kg de sac à dos)
        DrillAssistant      // Foreuse assistée (Vitesse de minage des voxels x3)
    }

    public struct VoxelAnatomyPart
    {
        public string Name;
        public int CurrentHp;
        public int MaxHp;
        public bool IsBroken => CurrentHp <= 0;
        public string EffectDesc;
    }

    public class ChimereInstance
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ChimerePhylum Phylum { get; set; }
        public int Level { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public BunkerUtilityRole AssignedRole { get; set; } = BunkerUtilityRole.None;

        // Anatomie Voxel Ciblée
        public VoxelAnatomyPart ArmorPlating { get; set; }
        public VoxelAnatomyPart LegsActuators { get; set; }
        public VoxelAnatomyPart EnergyTank { get; set; }
        public VoxelAnatomyPart CoreVital { get; set; }

        public bool IsOverkilled { get; set; } = false;

        public bool IsMechanical => Phylum == ChimerePhylum.TechnoideLourd || Phylum == ChimerePhylum.TechnoideReseau;

        // Calcul du Taux de Capture GDD :
        // [Qualité du module] * [Intégrité restante du noyau] / [Niveau de la Chimère]
        public float CalculateCaptureChance(float moduleQualityMultiplier = 1.0f)
        {
            if (IsOverkilled) return 0.0f; // Incapturable si pulvérisée

            float hpFactor = (1.0f - ((float)CurrentHp / MaxHp)) * 0.5f;
            float coreIntegrity = CoreVital.MaxHp > 0 ? (float)CoreVital.CurrentHp / CoreVital.MaxHp : 0.5f;

            float baseRate = (0.35f + hpFactor) * (0.6f + coreIntegrity * 0.4f);
            float levelPenalty = Mathf.Max(0.7f, 1.0f - (Level * 0.05f));

            return Mathf.Clamp(baseRate * moduleQualityMultiplier * levelPenalty, 0.05f, 0.95f);
        }

        // Dégâts ciblés sur un composant voxel
        public void ApplyTargetedDamage(string targetPart, int damage)
        {
            CurrentHp = Math.Max(0, CurrentHp - damage);

            switch (targetPart.ToLower())
            {
                case "armor":
                    var armor = ArmorPlating;
                    armor.CurrentHp = Math.Max(0, armor.CurrentHp - damage);
                    ArmorPlating = armor;
                    break;
                case "legs":
                    var legs = LegsActuators;
                    legs.CurrentHp = Math.Max(0, legs.CurrentHp - damage);
                    LegsActuators = legs;
                    Attack = (int)(Attack * 0.7f); // Affaiblissement
                    break;
                case "tank":
                    var tank = EnergyTank;
                    tank.CurrentHp = Math.Max(0, tank.CurrentHp - damage);
                    EnergyTank = tank;
                    break;
                case "core":
                    var core = CoreVital;
                    core.CurrentHp = Math.Max(0, core.CurrentHp - damage);
                    CoreVital = core;
                    if (core.IsBroken && CurrentHp <= 0)
                    {
                        IsOverkilled = true; // Danger de l'acharnement !
                        GD.Print("[CHIMÈRE] Overkill critique : noyau vital anéanti, capture impossible.");
                    }
                    break;
            }
        }
    }

    public static class ChimereSpawner
    {
        private static readonly Random _rng = new Random();

        public static ChimereInstance GenerateFromOsm(string osmTag, double lat, double lon)
        {
            if (osmTag.Contains("industrial") || osmTag.Contains("railway"))
            {
                return new ChimereInstance
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = "Bélier de Chantier Mk-II",
                    Phylum = ChimerePhylum.TechnoideLourd,
                    Level = _rng.Next(2, 6),
                    MaxHp = 120,
                    CurrentHp = 120,
                    Attack = 28,
                    Defense = 18,
                    ArmorPlating = new VoxelAnatomyPart { Name = "Blindage Frontal", CurrentHp = 40, MaxHp = 40, EffectDesc = "Protection de noyau" },
                    LegsActuators = new VoxelAnatomyPart { Name = "Vérins Hydrauliques", CurrentHp = 30, MaxHp = 30, EffectDesc = "Mobilité" },
                    EnergyTank = new VoxelAnatomyPart { Name = "Alternateur Foudre", CurrentHp = 25, MaxHp = 25, EffectDesc = "Alimentation" },
                    CoreVital = new VoxelAnatomyPart { Name = "Unité Centrale IA", CurrentHp = 50, MaxHp = 50, EffectDesc = "Noyau vital" }
                };
            }
            else if (osmTag.Contains("natural") || osmTag.Contains("park") || osmTag.Contains("waterway"))
            {
                return new ChimereInstance
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = "Cerf d'Écorce Muté",
                    Phylum = ChimerePhylum.BiomutantSylvestre,
                    Level = _rng.Next(1, 5),
                    MaxHp = 95,
                    CurrentHp = 95,
                    Attack = 22,
                    Defense = 10,
                    ArmorPlating = new VoxelAnatomyPart { Name = "Écorce Blindée", CurrentHp = 25, MaxHp = 25, EffectDesc = "Carapace" },
                    LegsActuators = new VoxelAnatomyPart { Name = "Tendons Renforcés", CurrentHp = 20, MaxHp = 20, EffectDesc = "Vitesse" },
                    EnergyTank = new VoxelAnatomyPart { Name = "Glande de Spores", CurrentHp = 20, MaxHp = 20, EffectDesc = "Bile corrosive" },
                    CoreVital = new VoxelAnatomyPart { Name = "Cœur Mutagène", CurrentHp = 40, MaxHp = 40, EffectDesc = "Organe vital" }
                };
            }
            else
            {
                return new ChimereInstance
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = "Molosse de Mâchefer",
                    Phylum = ChimerePhylum.BiomutantTerrestre,
                    Level = _rng.Next(1, 4),
                    MaxHp = 80,
                    CurrentHp = 80,
                    Attack = 24,
                    Defense = 12,
                    ArmorPlating = new VoxelAnatomyPart { Name = "Plaques Osseuses", CurrentHp = 20, MaxHp = 20, EffectDesc = "Blindage" },
                    LegsActuators = new VoxelAnatomyPart { Name = "Pattes Griffues", CurrentHp = 20, MaxHp = 20, EffectDesc = "Course" },
                    EnergyTank = new VoxelAnatomyPart { Name = "Poche de Venin", CurrentHp = 15, MaxHp = 15, EffectDesc = "Toxine" },
                    CoreVital = new VoxelAnatomyPart { Name = "Cervelet Sauvage", CurrentHp = 35, MaxHp = 35, EffectDesc = "Noyau vital" }
                };
            }
        }
    }
}
