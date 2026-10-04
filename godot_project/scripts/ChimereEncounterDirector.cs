using System;

namespace TerreZero.Chimeres
{
    public sealed class ChimereEncounterDirector
    {
        private int _encounterIndex;

        public ChimereCombatant CreateEncounter(string h3Index, string osmContext = "")
        {
            int seed = StableSeed(h3Index, osmContext, _encounterIndex++);
            return ChimereSpeciesCatalog.CreateWildFromContext(osmContext, seed);
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
