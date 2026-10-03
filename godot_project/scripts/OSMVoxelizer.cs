using System;
using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.World.Generation
{
    public readonly struct BuildingGenerationResult
    {
        public int Seed { get; }
        public int Floors { get; }
        public int Rooms { get; }

        public BuildingGenerationResult(int seed, int floors, int rooms)
        {
            Seed = seed;
            Floors = floors;
            Rooms = rooms;
        }
    }

    public static class OSMVoxelizer
    {
        public const int WorldVersion = 1;
        public const int GeneratorVersion = 2;

        private const int FloorHeightVoxels = 15; // 3 m à 20 cm/voxel
        private const int DoorHeightVoxels = 10; // 2 m

        public static BuildingGenerationResult VoxelizeBuilding(
            VoxelChunk chunk,
            Rect2 footprintMeters,
            float heightMeters,
            string buildingType,
            string amenityTag,
            long osmId)
        {
            int minX = ClampVoxel(Mathf.FloorToInt(footprintMeters.Position.X / VoxelChunk.VoxelScale));
            int maxX = ClampVoxel(Mathf.CeilToInt(footprintMeters.End.X / VoxelChunk.VoxelScale));
            int minZ = ClampVoxel(Mathf.FloorToInt(footprintMeters.Position.Y / VoxelChunk.VoxelScale));
            int maxZ = ClampVoxel(Mathf.CeilToInt(footprintMeters.End.Y / VoxelChunk.VoxelScale));
            int topY = Mathf.Clamp(Mathf.RoundToInt(heightMeters / VoxelChunk.VoxelScale), 4, VoxelChunk.Size - 1);

            int seed = StableHash(WorldVersion, GeneratorVersion, osmId, buildingType, amenityTag);
            var rng = new DeterministicRng((uint)seed);
            var wallMaterial = ResolveWallMaterial(buildingType, amenityTag);

            GenerateGround(chunk);
            GenerateShell(chunk, minX, maxX, minZ, maxZ, topY, wallMaterial, seed);

            int floorCount = Math.Max(1, (topY - 1) / FloorHeightVoxels + 1);
            int roomCount = GenerateInteriors(
                chunk,
                minX,
                maxX,
                minZ,
                maxZ,
                topY,
                floorCount,
                wallMaterial,
                ref rng
            );

            GenerateUtilitiesAndLoot(
                chunk,
                minX,
                maxX,
                minZ,
                maxZ,
                topY,
                amenityTag,
                ref rng
            );

            return new BuildingGenerationResult(seed, floorCount, roomCount);
        }

        private static void GenerateGround(VoxelChunk chunk)
        {
            for (int x = 0; x < VoxelChunk.Size; x++)
            for (int z = 0; z < VoxelChunk.Size; z++)
                chunk.SetVoxel(x, 0, z, VoxelMaterial.Asphalt);
        }

        private static void GenerateShell(
            VoxelChunk chunk,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            VoxelMaterial wallMaterial,
            int seed)
        {
            int entranceX = (minX + maxX) / 2;
            int entranceZ = minZ;

            for (int y = 1; y <= topY; y++)
            {
                bool floorSlab = y == 1 || y == topY || (y > 1 && (y - 1) % FloorHeightVoxels == 0);

                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    bool perimeter = x == minX || x == maxX || z == minZ || z == maxZ;

                    if (floorSlab)
                    {
                        chunk.SetVoxel(x, y, z, VoxelMaterial.Concrete);
                        continue;
                    }

                    if (!perimeter)
                        continue;

                    bool entrance =
                        z == entranceZ &&
                        Math.Abs(x - entranceX) <= 2 &&
                        y >= 2 &&
                        y <= DoorHeightVoxels + 1;

                    if (entrance)
                    {
                        chunk.SetVoxel(x, y, z, VoxelMaterial.Air);
                        continue;
                    }

                    int localFloorY = (y - 2) % FloorHeightVoxels;
                    bool windowBand = localFloorY >= 5 && localFloorY <= 10;
                    bool window = windowBand && ((x * 31 + z * 17 + seed) & 3) == 0;

                    chunk.SetVoxel(
                        x,
                        y,
                        z,
                        window ? VoxelMaterial.ReinforcedGlass : wallMaterial
                    );
                }
            }
        }

        private static int GenerateInteriors(
            VoxelChunk chunk,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            int floorCount,
            VoxelMaterial wallMaterial,
            ref DeterministicRng rng)
        {
            int width = maxX - minX - 1;
            int depth = maxZ - minZ - 1;
            if (width < 7 || depth < 7)
                return 0;

            int rooms = 0;

            for (int floor = 0; floor < floorCount; floor++)
            {
                int floorBase = 1 + floor * FloorHeightVoxels;
                int ceiling = Math.Min(topY, floorBase + FloorHeightVoxels);
                if (ceiling - floorBase < 5)
                    continue;

                bool splitAlongX = rng.NextBool();
                int minSplit;
                int maxSplit;
                int split;

                if (splitAlongX)
                {
                    minSplit = minX + 3;
                    maxSplit = maxX - 3;
                    if (maxSplit <= minSplit)
                        continue;

                    split = rng.NextInt(minSplit, maxSplit + 1);
                    BuildPartitionX(chunk, split, minZ + 1, maxZ - 1, floorBase + 1, ceiling - 1, wallMaterial, ref rng);
                }
                else
                {
                    minSplit = minZ + 3;
                    maxSplit = maxZ - 3;
                    if (maxSplit <= minSplit)
                        continue;

                    split = rng.NextInt(minSplit, maxSplit + 1);
                    BuildPartitionZ(chunk, split, minX + 1, maxX - 1, floorBase + 1, ceiling - 1, wallMaterial, ref rng);
                }

                rooms += 2;

                // Les grands plateaux reçoivent une seconde cloison déterministe.
                if (width >= 16 && depth >= 16 && rng.NextBool())
                {
                    if (splitAlongX)
                    {
                        int second = rng.NextInt(minZ + 3, maxZ - 2);
                        BuildPartitionZ(chunk, second, minX + 1, maxX - 1, floorBase + 1, ceiling - 1, wallMaterial, ref rng);
                    }
                    else
                    {
                        int second = rng.NextInt(minX + 3, maxX - 2);
                        BuildPartitionX(chunk, second, minZ + 1, maxZ - 1, floorBase + 1, ceiling - 1, wallMaterial, ref rng);
                    }
                    rooms += 2;
                }

                if (floor < floorCount - 1)
                    GenerateStaircase(chunk, minX, maxX, minZ, maxZ, floorBase, ceiling, ref rng);
            }

            return rooms;
        }

        private static void BuildPartitionX(
            VoxelChunk chunk,
            int x,
            int startZ,
            int endZ,
            int startY,
            int endY,
            VoxelMaterial material,
            ref DeterministicRng rng)
        {
            int doorZ = rng.NextInt(startZ + 1, Math.Max(startZ + 2, endZ));

            for (int y = startY; y <= endY; y++)
            for (int z = startZ; z <= endZ; z++)
            {
                bool doorway = Math.Abs(z - doorZ) <= 1 && y < startY + DoorHeightVoxels;
                chunk.SetVoxel(x, y, z, doorway ? VoxelMaterial.Air : material);
            }
        }

        private static void BuildPartitionZ(
            VoxelChunk chunk,
            int z,
            int startX,
            int endX,
            int startY,
            int endY,
            VoxelMaterial material,
            ref DeterministicRng rng)
        {
            int doorX = rng.NextInt(startX + 1, Math.Max(startX + 2, endX));

            for (int y = startY; y <= endY; y++)
            for (int x = startX; x <= endX; x++)
            {
                bool doorway = Math.Abs(x - doorX) <= 1 && y < startY + DoorHeightVoxels;
                chunk.SetVoxel(x, y, z, doorway ? VoxelMaterial.Air : material);
            }
        }

        private static void GenerateStaircase(
            VoxelChunk chunk,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int floorBase,
            int ceiling,
            ref DeterministicRng rng)
        {
            int stairX = rng.NextBool() ? minX + 2 : maxX - 3;
            int stairZ = rng.NextBool() ? minZ + 2 : maxZ - 3;

            int usableRise = Math.Min(FloorHeightVoxels - 1, ceiling - floorBase - 1);
            for (int step = 0; step < usableRise; step++)
            {
                int x = Mathf.Clamp(stairX + (step / 3), minX + 1, maxX - 1);
                int z = stairZ;
                int y = floorBase + 1 + step;

                // Dégagement au-dessus de la marche.
                for (int clearY = y + 1; clearY <= Math.Min(ceiling - 1, y + 9); clearY++)
                for (int dx = 0; dx < 2; dx++)
                for (int dz = 0; dz < 3; dz++)
                    chunk.SetVoxel(
                        Mathf.Clamp(x + dx, minX + 1, maxX - 1),
                        clearY,
                        Mathf.Clamp(z + dz, minZ + 1, maxZ - 1),
                        VoxelMaterial.Air
                    );

                for (int dx = 0; dx < 2; dx++)
                for (int dz = 0; dz < 3; dz++)
                    chunk.SetVoxel(
                        Mathf.Clamp(x + dx, minX + 1, maxX - 1),
                        y,
                        Mathf.Clamp(z + dz, minZ + 1, maxZ - 1),
                        VoxelMaterial.Concrete
                    );
            }
        }

        private static void GenerateUtilitiesAndLoot(
            VoxelChunk chunk,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            string amenityTag,
            ref DeterministicRng rng)
        {
            int copperX = Mathf.Clamp(rng.NextInt(minX + 1, Math.Max(minX + 2, maxX)), 0, VoxelChunk.Size - 1);
            int copperZ = Mathf.Clamp(rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ)), 0, VoxelChunk.Size - 1);

            for (int y = 3; y < Math.Min(topY, 14); y++)
                chunk.SetVoxel(copperX, y, copperZ, VoxelMaterial.CopperWiring);

            if (amenityTag == "pharmacy")
            {
                int medX = Mathf.Clamp(rng.NextInt(minX + 1, Math.Max(minX + 2, maxX)), 0, VoxelChunk.Size - 1);
                int medZ = Mathf.Clamp(rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ)), 0, VoxelChunk.Size - 1);
                chunk.SetVoxel(medX, Math.Min(5, topY - 1), medZ, VoxelMaterial.MedCache);
            }
        }

        private static VoxelMaterial ResolveWallMaterial(string buildingType, string amenityTag)
        {
            if (amenityTag == "pharmacy" || buildingType == "retail")
                return VoxelMaterial.Brick;

            if (buildingType == "industrial")
                return VoxelMaterial.SteelBarricade;

            return VoxelMaterial.Concrete;
        }

        private static int ClampVoxel(int value) =>
            Mathf.Clamp(value, 0, VoxelChunk.Size - 1);

        public static int StableHash(
            int worldVersion,
            int generatorVersion,
            long osmId,
            string buildingType,
            string amenityTag)
        {
            unchecked
            {
                uint hash = 2166136261;

                Mix(ref hash, worldVersion);
                Mix(ref hash, generatorVersion);

                foreach (byte b in BitConverter.GetBytes(osmId))
                    hash = (hash ^ b) * 16777619;

                Mix(ref hash, buildingType);
                Mix(ref hash, amenityTag);

                return (int)(hash & 0x7fffffff);
            }
        }

        private static void Mix(ref uint hash, int value)
        {
            foreach (byte b in BitConverter.GetBytes(value))
                hash = (hash ^ b) * 16777619;
        }

        private static void Mix(ref uint hash, string value)
        {
            foreach (char c in value ?? string.Empty)
                hash = (hash ^ c) * 16777619;
        }

        private struct DeterministicRng
        {
            private uint _state;

            public DeterministicRng(uint seed)
            {
                _state = seed == 0 ? 0x6D2B79F5u : seed;
            }

            public uint Next()
            {
                uint x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }

            public bool NextBool() => (Next() & 1u) == 1u;

            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive)
                    return minInclusive;

                uint range = (uint)(maxExclusive - minInclusive);
                return minInclusive + (int)(Next() % range);
            }
        }
    }
}
