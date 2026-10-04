namespace TerreZero.Gameplay
{
    public static class GameState
    {
        public const int SaveVersion = 1;

        public static PlayerProfileState Player { get; } = new();
        public static InventoryState Inventory { get; } = new();
        public static QuestJournalState Quests { get; } = new();

        public static void SeedNewGameInventory()
        {
            if (Inventory.Stacks.Count > 0)
                return;

            Inventory.Add("capture_basic", 8);
            Inventory.Add("med_gel", 2);
            Inventory.Add("ration", 3);
        }
    }
}
