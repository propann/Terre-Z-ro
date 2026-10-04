using TerreZero.Chimeres;

namespace TerreZero.Gameplay
{
    public readonly struct CaptureDeviceSelection
    {
        public string ItemId { get; }
        public string Name { get; }
        public float Quality { get; }

        public CaptureDeviceSelection(
            string itemId,
            string name,
            float quality)
        {
            ItemId = itemId;
            Name = name;
            Quality = quality;
        }
    }

    public static class CaptureDeviceService
    {
        public static bool TrySelectBest(
            ChimereCombatant wild,
            out CaptureDeviceSelection selection)
        {
            string[] order = wild?.Rarity == ChimereRarity.Alpha
                ? new[] { "capture_alpha", "capture_stable", "capture_basic" }
                : new[] { "capture_stable", "capture_basic", "capture_alpha" };

            foreach (string itemId in order)
            {
                if (!GameState.Inventory.Has(itemId))
                    continue;

                ItemDefinition item = ItemCatalog.Get(itemId);
                if (item == null)
                    continue;

                selection = new CaptureDeviceSelection(
                    item.Id,
                    item.Name,
                    item.CaptureQuality
                );
                return true;
            }

            selection = default;
            return false;
        }

        public static bool Consume(CaptureDeviceSelection selection) =>
            !string.IsNullOrWhiteSpace(selection.ItemId) &&
            GameState.Inventory.Remove(selection.ItemId, 1);

        public static int TotalAvailable =>
            GameState.Inventory.Count("capture_basic") +
            GameState.Inventory.Count("capture_stable") +
            GameState.Inventory.Count("capture_alpha");
    }
}
