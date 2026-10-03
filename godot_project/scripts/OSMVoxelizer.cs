using System;
using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.World.Generation
{
    public static class OSMVoxelizer
    {
        public static void VoxelizeBuilding(
            VoxelChunk chunk,
            Rect2 footprintMeters,
            float heightMeters,
            string amenityTag,
            long osmId)
        {
            int minX = ClampVoxel(Mathf.FloorToInt(footprintMeters.Position.X / VoxelChunk.VoxelScale));
            int maxX = ClampVoxel(Mathf.CeilToInt(footprintMeters.End.X / VoxelChunk.VoxelScale));
            int minZ = ClampVoxel(Mathf.FloorToInt(footprintMeters.Position.Y / VoxelChunk.VoxelScale));
            int maxZ = ClampVoxel(Mathf.CeilToInt(footprintMeters.End.Y / VoxelChunk.VoxelScale));
            int topY = Mathf.Clamp(Mathf.RoundToInt(heightMeters / VoxelChunk.VoxelScale), 4, VoxelChunk.Size - 1);

            var wall = amenityTag == "pharmacy" ? VoxelMaterial.Brick : VoxelMaterial.Concrete;
            int seed = StableHash(osmId, amenityTag);

            // Sol et trottoir.
            for (int x = 0; x < VoxelChunk.Size; x++)
            for (int z = 0; z < VoxelChunk.Size; z++)
                chunk.SetVoxel(x, 0, z, VoxelMaterial.Asphalt);

            for (int y = 1; y <= topY; y++)
            {
                bool floor = y == 1 || y == topY || (y > 1 && y % 15 == 0);
                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    bool perimeter = x == minX || x == maxX || z == minZ || z == maxZ;
                    if (floor)
                    {
                        chunk.SetVoxel(x, y, z, VoxelMaterial.Concrete);
                        continue;
                    }

                    if (!perimeter)
                        continue;

                    bool windowBand = y >= 6 && y <= 11;
                    bool window = windowBand && ((x + z + seed) % 4 == 0);
                    chunk.SetVoxel(x, y, z, window ? VoxelMaterial.ReinforcedGlass : wall);
                }
            }

            // Ressources cachées : leur position dépend uniquement de l'identifiant OSM.
            int copperX = Mathf.Clamp(minX + 1, 0, VoxelChunk.Size - 1);
            int copperZ = Mathf.Clamp(minZ + 2 + Math.Abs(seed % Math.Max(1, maxZ - minZ - 2)), 0, VoxelChunk.Size - 1);
            for (int y = 3; y < Math.Min(topY, 14); y++)
                chunk.SetVoxel(copperX, y, copperZ, VoxelMaterial.CopperWiring);

            if (amenityTag == "pharmacy")
            {
                int medX = Mathf.Clamp((minX + maxX) / 2, 0, VoxelChunk.Size - 1);
                int medZ = Mathf.Clamp(maxZ - 1, 0, VoxelChunk.Size - 1);
                chunk.SetVoxel(medX, Math.Min(5, topY - 1), medZ, VoxelMaterial.MedCache);
            }
        }

        private static int ClampVoxel(int value) => Mathf.Clamp(value, 0, VoxelChunk.Size - 1);

        private static int StableHash(long osmId, string tag)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (byte b in BitConverter.GetBytes(osmId))
                    hash = (hash ^ b) * 16777619;
                foreach (char c in tag ?? string.Empty)
                    hash = (hash ^ c) * 16777619;
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
