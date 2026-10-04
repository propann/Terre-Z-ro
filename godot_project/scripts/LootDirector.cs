using System;
using System.Collections.Generic;

namespace TerreZero.Gameplay
{
    public sealed class LootEntry
    {
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public sealed class LootBundle
    {
        public string SourceId { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<LootEntry> Entries { get; set; } = new();
    }

    public static class LootDirector
    {
        public static LootBundle Generate(
            string sourceId,
            string context,
            int seed)
        {
            var random = new Random(StableSeed(sourceId, context, seed));
            string normalized = (context ?? string.Empty).ToLowerInvariant();

            var bundle = new LootBundle
            {
                SourceId = sourceId ?? string.Empty,
                Context = context ?? string.Empty
            };

            if (normalized.Contains("pharmacy") || normalized.Contains("hospital"))
            {
                Add(bundle, "med_gel", random.Next(1, 4));
                MaybeAdd(bundle, random, "capture_stable", 0.20, 1);
                MaybeAdd(bundle, random, "battery", 0.45, random.Next(1, 3));
            }
            else if (normalized.Contains("industrial") || normalized.Contains("railway"))
            {
                Add(bundle, "scrap", random.Next(2, 7));
                Add(bundle, "copper", random.Next(1, 5));
                MaybeAdd(bundle, random, "circuit", 0.65, random.Next(1, 4));
                MaybeAdd(bundle, random, "capture_stable", 0.10, 1);
            }
            else if (normalized.Contains("park") || normalized.Contains("wood"))
            {
                Add(bundle, "ration", random.Next(1, 3));
                MaybeAdd(bundle, random, "med_gel", 0.35, 1);
                MaybeAdd(bundle, random, "capture_basic", 0.40, random.Next(1, 3));
            }
            else
            {
                Add(bundle, "scrap", random.Next(1, 4));
                MaybeAdd(bundle, random, "circuit", 0.35, 1);
                MaybeAdd(bundle, random, "ration", 0.45, 1);
                MaybeAdd(bundle, random, "capture_basic", 0.28, 1);
            }

            if (random.NextDouble() < 0.025)
                Add(bundle, "capture_alpha", 1);

            return bundle;
        }

        public static int ApplyToInventory(
            LootBundle bundle,
            InventoryState inventory)
        {
            int totalAdded = 0;

            foreach (var entry in bundle.Entries)
                totalAdded += inventory.Add(entry.ItemId, entry.Quantity);

            return totalAdded;
        }

        private static void Add(
            LootBundle bundle,
            string itemId,
            int quantity)
        {
            if (quantity <= 0)
                return;

            bundle.Entries.Add(new LootEntry
            {
                ItemId = itemId,
                Quantity = quantity
            });
        }

        private static void MaybeAdd(
            LootBundle bundle,
            Random random,
            string itemId,
            double chance,
            int quantity)
        {
            if (random.NextDouble() <= chance)
                Add(bundle, itemId, quantity);
        }

        private static int StableSeed(
            string sourceId,
            string context,
            int seed)
        {
            unchecked
            {
                uint hash = 2166136261u;

                foreach (char c in sourceId ?? string.Empty)
                    hash = (hash ^ c) * 16777619u;

                foreach (char c in context ?? string.Empty)
                    hash = (hash ^ c) * 16777619u;

                hash = (hash ^ (uint)seed) * 16777619u;
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
