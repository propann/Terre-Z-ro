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
        public int TouchedChunks { get; }

        public BuildingGenerationResult(int seed, int floors, int rooms, int touchedChunks)
        {
            Seed = seed;
            Floors = floors;
            Rooms = rooms;
            TouchedChunks = touchedChunks;
        }
    }

    public static class OSMVoxelizer
    {
        public const int WorldVersion = 1;
        public const int GeneratorVersion = 3;

        private const int FloorHeightVoxels = 15;
        private const int DoorHeightVoxels = 10;

        public static BuildingGenerationResult VoxelizeBuilding(
            VoxelWorldGrid world,
            Rect2 footprintMeters,
            float heightMeters,
            string buildingType,
            string amenityTag,
            long osmId)
        {
            int minX = Mathf.FloorToInt(footprintMeters.Position.X / VoxelChunk.VoxelScale);
            int maxX = Mathf.CeilToInt(footprintMeters.End.X / VoxelChunk.VoxelScale);
            int minZ = Mathf.FloorToInt(footprintMeters.Position.Y / VoxelChunk.VoxelScale);
            int maxZ = Mathf.CeilToInt(footprintMeters.End.Y / VoxelChunk.VoxelScale);
            int topY = Math.Max(4, Mathf.RoundToInt(heightMeters / VoxelChunk.VoxelScale));

            int seed = StableHash(
                WorldVersion,
                GeneratorVersion,
                osmId,
                buildingType,
                amenityTag
            );

            var rng = new DeterministicRng((uint)seed);
            var wallMaterial = ResolveWallMaterial(buildingType, amenityTag);

            GenerateGround(world, minX - 3, maxX + 3, minZ - 3, maxZ + 3);
            GenerateShell(world, minX, maxX, minZ, maxZ, topY, wallMaterial, seed);

            int floorCount = Math.Max(1, (topY - 1) / FloorHeightVoxels + 1);
            int roomCount = GenerateInteriors(
                world,
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
                world,
                minX,
                maxX,
                minZ,
                maxZ,
                topY,
                amenityTag,
                ref rng
            );

            world.RebuildDirtyChunks();

            return new BuildingGenerationResult(
                seed,
                floorCount,
                roomCount,
                world.LoadedChunkCount
            );
        }

        private static void GenerateGround(
            VoxelWorldGrid world,
            int minX,
            int maxX,
            int minZ,
            int maxZ)
        {
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
                world.SetVoxelGlobal(x, 0, z, VoxelMaterial.Asphalt);
        }

        private static void GenerateShell(
            VoxelWorldGrid world,
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
                bool floorSlab =
                    y == 1 ||
                    y == topY ||
                    (y > 1 && (y - 1) % FloorHeightVoxels == 0);

                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    bool perimeter =
                        x == minX ||
                        x == maxX ||
                        z == minZ ||
                        z == maxZ;

                    if (floorSlab)
                    {
                        world.SetVoxelGlobal(x, y, z, VoxelMaterial.Concrete);
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
                        world.SetVoxelGlobal(x, y, z, VoxelMaterial.Air);
                        continue;
                    }

                    int localFloorY = (y - 2) % FloorHeightVoxels;
                    bool windowBand = localFloorY >= 5 && localFloorY <= 10;
                    bool window =
                        windowBand &&
                        ((x * 31 + z * 17 + seed) & 3) == 0;

                    world.SetVoxelGlobal(
                        x,
                        y,
                        z,
                        window ? VoxelMaterial.ReinforcedGlass : wallMaterial
                    );
                }
            }
        }

        private static int GenerateInteriors(
            VoxelWorldGrid world,
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

                if (splitAlongX)
                {
                    int split = rng.NextInt(minX + 3, maxX - 2);
                    BuildPartitionX(
                        world,
                        split,
                        minZ + 1,
                        maxZ - 1,
                        floorBase + 1,
                        ceiling - 1,
                        wallMaterial,
                        ref rng
                    );
                }
                else
                {
                    int split = rng.NextInt(minZ + 3, maxZ - 2);
                    BuildPartitionZ(
                        world,
                        split,
                        minX + 1,
                        maxX - 1,
                        floorBase + 1,
                        ceiling - 1,
                        wallMaterial,
                        ref rng
                    );
                }

                rooms += 2;

                if (width >= 32 && depth >= 32)
                {
                    if (splitAlongX)
                    {
                        int second = rng.NextInt(minZ + 5, maxZ - 4);
                        BuildPartitionZ(
                            world,
                            second,
                            minX + 1,
                            maxX - 1,
                            floorBase + 1,
                            ceiling - 1,
                            wallMaterial,
                            ref rng
                        );
                    }
                    else
                    {
                        int second = rng.NextInt(minX + 5, maxX - 4);
                        BuildPartitionX(
                            world,
                            second,
                            minZ + 1,
                            maxZ - 1,
                            floorBase + 1,
                            ceiling - 1,
                            wallMaterial,
                            ref rng
                        );
                    }

                    rooms += 2;
                }

                if (floor < floorCount - 1)
                {
                    GenerateStaircase(
                        world,
                        minX,
                        maxX,
                        minZ,
                        maxZ,
                        floorBase,
                        ceiling,
                        ref rng
                    );
                }
            }

            return rooms;
        }

        private static void BuildPartitionX(
            VoxelWorldGrid world,
            int x,
            int startZ,
            int endZ,
            int startY,
            int endY,
            VoxelMaterial material,
            ref DeterministicRng rng)
        {
            int doorZ = rng.NextInt(startZ + 2, Math.Max(startZ + 3, endZ - 1));

            for (int y = startY; y <= endY; y++)
            for (int z = startZ; z <= endZ; z++)
            {
                bool doorway =
                    Math.Abs(z - doorZ) <= 1 &&
                    y < startY + DoorHeightVoxels;

                world.SetVoxelGlobal(
                    x,
                    y,
                    z,
                    doorway ? VoxelMaterial.Air : material
                );
            }
        }

        private static void BuildPartitionZ(
            VoxelWorldGrid world,
            int z,
            int startX,
            int endX,
            int startY,
            int endY,
            VoxelMaterial material,
            ref DeterministicRng rng)
        {
            int doorX = rng.NextInt(startX + 2, Math.Max(startX + 3, endX - 1));

            for (int y = startY; y <= endY; y++)
            for (int x = startX; x <= endX; x++)
            {
                bool doorway =
                    Math.Abs(x - doorX) <= 1 &&
                    y < startY + DoorHeightVoxels;

                world.SetVoxelGlobal(
                    x,
                    y,
                    z,
                    doorway ? VoxelMaterial.Air : material
                );
            }
        }

        private static void GenerateStaircase(
            VoxelWorldGrid world,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int floorBase,
            int ceiling,
            ref DeterministicRng rng)
        {
            int baseX = rng.NextBool() ? minX + 3 : maxX - 8;
            int baseZ = rng.NextBool() ? minZ + 3 : maxZ - 5;
            int usableRise = Math.Min(
                FloorHeightVoxels - 1,
                ceiling - floorBase - 1
            );

            for (int step = 0; step < usableRise; step++)
            {
                int x = baseX + step / 3;
                int y = floorBase + 1 + step;

                for (int clearY = y + 1;
                     clearY <= Math.Min(ceiling - 1, y + 9);
                     clearY++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    for (int dz = 0; dz < 3; dz++)
                        world.SetVoxelGlobal(
                            x + dx,
                            clearY,
                            baseZ + dz,
                            VoxelMaterial.Air
                        );
                }

                for (int dx = 0; dx < 2; dx++)
                for (int dz = 0; dz < 3; dz++)
                    world.SetVoxelGlobal(
                        x + dx,
                        y,
                        baseZ + dz,
                        VoxelMaterial.Concrete
                    );
            }
        }

        private static void GenerateUtilitiesAndLoot(
            VoxelWorldGrid world,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            string amenityTag,
            ref DeterministicRng rng)
        {
            int copperX = rng.NextInt(minX + 1, Math.Max(minX + 2, maxX));
            int copperZ = rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ));

            for (int y = 3; y < Math.Min(topY, 14); y++)
                world.SetVoxelGlobal(
                    copperX,
                    y,
                    copperZ,
                    VoxelMaterial.CopperWiring
                );

            if (amenityTag == "pharmacy")
            {
                int medX = rng.NextInt(minX + 1, Math.Max(minX + 2, maxX));
                int medZ = rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ));

                world.SetVoxelGlobal(
                    medX,
                    Math.Min(5, topY - 1),
                    medZ,
                    VoxelMaterial.MedCache
                );
            }
        }

        private static VoxelMaterial ResolveWallMaterial(
            string buildingType,
            string amenityTag)
        {
            if (amenityTag == "pharmacy" || buildingType == "retail")
                return VoxelMaterial.Brick;

            if (buildingType == "industrial")
                return VoxelMaterial.SteelBarricade;

            return VoxelMaterial.Concrete;
        }

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
