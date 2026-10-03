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
        Engineer,
        BioTracker,
        HeavyCombat
    }

    public class CyberneticImplants
    {
        public int OcularScannerLevel { get; set; } = 1;
        public bool SpinalExoskeleton { get; set; } = false;
        public bool CerebralInterface { get; set; } = false;
        public int DermalArmorLevel { get; set; } = 0;
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

    public partial class PlayerProgression : Node
    {
        [Export] public double TotalWalkedKilometers = 12.4;
        [Export] public int MasteryPoints = 4;

        public CyberneticImplants Implants { get; private set; } = new();
        public HashSet<string> UnlockedPerks { get; private set; } = new();
        public DeathCrate CurrentDeathCrate { get; private set; }

        public bool HasEconomicStride => TotalWalkedKilometers >= 10.0;
        public bool HasSteelBack => TotalWalkedKilometers >= 50.0;
        public bool HasTrackerSense => TotalWalkedKilometers >= 100.0;
        public bool HasHardenedMetabolism => TotalWalkedKilometers >= 250.0;
        public bool HasVeteranDrifter => TotalWalkedKilometers >= 500.0;

        public void AddKilometers(double km)
        {
            TotalWalkedKilometers += km;
            GD.Print($"[PROGRESSION] Distance cumulée : {TotalWalkedKilometers:F2} km");
        }

        public bool UnlockPerk(string perkId, int cost)
        {
            if (MasteryPoints < cost || UnlockedPerks.Contains(perkId))
                return false;

            MasteryPoints -= cost;
            UnlockedPerks.Add(perkId);
            GD.Print($"[PROGRESSION] Compétence débloquée : {perkId}");
            return true;
        }

        public void UpgradeOcularScanner()
        {
            if (Implants.OcularScannerLevel < 3)
                Implants.OcularScannerLevel++;
        }

        public void InstallSpinalExoskeleton()
        {
            Implants.SpinalExoskeleton = true;
        }

        public void OnPlayerDeath(double currentLat, double currentLon)
        {
            CurrentDeathCrate = new DeathCrate
            {
                Lat = currentLat,
                Lon = currentLon,
                DroppedTimestampMs = Time.GetTicksMsec(),
                IsRecovered = false
            };
            GD.Print($"[SURVIE] Caisse de mort déposée à ({currentLat:F5}, {currentLon:F5}).");
        }

        public bool TryRecoverDeathCrate(double playerLat, double playerLon)
        {
            if (CurrentDeathCrate == null || CurrentDeathCrate.IsRecovered)
                return false;

            if (CurrentDeathCrate.IsExpired(Time.GetTicksMsec()))
            {
                CurrentDeathCrate = null;
                return false;
            }

            double dLat = (CurrentDeathCrate.Lat - playerLat) * 111000.0;
            double dLon = (CurrentDeathCrate.Lon - playerLon) * 111000.0;
            double distMeters = Math.Sqrt(dLat * dLat + dLon * dLon);

            if (distMeters > 25.0)
                return false;

            CurrentDeathCrate.IsRecovered = true;
            return true;
        }
    }
}
