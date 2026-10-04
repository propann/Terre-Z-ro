using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace TerreZero.Gameplay
{
    public sealed class ExplorationStateData
    {
        public HashSet<string> OpenedLootSources { get; set; } = new();
        public HashSet<string> DiscoveredCells { get; set; } = new();
    }

    public static class ExplorationState
    {
        private const string SaveFile = "user://exploration.json";

        public static HashSet<string> OpenedLootSources { get; } = new();
        public static HashSet<string> DiscoveredCells { get; } = new();

        public static bool IsLootOpened(string sourceId) =>
            !string.IsNullOrWhiteSpace(sourceId) &&
            OpenedLootSources.Contains(sourceId);

        public static void MarkLootOpened(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                return;

            if (OpenedLootSources.Add(sourceId))
                Save();
        }

        public static void DiscoverCell(string h3Index)
        {
            if (string.IsNullOrWhiteSpace(h3Index))
                return;

            if (DiscoveredCells.Add(h3Index))
                Save();
        }

        public static void Save()
        {
            var data = new ExplorationStateData
            {
                OpenedLootSources = new HashSet<string>(OpenedLootSources),
                DiscoveredCells = new HashSet<string>(DiscoveredCells)
            };

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(
                ProjectSettings.GlobalizePath(SaveFile),
                json
            );
        }

        public static bool Load()
        {
            string path = ProjectSettings.GlobalizePath(SaveFile);
            if (!File.Exists(path))
                return false;

            try
            {
                var data = JsonSerializer.Deserialize<ExplorationStateData>(
                    File.ReadAllText(path)
                );

                if (data == null)
                    return false;

                OpenedLootSources.Clear();
                DiscoveredCells.Clear();

                foreach (string id in data.OpenedLootSources ?? new HashSet<string>())
                    OpenedLootSources.Add(id);

                foreach (string cell in data.DiscoveredCells ?? new HashSet<string>())
                    DiscoveredCells.Add(cell);

                return true;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[EXPLORATION] sauvegarde illisible : {ex.Message}");
                return false;
            }
        }
    }
}
