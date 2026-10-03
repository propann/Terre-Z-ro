// BunkerManager.cs - Mode Sédentaire & Ancrage Résidence Réelle (Godot 4 C#)
// Spécification Terre Zéro : Ancrage GPS domicile, construction de blocs utilitaires,
// raffinerie de minerais/débris et couveuse de chimères.

using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.Bunker
{
    public class BunkerData
    {
        public bool IsAnchored { get; set; } = false;
        public double AnchorLat { get; set; }
        public double AnchorLon { get; set; }
        public int EnergyLevelKW { get; set; } = 40;
        public int DefensePoints { get; set; } = 120;
        public int StorageCapacityKg { get; set; } = 250;

        // Raffinerie : stocks de matières premières et matériaux raffinés
        public int RawScrap { get; set; } = 45;
        public int RawCopperOre { get; set; } = 28;
        public int RefinedTitaniumPlates { get; set; } = 8;
        public int ElectronicCircuits { get; set; } = 12;

        public bool AnchorHome(double lat, double lon)
        {
            AnchorLat = lat;
            AnchorLon = lon;
            IsAnchored = true;
            GD.Print($"[BUNKER] Abri ancré aux coordonnées réelles : {lat:F5}, {lon:F5}");
            return true;
        }

        // Raffinerie : transformation des débris en plaques et circuits
        public bool RefineRawMaterials(int scrapAmount, out string resultMessage)
        {
            if (RawScrap < scrapAmount)
            {
                resultMessage = "Ferraille insuffisante pour la raffinerie.";
                return false;
            }

            RawScrap -= scrapAmount;
            int platesProduced = scrapAmount / 4;
            int circuitsProduced = scrapAmount / 8;

            RefinedTitaniumPlates += platesProduced;
            ElectronicCircuits += circuitsProduced;

            resultMessage = $"Raffinage terminé : +{platesProduced} Plaques Titane, +{circuitsProduced} Circuits Imprimés.";
            return true;
        }
    }
}
