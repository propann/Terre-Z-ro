using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace TerreZero.Network
{
    public class VoxelDeltaEvent
    {
        [JsonPropertyName("world_version")]
        public int WorldVersion { get; set; } = 1;

        [JsonPropertyName("generator_version")]
        public int GeneratorVersion { get; set; } = 4;

        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; }

        [JsonPropertyName("chunk_coords")]
        public int[] ChunkCoords { get; set; }

        [JsonPropertyName("local_voxel")]
        public int[] LocalVoxel { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("material_id")]
        public byte MaterialId { get; set; }

        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; }
    }

    public class SpatialEnvelope : VoxelDeltaEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public static class DeltaSyncManager
    {
        private static readonly List<VoxelDeltaEvent> LocalEdits = new();
        private static readonly SemaphoreSlim SocketGate = new(1, 1);
        private static readonly CancellationTokenSource Lifetime = new();

        private static ClientWebSocket _socket;
        private static Uri _serverUri;
        private static string _playerId = "local-player";
        private static string _subscribedH3;

        public static event Action<VoxelDeltaEvent> RemoteDeltaReceived;

        public static void Configure(string websocketUrl, string playerId)
        {
            if (!string.IsNullOrWhiteSpace(websocketUrl))
                _serverUri = new Uri(websocketUrl);
            if (!string.IsNullOrWhiteSpace(playerId))
                _playerId = playerId;
        }

        public static async Task RecordVoxelModificationAsync(
            string h3Index,
            Vector3I chunkCoord,
            Vector3I localVoxel,
            bool isDestroy,
            byte materialId,
            string playerId = null)
        {
            var delta = new VoxelDeltaEvent
            {
                WorldVersion = 1,
                GeneratorVersion = 4,
                H3Index = h3Index,
                ChunkCoords = new[] { chunkCoord.X, chunkCoord.Y, chunkCoord.Z },
                LocalVoxel = new[] { localVoxel.X, localVoxel.Y, localVoxel.Z },
                Action = isDestroy ? "DESTROY" : "PLACE",
                MaterialId = materialId,
                PlayerId = string.IsNullOrWhiteSpace(playerId) ? _playerId : playerId
            };

            lock (LocalEdits)
                LocalEdits.Add(delta);

            if (_serverUri == null)
                return;

            try
            {
                await EnsureConnectedAsync(h3Index);
                await SendAsync(new SpatialEnvelope
                {
                    Type = "delta",
                    WorldVersion = delta.WorldVersion,
                    GeneratorVersion = delta.GeneratorVersion,
                    H3Index = delta.H3Index,
                    ChunkCoords = delta.ChunkCoords,
                    LocalVoxel = delta.LocalVoxel,
                    Action = delta.Action,
                    MaterialId = delta.MaterialId,
                    PlayerId = delta.PlayerId
                });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DELTA SYNC] mode hors-ligne: {ex.Message}");
            }
        }

        public static async Task SubscribeAsync(string h3Index)
        {
            if (_serverUri == null || string.IsNullOrWhiteSpace(h3Index))
                return;

            try
            {
                await EnsureConnectedAsync(h3Index);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DELTA SYNC] abonnement impossible: {ex.Message}");
            }
        }

        public static int GetPendingDeltaCount()
        {
            lock (LocalEdits)
                return LocalEdits.Count;
        }

        private static async Task EnsureConnectedAsync(string h3Index)
        {
            await SocketGate.WaitAsync();
            try
            {
                if (_socket == null || _socket.State != WebSocketState.Open)
                {
                    _socket?.Dispose();
                    _socket = new ClientWebSocket();
                    await _socket.ConnectAsync(_serverUri, Lifetime.Token);
                    _ = Task.Run(ReceiveLoopAsync);
                    _subscribedH3 = null;
                    GD.Print($"[DELTA SYNC] connecté {_serverUri}");
                }

                if (_subscribedH3 != h3Index)
                {
                    await SendRawAsync(JsonSerializer.Serialize(new
                    {
                        type = "subscribe",
                        h3_index = h3Index
                    }));
                    _subscribedH3 = h3Index;
                }
            }
            finally
            {
                SocketGate.Release();
            }
        }

        private static Task SendAsync(SpatialEnvelope envelope) =>
            SendRawAsync(JsonSerializer.Serialize(envelope));

        private static async Task SendRawAsync(string json)
        {
            if (_socket == null || _socket.State != WebSocketState.Open)
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                Lifetime.Token
            );
        }

        private static async Task ReceiveLoopAsync()
        {
            byte[] buffer = new byte[16 * 1024];

            try
            {
                while (_socket != null && _socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), Lifetime.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    using var document = JsonDocument.Parse(json);
                    if (!document.RootElement.TryGetProperty("type", out var typeNode))
                        continue;

                    if (typeNode.GetString() != "delta")
                        continue;

                    var envelope = JsonSerializer.Deserialize<SpatialEnvelope>(json);
                    if (envelope != null)
                        RemoteDeltaReceived?.Invoke(envelope);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DELTA SYNC] réception interrompue: {ex.Message}");
            }
        }
    }
}
