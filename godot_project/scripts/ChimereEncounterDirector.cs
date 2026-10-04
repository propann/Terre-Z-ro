using System;

namespace TerreZero.Chimeres
{
    public sealed class ChimereEncounterDirector
    {
        private int _encounterIndex;

        public ChimereCombatant CreateEncounter(string h3Index, string osmContext = "")
        {
            int seed = StableSeed(h3Index, osmContext, _encounterIndex++);
            var wild = ChimereSpeciesCatalog.CreateWildFromContext(osmContext, seed);

            var random = new Random(seed ^ 0x5f3759df);
            double roll = random.NextDouble();

            if (roll < 0.02)
                ApplyRarity(wild, ChimereRarity.Alpha);
            else if (roll < 0.10)
                ApplyRarity(wild, ChimereRarity.Rare);

            return wild;
        }

        private static void ApplyRarity(
            ChimereCombatant chimere,
            ChimereRarity rarity)
        {
            chimere.Rarity = rarity;

            if (rarity == ChimereRarity.Rare)
            {
                chimere.MaxHp = (int)Math.Round(chimere.MaxHp * 1.12);
                chimere.CurrentHp = chimere.MaxHp;
                chimere.Attack = (int)Math.Round(chimere.Attack * 1.08);
                chimere.Defense = (int)Math.Round(chimere.Defense * 1.08);
                chimere.MaxStability += 10;
                chimere.Stability = chimere.MaxStability;
                chimere.Name = $"Rare {chimere.Name}";
            }
            else if (rarity == ChimereRarity.Alpha)
            {
                chimere.MaxHp = (int)Math.Round(chimere.MaxHp * 1.35);
                chimere.CurrentHp = chimere.MaxHp;
                chimere.Attack = (int)Math.Round(chimere.Attack * 1.18);
                chimere.Defense = (int)Math.Round(chimere.Defense * 1.16);
                chimere.Speed = Math.Max(1, chimere.Speed + 2);
                chimere.MaxStability += 25;
                chimere.Stability = chimere.MaxStability;
                chimere.Name = $"ALPHA {chimere.Name}";
            }
        }

        private static int StableSeed(string h3Index, string context, int index)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in h3Index ?? string.Empty)
                    hash = (hash ^ c) * 16777619;
                foreach (char c in context ?? string.Empty)
                    hash = (hash ^ c) * 16777619;
                hash = (hash ^ (uint)index) * 16777619;
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
