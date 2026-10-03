using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.World.Voxel
{
    public sealed class VoxelWorldGrid
    {
        private readonly Node3D _container;
        private readonly PackedScene _chunkScene;
        private readonly string _h3Index;
        private readonly Dictionary<Vector3I, VoxelChunk> _chunks = new();

        public VoxelWorldGrid(Node3D container, string h3Index)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _h3Index = h3Index;
            _chunkScene = GD.Load<PackedScene>("res://scenes/VoxelChunk.tscn");
        }

        public IEnumerable<VoxelChunk> Chunks => _chunks.Values;
        public int LoadedChunkCount => _chunks.Count;

        public VoxelChunk GetOrCreateChunk(Vector3I chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out var existing))
                return existing;

            var chunk = _chunkScene.Instantiate<VoxelChunk>();
            chunk.Name = $"Chunk_{chunkCoord.X}_{chunkCoord.Y}_{chunkCoord.Z}";
            chunk.H3Index = _h3Index;
            chunk.ChunkCoord = chunkCoord;
            chunk.Position = new Vector3(
                chunkCoord.X * VoxelChunk.Size * VoxelChunk.VoxelScale,
                chunkCoord.Y * VoxelChunk.Size * VoxelChunk.VoxelScale,
                chunkCoord.Z * VoxelChunk.Size * VoxelChunk.VoxelScale
            );

            _container.AddChild(chunk);
            _chunks[chunkCoord] = chunk;
            return chunk;
        }

        public bool TryGetChunk(Vector3I chunkCoord, out VoxelChunk chunk) =>
            _chunks.TryGetValue(chunkCoord, out chunk);

        public void SetVoxelGlobal(
            int globalX,
            int globalY,
            int globalZ,
            VoxelMaterial material,
            byte durability = 15,
            bool trackEdit = false)
        {
            if (globalY < 0)
                return;

            var chunkCoord = new Vector3I(
                FloorDiv(globalX, VoxelChunk.Size),
                FloorDiv(globalY, VoxelChunk.Size),
                FloorDiv(globalZ, VoxelChunk.Size)
            );

            var local = new Vector3I(
                PositiveMod(globalX, VoxelChunk.Size),
                PositiveMod(globalY, VoxelChunk.Size),
                PositiveMod(globalZ, VoxelChunk.Size)
            );

            GetOrCreateChunk(chunkCoord)
                .SetVoxel(local.X, local.Y, local.Z, material, durability, trackEdit);
        }

        public VoxelMaterial GetVoxelGlobal(int globalX, int globalY, int globalZ)
        {
            if (globalY < 0)
                return VoxelMaterial.Air;

            var chunkCoord = new Vector3I(
                FloorDiv(globalX, VoxelChunk.Size),
                FloorDiv(globalY, VoxelChunk.Size),
                FloorDiv(globalZ, VoxelChunk.Size)
            );

            if (!_chunks.TryGetValue(chunkCoord, out var chunk))
                return VoxelMaterial.Air;

            return chunk.GetVoxel(
                PositiveMod(globalX, VoxelChunk.Size),
                PositiveMod(globalY, VoxelChunk.Size),
                PositiveMod(globalZ, VoxelChunk.Size)
            );
        }

        public void ApplyDelta(
            Vector3I chunkCoord,
            Vector3I localVoxel,
            VoxelMaterial material)
        {
            var chunk = GetOrCreateChunk(chunkCoord);
            chunk.SetVoxel(
                localVoxel.X,
                localVoxel.Y,
                localVoxel.Z,
                material,
                trackEdit: false
            );
            chunk.RebuildMeshGreedy();
        }

        public void RebuildDirtyChunks()
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk.IsDirty)
                    chunk.RebuildMeshGreedy();
            }
        }

        public static Vector3I WorldPositionToChunk(Vector3 worldPosition)
        {
            float chunkMeters = VoxelChunk.Size * VoxelChunk.VoxelScale;
            return new Vector3I(
                Mathf.FloorToInt(worldPosition.X / chunkMeters),
                Mathf.FloorToInt(worldPosition.Y / chunkMeters),
                Mathf.FloorToInt(worldPosition.Z / chunkMeters)
            );
        }

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                quotient--;
            return quotient;
        }

        private static int PositiveMod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
