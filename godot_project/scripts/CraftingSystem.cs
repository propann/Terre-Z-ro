using System;
using System.Collections.Generic;

namespace TerreZero.Gameplay
{
    public sealed class CraftIngredient
    {
        public string ItemId { get; init; } = string.Empty;
        public int Quantity { get; init; }
    }

    public sealed class CraftRecipe
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<CraftIngredient> Ingredients { get; init; } = new();
        public string OutputItemId { get; init; } = string.Empty;
        public int OutputQuantity { get; init; } = 1;
        public bool BunkerOnly { get; init; }
    }

    public static class CraftingCatalog
    {
        private static readonly Dictionary<string, CraftRecipe> Recipes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["capture_basic"] = new()
                {
                    Id = "capture_basic",
                    Name = "Module de capture",
                    Description = "Assemblage terrain standard.",
                    Ingredients = new()
                    {
                        new() { ItemId = "copper", Quantity = 2 },
                        new() { ItemId = "circuit", Quantity = 1 }
                    },
                    OutputItemId = "capture_basic",
                    OutputQuantity = 2
                },
                ["capture_stable"] = new()
                {
                    Id = "capture_stable",
                    Name = "Module stabilisé",
                    Description = "Capture renforcée pour Chimères rares.",
                    Ingredients = new()
                    {
                        new() { ItemId = "copper", Quantity = 3 },
                        new() { ItemId = "circuit", Quantity = 2 },
                        new() { ItemId = "battery", Quantity = 1 }
                    },
                    OutputItemId = "capture_stable",
                    OutputQuantity = 1,
                    BunkerOnly = true
                },
                ["med_gel"] = new()
                {
                    Id = "med_gel",
                    Name = "Gel médical",
                    Description = "Reconditionne un soin de terrain.",
                    Ingredients = new()
                    {
                        new() { ItemId = "ration", Quantity = 1 },
                        new() { ItemId = "battery", Quantity = 1 }
                    },
                    OutputItemId = "med_gel",
                    OutputQuantity = 1
                }
            };

        public static IEnumerable<CraftRecipe> All => Recipes.Values;

        public static CraftRecipe Get(string id) =>
            !string.IsNullOrWhiteSpace(id) &&
            Recipes.TryGetValue(id, out var recipe)
                ? recipe
                : null;
    }

    public static class CraftingService
    {
        public static bool CanCraft(
            CraftRecipe recipe,
            bool atBunker,
            out string reason)
        {
            if (recipe == null)
            {
                reason = "Recette inconnue.";
                return false;
            }

            if (recipe.BunkerOnly && !atBunker)
            {
                reason = "Cette recette nécessite le bunker.";
                return false;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                if (!GameState.Inventory.Has(
                    ingredient.ItemId,
                    ingredient.Quantity))
                {
                    ItemDefinition item = ItemCatalog.Get(ingredient.ItemId);
                    reason = $"Ressource manquante : {item?.Name ?? ingredient.ItemId}.";
                    return false;
                }
            }

            if (!GameState.Inventory.CanAdd(
                recipe.OutputItemId,
                recipe.OutputQuantity))
            {
                reason = "Inventaire trop lourd.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static bool TryCraft(
            string recipeId,
            bool atBunker,
            out string message)
        {
            CraftRecipe recipe = CraftingCatalog.Get(recipeId);

            if (!CanCraft(recipe, atBunker, out message))
                return false;

            foreach (var ingredient in recipe.Ingredients)
                GameState.Inventory.Remove(
                    ingredient.ItemId,
                    ingredient.Quantity
                );

            int added = GameState.Inventory.Add(
                recipe.OutputItemId,
                recipe.OutputQuantity
            );

            if (added != recipe.OutputQuantity)
            {
                message = "Échec d'ajout de l'objet fabriqué.";
                return false;
            }

            ItemDefinition output = ItemCatalog.Get(recipe.OutputItemId);
            message = $"{recipe.Name} fabriqué : {output?.Name ?? recipe.OutputItemId} x{added}.";
            GlobalSaveStore.SaveAll();
            return true;
        }
    }
}