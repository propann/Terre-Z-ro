using System;
using System.Runtime.CompilerServices;
using Godot;

namespace TerreZero.World.Voxel
{
    public enum VoxelMaterial : byte
    {
        Air = 0,
        Concrete = 1,        // Béton armé
        Brick = 2,           // Brique rouge
        Asphalt = 3,         // Asphalte craquelé
        Sidewalk = 4,        // Bordure de trottoir
        ReinforcedGlass = 5, // Verre blindé
        CopperWiring = 6,    // Câblage cuivre interne (Loot précieux)
        MedCache = 7,        // Réserve médicale cachée
        SteelBarricade = 8,  // Blindage joueur
        TurretBase = 9,      // Base tourelle
        GrassOrganic = 10    // Mousse mutée
    }

    public struct Voxel16Bit
    {
        public ushort Raw; // [0..7]: Material, [8..11]: Durability, [12..15]: Light/State

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Voxel16Bit(VoxelMaterial mat, byte durability = 15, byte light = 0)
        {
            Raw = (ushort)(((byte)mat & 0xFF) | ((durability & 0x0F) << 8) | ((light & 0x0F) << 12));
        }

        public VoxelMaterial Material => (VoxelMaterial)(Raw & 0xFF);
        public byte Durability => (byte)((Raw >> 8) & 0x0F);
        public byte Light => (byte)((Raw >> 12) & 0x0F);
        public bool IsAir => (Raw & 0xFF) == 0;
    }

    public partial class VoxelChunk : Node3D
    {
        public const int Size = 32; // 32x32x32 voxels
        public const float VoxelScale = 0.2f; // 20 cm par voxel (Taille chunk = 6.4 m)

        private readonly ushort[] _voxels = new ushort[Size * Size * Size];
        public Vector3I ChunkCoord { get; set; }
        public bool IsDirty { get; set; } = true;

        [Export] public MeshInstance3D MeshInstance { get; set; }
        [Export] public StaticBody3D CollisionBody { get; set; }
        [Export] public CollisionShape3D CollisionShape { get; set; }

        public override void _Ready()
        {
            if (MeshInstance == null)
            {
                MeshInstance = new MeshInstance3D();
                AddChild(MeshInstance);
            }
            if (CollisionBody == null)
            {
                CollisionBody = new StaticBody3D();
                CollisionShape = new CollisionShape3D();
                CollisionBody.AddChild(CollisionShape);
                AddChild(CollisionBody);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z) => x + y * Size + z * Size * Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelMaterial GetVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size) return VoxelMaterial.Air;
            return (VoxelMaterial)(_voxels[GetIndex(x, y, z)] & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVoxel(int x, int y, int z, VoxelMaterial mat, byte durability = 15)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size) return;
            _voxels[GetIndex(x, y, z)] = (ushort)(((byte)mat & 0xFF) | ((durability & 0x0F) << 8));
            IsDirty = true;
        }

        // Minage chirurgical / Forage au laser (Sprint 3.1)
        public int CarveSphere(Vector3 localPos, float radiusMeters)
        {
            int cx = Mathf.RoundToInt(localPos.X / VoxelScale);
            int cy = Mathf.RoundToInt(localPos.Y / VoxelScale);
            int cz = Mathf.RoundToInt(localPos.Z / VoxelScale);
            int r = Mathf.CeilToInt(radiusMeters / VoxelScale);
            int rSq = r * r;
            int destroyed = 0;

            for (int x = Math.Max(0, cx - r); x <= Math.Min(Size - 1, cx + r); x++)
            for (int y = Math.Max(0, cy - r); y <= Math.Min(Size - 1, cy + r); y++)
            for (int z = Math.Max(0, cz - r); z <= Math.Min(Size - 1, cz + r); z++)
            {
                int dSq = (x - cx) * (x - cx) + (y - cy) * (y - cy) + (z - cz) * (z - cz);
                if (dSq <= rSq)
                {
                    if (GetVoxel(x, y, z) != VoxelMaterial.Air)
                    {
                        SetVoxel(x, y, z, VoxelMaterial.Air);
                        destroyed++;
                    }
                }
            }

            if (destroyed > 0)
            {
                RebuildMeshGreedy();
            }

            return destroyed;
        }

        public void RebuildMeshGreedy()
        {
            GreedyMesher.GenerateMesh(this);
            IsDirty = false;
        }
    }
}
