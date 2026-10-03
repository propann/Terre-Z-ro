// DeltaSyncManager.cs - Synchronisation Réseau & Format Delta (Godot 4 C#)
// Spécification Terre Zéro : Format d'événement compressé pour monde destructible mondial
// Bâtiment de base = état OSM 0. Seules les modifications (trous/blocs) sont diffusées.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace TerreZero.Network
{
    public class VoxelDeltaEvent
    {
        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; }

        [JsonPropertyName("chunk_coords")]
        public int[] ChunkCoords { get; set; } // [X, Y, Z]

        [JsonPropertyName("local_voxel")]
        public int[] LocalVoxel { get; set; } // [X, Y, Z]

        [JsonPropertyName("action")]
        public string Action { get; set; } // "DESTROY" ou "PLACE"

        [JsonPropertyName("material_id")]
        public byte MaterialId { get; set; }

        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; }
    }

    public static class DeltaSyncManager
    {
        private static readonly List<VoxelDeltaEvent> _localEdits = new List<VoxelDeltaEvent>();

        public static void RecordVoxelModification(string h3Index, Vector3I chunkCoord, Vector3I localVoxel, bool isDestroy, byte matId, string playerId)
        {
            var delta = new VoxelDeltaEvent
            {
                H3Index = h3Index,
                ChunkCoords = new int[] { chunkCoord.X, chunkCoord.Y, chunkCoord.Z },
                LocalVoxel = new int[] { localVoxel.X, localVoxel.Y, localVoxel.Z },
                Action = isDestroy ? "DESTROY" : "PLACE",
                MaterialId = matId,
                PlayerId = playerId
            };

            _localEdits.Add(delta);
            string json = JsonSerializer.Serialize(delta);
            GD.Print($"[DELTA SYNC] Événement réseau émis ({json.Length} octets) : {json}");
        }

        public static int GetPendingDeltaCount() => _localEdits.Count;
    }
}
