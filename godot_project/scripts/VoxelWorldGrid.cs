using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.World.Voxel
{
    public readonly struct WorldVoxelEdit
    {
        public Vector3I ChunkCoord { get; }
        public VoxelEdit Edit { get; }

        public WorldVoxelEdit(Vector3I chunkCoord, VoxelEdit edit)
        {
            ChunkCoord = chunkCoord;
            Edit = edit;
        }
    }

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
        public string H3Index => _h3Index;

        public VoxelChunk GetOrCreateChunk(Vector3I chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out var existing))
                return existing;

            var chunk = _chunkScene.Instantiate<VoxelChunk>();
            chunk.Name = $"Chunk_{chunkCoord.X}_{chunkCoord.Y}_{chunkCoord.Z}";
            chunk.H3Index = _h3Index;
            chunk.ChunkCoord = chunkCoord;
            chunk.WorldGrid = this;
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

            GlobalToChunk(globalX, globalY, globalZ, out var chunkCoord, out var local);
            GetOrCreateChunk(chunkCoord)
                .SetVoxel(local.X, local.Y, local.Z, material, durability, trackEdit);
        }

        public VoxelMaterial GetVoxelGlobal(int globalX, int globalY, int globalZ)
        {
            if (globalY < 0)
                return VoxelMaterial.Air;

            GlobalToChunk(globalX, globalY, globalZ, out var chunkCoord, out var local);

            return _chunks.TryGetValue(chunkCoord, out var chunk)
                ? chunk.GetVoxel(local.X, local.Y, local.Z)
                : VoxelMaterial.Air;
        }

        public int CarveSphere(VoxelChunk sourceChunk, Vector3 localPosition, float radiusMeters)
        {
            Vector3I sourceOrigin = sourceChunk.ChunkCoord * VoxelChunk.Size;
            var center = new Vector3I(
                sourceOrigin.X + Mathf.RoundToInt(localPosition.X / VoxelChunk.VoxelScale),
                sourceOrigin.Y + Mathf.RoundToInt(localPosition.Y / VoxelChunk.VoxelScale),
                sourceOrigin.Z + Mathf.RoundToInt(localPosition.Z / VoxelChunk.VoxelScale)
            );

            int radius = Mathf.CeilToInt(radiusMeters / VoxelChunk.VoxelScale);
            int radiusSq = radius * radius;
            int destroyed = 0;

            for (int x = center.X - radius; x <= center.X + radius; x++)
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
            for (int z = center.Z - radius; z <= center.Z + radius; z++)
            {
                if (y < 0)
                    continue;

                int dx = x - center.X;
                int dy = y - center.Y;
                int dz = z - center.Z;

                if ((dx * dx) + (dy * dy) + (dz * dz) > radiusSq)
                    continue;

                if (GetVoxelGlobal(x, y, z) == VoxelMaterial.Air)
                    continue;

                SetVoxelGlobal(x, y, z, VoxelMaterial.Air, trackEdit: true);
                destroyed++;
            }

            if (destroyed > 0)
                RebuildDirtyChunks();

            return destroyed;
        }

        public void PlaceVoxelAtWorldPosition(
            Vector3 worldPosition,
            VoxelMaterial material,
            bool trackEdit = true)
        {
            int globalX = Mathf.FloorToInt(worldPosition.X / VoxelChunk.VoxelScale);
            int globalY = Mathf.FloorToInt(worldPosition.Y / VoxelChunk.VoxelScale);
            int globalZ = Mathf.FloorToInt(worldPosition.Z / VoxelChunk.VoxelScale);

            SetVoxelGlobal(globalX, globalY, globalZ, material, trackEdit: trackEdit);
            RebuildDirtyChunks();
        }

        public WorldVoxelEdit[] DrainPendingEdits()
        {
            var edits = new List<WorldVoxelEdit>();

            foreach (var pair in _chunks)
            {
                foreach (var edit in pair.Value.DrainPendingEdits())
                    edits.Add(new WorldVoxelEdit(pair.Key, edit));
            }

            return edits.ToArray();
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

        public void UpdateVisibility(Vector3 worldPosition, int horizontalRadiusChunks)
        {
            Vector3I center = WorldPositionToChunk(worldPosition);

            foreach (var pair in _chunks)
            {
                int dx = Math.Abs(pair.Key.X - center.X);
                int dz = Math.Abs(pair.Key.Z - center.Z);
                bool active = dx <= horizontalRadiusChunks && dz <= horizontalRadiusChunks;
                pair.Value.SetStreamingActive(active);
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

        private static void GlobalToChunk(
            int globalX,
            int globalY,
            int globalZ,
            out Vector3I chunkCoord,
            out Vector3I local)
        {
            chunkCoord = new Vector3I(
                FloorDiv(globalX, VoxelChunk.Size),
                FloorDiv(globalY, VoxelChunk.Size),
                FloorDiv(globalZ, VoxelChunk.Size)
            );

            local = new Vector3I(
                PositiveMod(globalX, VoxelChunk.Size),
                PositiveMod(globalY, VoxelChunk.Size),
                PositiveMod(globalZ, VoxelChunk.Size)
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
