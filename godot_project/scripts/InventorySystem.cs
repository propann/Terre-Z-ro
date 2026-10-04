using System;
using System.Collections.Generic;
using System.Linq;

namespace TerreZero.Gameplay
{
    public enum ItemCategory
    {
        Resource,
        Consumable,
        CaptureDevice,
        Crafting,
        Quest,
        Equipment
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public sealed class ItemDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ItemCategory Category { get; init; }
        public ItemRarity Rarity { get; init; }
        public float UnitWeightKg { get; init; }
        public int MaxStack { get; init; } = 99;
        public float CaptureQuality { get; init; } = 1f;
        public int HealAmount { get; init; }
    }

    public sealed class InventoryStack
    {
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public sealed class InventoryState
    {
        public float MaxWeightKg { get; set; } = 28f;
        public List<InventoryStack> Stacks { get; set; } = new();

        public float CurrentWeightKg =>
            Stacks.Sum(stack =>
            {
                ItemDefinition def = ItemCatalog.Get(stack.ItemId);
                return def == null ? 0f : def.UnitWeightKg * stack.Quantity;
            });

        public int Count(string itemId) =>
            Stacks.FirstOrDefault(s => s.ItemId == itemId)?.Quantity ?? 0;

        public bool Has(string itemId, int quantity = 1) =>
            Count(itemId) >= Math.Max(1, quantity);

        public bool CanAdd(string itemId, int quantity)
        {
            ItemDefinition def = ItemCatalog.Get(itemId);
            if (def == null || quantity <= 0)
                return false;

            float futureWeight = CurrentWeightKg + def.UnitWeightKg * quantity;
            return futureWeight <= MaxWeightKg + 0.001f;
        }

        public int Add(string itemId, int quantity)
        {
            ItemDefinition def = ItemCatalog.Get(itemId);
            if (def == null || quantity <= 0)
                return 0;

            int added = 0;
            while (quantity > 0 && CanAdd(itemId, 1))
            {
                InventoryStack stack = Stacks.FirstOrDefault(
                    s => s.ItemId == itemId && s.Quantity < def.MaxStack
                );

                if (stack == null)
                {
                    stack = new InventoryStack { ItemId = itemId };
                    Stacks.Add(stack);
                }

                int room = Math.Max(0, def.MaxStack - stack.Quantity);
                if (room == 0)
                    break;

                int step = Math.Min(room, quantity);
                while (step > 0 &&
                       CurrentWeightKg + def.UnitWeightKg * step > MaxWeightKg + 0.001f)
                {
                    step--;
                }

                if (step <= 0)
                    break;

                stack.Quantity += step;
                added += step;
                quantity -= step;
            }

            return added;
        }

        public bool Remove(string itemId, int quantity = 1)
        {
            int remaining = Math.Max(1, quantity);

            for (int i = Stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                InventoryStack stack = Stacks[i];
                if (stack.ItemId != itemId)
                    continue;

                int removed = Math.Min(stack.Quantity, remaining);
                stack.Quantity -= removed;
                remaining -= removed;

                if (stack.Quantity <= 0)
                    Stacks.RemoveAt(i);
            }

            return remaining == 0;
        }

        public void Normalize()
        {
            Stacks.RemoveAll(s =>
                s == null ||
                string.IsNullOrWhiteSpace(s.ItemId) ||
                s.Quantity <= 0 ||
                ItemCatalog.Get(s.ItemId) == null
            );
        }
    }

    public static class ItemCatalog
    {
        private static readonly Dictionary<string, ItemDefinition> Items =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["scrap"] = new()
                {
                    Id = "scrap",
                    Name = "Ferraille",
                    Description = "Débris métalliques utilisables au bunker.",
                    Category = ItemCategory.Resource,
                    Rarity = ItemRarity.Common,
                    UnitWeightKg = 0.35f,
                    MaxStack = 50
                },
                ["copper"] = new()
                {
                    Id = "copper",
                    Name = "Cuivre",
                    Description = "Conducteur utile aux modules et circuits.",
                    Category = ItemCategory.Resource,
                    Rarity = ItemRarity.Common,
                    UnitWeightKg = 0.20f,
                    MaxStack = 50
                },
                ["circuit"] = new()
                {
                    Id = "circuit",
                    Name = "Circuit récupéré",
                    Description = "Électronique récupérable et raffinable.",
                    Category = ItemCategory.Crafting,
                    Rarity = ItemRarity.Uncommon,
                    UnitWeightKg = 0.08f,
                    MaxStack = 30
                },
                ["med_gel"] = new()
                {
                    Id = "med_gel",
                    Name = "Gel médical",
                    Description = "Soin de terrain rapide.",
                    Category = ItemCategory.Consumable,
                    Rarity = ItemRarity.Uncommon,
                    UnitWeightKg = 0.15f,
                    MaxStack = 12,
                    HealAmount = 35
                },
                ["ration"] = new()
                {
                    Id = "ration",
                    Name = "Ration compacte",
                    Description = "Restaure une partie de l'endurance.",
                    Category = ItemCategory.Consumable,
                    Rarity = ItemRarity.Common,
                    UnitWeightKg = 0.30f,
                    MaxStack = 12
                },
                ["battery"] = new()
                {
                    Id = "battery",
                    Name = "Cellule d'énergie",
                    Description = "Source d'énergie portable.",
                    Category = ItemCategory.Crafting,
                    Rarity = ItemRarity.Uncommon,
                    UnitWeightKg = 0.22f,
                    MaxStack = 16
                },
                ["capture_basic"] = new()
                {
                    Id = "capture_basic",
                    Name = "Module de capture",
                    Description = "Module standard de liaison Chimère.",
                    Category = ItemCategory.CaptureDevice,
                    Rarity = ItemRarity.Common,
                    UnitWeightKg = 0.12f,
                    MaxStack = 20,
                    CaptureQuality = 1.0f
                },
                ["capture_stable"] = new()
                {
                    Id = "capture_stable",
                    Name = "Module stabilisé",
                    Description = "Module de capture plus fiable.",
                    Category = ItemCategory.CaptureDevice,
                    Rarity = ItemRarity.Rare,
                    UnitWeightKg = 0.14f,
                    MaxStack = 12,
                    CaptureQuality = 1.18f
                },
                ["capture_alpha"] = new()
                {
                    Id = "capture_alpha",
                    Name = "Module Alpha",
                    Description = "Module rare conçu pour les Chimères extrêmes.",
                    Category = ItemCategory.CaptureDevice,
                    Rarity = ItemRarity.Epic,
                    UnitWeightKg = 0.18f,
                    MaxStack = 6,
                    CaptureQuality = 1.35f
                }
            };

        public static ItemDefinition Get(string id) =>
            !string.IsNullOrWhiteSpace(id) && Items.TryGetValue(id, out var item)
                ? item
                : null;

        public static IEnumerable<ItemDefinition> All => Items.Values;
    }
}
