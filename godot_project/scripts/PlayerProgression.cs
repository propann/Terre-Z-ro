// PlayerProgression.cs - Système de Progression & Implants Cybernétiques (Godot 4 C#)
// Spécification Terre Zéro : Condition physique IRL cumulée, 3 arbres de maîtrise,
// 4 implants greffés à l'abri et gestion de la caisse de mort (Death Crate).

using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.Progression
{
    public enum SkillBranch
    {
        Engineer,    // Voxel, Craft, Défense de base
        BioTracker,  // Chimères, Scan, Traque, Troc
        HeavyCombat  // Armes, Forage lourd, Survie hostile
    }

    public class CyberneticImplants
    {
        public int OcularScannerLevel { get; set; } = 1; // 1: Thermique, 2: Failles, 3: Spectromètre
        public bool SpinalExoskeleton { get; set; } = false; // +Charge utile, pas de malus forage lourd
        public bool CerebralInterface { get; set; } = false; // -50% temps d'injection modules
        public int DermalArmorLevel { get; set; } = 0;   // Grille sous-cutanée (réduit acide/entailles)
    }

    public class DeathCrate
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public ulong DroppedTimestampMs { get; set; }
        public bool IsRecovered { get; set; } = false;

        public bool IsExpired(ulong currentTimestampMs)
        {
            const ulong twentyFourHoursMs = 24 * 60 * 60 * 1000;
            return (currentTimestampMs - DroppedTimestampMs) > twentyFourHoursMs;
        }
    }

    public class PlayerProgression : Node
    {
        [Export] public double TotalWalkedKilometers = 12.4;
        [Export] public int MasteryPoints = 4;

        public CyberneticImplants Implants { get; private set; } = new CyberneticImplants();
        public HashSet<string> UnlockedPerks { get; private set; } = new HashSet<string>();
        public DeathCrate CurrentDeathCrate { get; private set; }

        // Traits physiques débloqués selon les paliers réels
        public bool HasEconomicStride => TotalWalkedKilometers >= 10.0;    // -15% fatigue sprint
        public bool HasSteelBack => TotalWalkedKilometers >= 50.0;          // +15kg capacité sac
        public bool HasTrackerSense => TotalWalkedKilometers >= 100.0;      // +10m rayon détection passive
        public bool HasHardenedMetabolism => TotalWalkedKilometers >= 250.0;// +20% résistance radiation
        public bool HasVeteranDrifter => TotalWalkedKilometers >= 500.0;    // Vitesse accrue & -50% rations

        // Ajouter de la distance via le podomètre validé
        public void AddKilometers(double km)
        {
            TotalWalkedKilometers += km;
            GD.Print($"[PROGRESSION] Distance cumulée : {TotalWalkedKilometers:F2} km");
        }

        // Débloquer une compétence d'arbre
        public bool UnlockPerk(string perkId, int cost)
        {
            if (MasteryPoints < cost || UnlockedPerks.Contains(perkId)) return false;
            MasteryPoints -= cost;
            UnlockedPerks.Add(perkId);
            GD.Print($"[PROGRESSION] Compétence débloquée : {perkId}");
            return true;
        }

        // Greffer un implant au bunker
        public void UpgradeOcularScanner()
        {
            if (Implants.OcularScannerLevel < 3) Implants.OcularScannerLevel++;
        }

        public void InstallSpinalExoskeleton()
        {
            Implants.SpinalExoskeleton = true;
        }

        // Mort du survivant : Dépose une caisse de largage aux coordonnées GPS exactes
        public void OnPlayerDeath(double currentLat, double currentLon)
        {
            CurrentDeathCrate = new DeathCrate
            {
                Lat = currentLat,
                Lon = currentLon,
                DroppedTimestampMs = Time.GetTicksMsec(),
                IsRecovered = false
            };
            GD.Print($"[SURVIE] Mort au combat. Caisse de mort déposée à ({currentLat:F5}, {currentLon:F5}). Fenêtre de 24h active !");
        }

        // Récupération de la caisse lors d'une marche réelle à moins de 25m
        public bool TryRecoverDeathCrate(double playerLat, double playerLon)
        {
            if (CurrentDeathCrate == null || CurrentDeathCrate.IsRecovered) return false;
            if (CurrentDeathCrate.IsExpired(Time.GetTicksMsec()))
            {
                GD.Print("[SURVIE] Caisse de mort dissoute ou pillée après 24h !");
                CurrentDeathCrate = null;
                return false;
            }

            // Calcul distance
            double dLat = (CurrentDeathCrate.Lat - playerLat) * 111000.0;
            double dLon = (CurrentDeathCrate.Lon - playerLon) * 111000.0;
            double distMeters = Math.Sqrt(dLat * dLat + dLon * dLon);

            if (distMeters <= 25.0)
            {
                CurrentDeathCrate.IsRecovered = true;
                GD.Print("[SURVIE] Caisse de mort récupérée avec succès sur site !");
                return true;
            }
            return false;
        }
    }
}
