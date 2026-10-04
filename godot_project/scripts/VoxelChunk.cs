using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace TerreZero.World.Voxel
{
    public enum VoxelMaterial : byte
    {
        Air = 0,
        Concrete = 1,
        Brick = 2,
        Asphalt = 3,
        Sidewalk = 4,
        ReinforcedGlass = 5,
        CopperWiring = 6,
        MedCache = 7,
        SteelBarricade = 8,
        TurretBase = 9,
        GrassOrganic = 10,
        RoadMarking = 11
    }

    public readonly struct VoxelEdit
    {
        public Vector3I Position { get; }
        public VoxelMaterial PreviousMaterial { get; }
        public VoxelMaterial NewMaterial { get; }

        public VoxelEdit(Vector3I position, VoxelMaterial previousMaterial, VoxelMaterial newMaterial)
        {
            Position = position;
            PreviousMaterial = previousMaterial;
            NewMaterial = newMaterial;
        }
    }

    public struct Voxel16Bit
    {
        public ushort Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Voxel16Bit(VoxelMaterial mat, byte durability = 15, byte state = 0)
        {
            Raw = (ushort)(((byte)mat & 0xFF) | ((durability & 0x0F) << 8) | ((state & 0x0F) << 12));
        }

        public VoxelMaterial Material => (VoxelMaterial)(Raw & 0xFF);
        public byte Durability => (byte)((Raw >> 8) & 0x0F);
        public byte State => (byte)((Raw >> 12) & 0x0F);
        public bool IsAir => (Raw & 0xFF) == 0;
    }

    public partial class VoxelChunk : Node3D
    {
        public const int Size = 32;
        public const float VoxelScale = 0.2f;

        private readonly ushort[] _voxels = new ushort[Size * Size * Size];
        private readonly List<VoxelEdit> _pendingEdits = new();
        private float _surfaceWetness;
        private float _snowCover;
        private bool _xrayActive;

        [Export] public string H3Index { get; set; } = "891fb466257ffff";
        [Export] public Vector3I ChunkCoord { get; set; } = Vector3I.Zero;
        public bool IsDirty { get; private set; } = true;
        public VoxelWorldGrid WorldGrid { get; set; }

        [Export] public MeshInstance3D MeshInstance { get; set; }
        [Export] public StaticBody3D CollisionBody { get; set; }
        [Export] public CollisionShape3D CollisionShape { get; set; }

        public override void _Ready()
        {
            MeshInstance ??= GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
            if (MeshInstance == null)
            {
                MeshInstance = new MeshInstance3D { Name = "MeshInstance3D" };
                AddChild(MeshInstance);
            }

            CollisionBody ??= GetNodeOrNull<StaticBody3D>("StaticBody3D");
            if (CollisionBody == null)
            {
                CollisionBody = new StaticBody3D { Name = "StaticBody3D" };
                AddChild(CollisionBody);
            }

            CollisionShape ??= CollisionBody.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (CollisionShape == null)
            {
                CollisionShape = new CollisionShape3D { Name = "CollisionShape3D" };
                CollisionBody.AddChild(CollisionShape);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z) => x + y * Size + z * Size * Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelMaterial GetVoxel(int x, int y, int z)
        {
            if (!IsInside(x, y, z))
                return VoxelMaterial.Air;
            return (VoxelMaterial)(_voxels[GetIndex(x, y, z)] & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVoxel(
            int x,
            int y,
            int z,
            VoxelMaterial material,
            byte durability = 15,
            bool trackEdit = false)
        {
            if (!IsInside(x, y, z))
                return;

            var previous = GetVoxel(x, y, z);
            if (previous == material)
                return;

            _voxels[GetIndex(x, y, z)] =
                (ushort)(((byte)material & 0xFF) | ((durability & 0x0F) << 8));
            IsDirty = true;

            if (trackEdit)
                _pendingEdits.Add(new VoxelEdit(new Vector3I(x, y, z), previous, material));
        }

        public int CarveSphere(Vector3 localPos, float radiusMeters)
        {
            if (WorldGrid != null)
                return WorldGrid.CarveSphere(this, localPos, radiusMeters);

            int cx = Mathf.RoundToInt(localPos.X / VoxelScale);
            int cy = Mathf.RoundToInt(localPos.Y / VoxelScale);
            int cz = Mathf.RoundToInt(localPos.Z / VoxelScale);
            int radius = Mathf.CeilToInt(radiusMeters / VoxelScale);
            int radiusSq = radius * radius;
            int destroyed = 0;

            for (int x = Math.Max(0, cx - radius); x <= Math.Min(Size - 1, cx + radius); x++)
            for (int y = Math.Max(0, cy - radius); y <= Math.Min(Size - 1, cy + radius); y++)
            for (int z = Math.Max(0, cz - radius); z <= Math.Min(Size - 1, cz + radius); z++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int dz = z - cz;

                if ((dx * dx) + (dy * dy) + (dz * dz) > radiusSq)
                    continue;

                if (GetVoxel(x, y, z) == VoxelMaterial.Air)
                    continue;

                SetVoxel(x, y, z, VoxelMaterial.Air, trackEdit: true);
                destroyed++;
            }

            if (destroyed > 0)
                RebuildMeshGreedy();

            return destroyed;
        }

        public VoxelEdit[] DrainPendingEdits()
        {
            if (_pendingEdits.Count == 0)
                return Array.Empty<VoxelEdit>();

            var edits = _pendingEdits.ToArray();
            _pendingEdits.Clear();
            return edits;
        }

        public void RebuildMeshGreedy()
        {
            GreedyMesher.GenerateMesh(this);
            ApplySurfaceWetness();
            IsDirty = false;
        }

        public void SetSurfaceWetness(float wetness)
        {
            _surfaceWetness = Mathf.Clamp(wetness, 0f, 1f);
            ApplySurfaceWetness();
        }

        public void SetSnowCover(float snowCover)
        {
            _snowCover = Mathf.Clamp(snowCover, 0f, 1f);
            ApplySurfaceWetness();
        }

        public void SetXrayActive(bool active)
        {
            _xrayActive = active;
            ApplySurfaceWetness();
        }

        private void ApplySurfaceWetness()
        {
            if (MeshInstance?.MaterialOverride is not ShaderMaterial material)
                return;

            material.SetShaderParameter("wetness", _surfaceWetness);
            material.SetShaderParameter("snow_cover", _snowCover);
            material.SetShaderParameter("xray_mode", _xrayActive);
        }

        public void SetStreamingActive(bool active)
        {
            Visible = active;

            if (CollisionShape != null)
                CollisionShape.SetDeferred("disabled", !active);
        }

        private static bool IsInside(int x, int y, int z) =>
            x >= 0 && x < Size &&
            y >= 0 && y < Size &&
            z >= 0 && z < Size;
    }
}
