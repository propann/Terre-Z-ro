using System;
using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.World.Generation
{
    public static class OSMPolygonVoxelizer
    {
        private const int FloorHeightVoxels = 15;
        private const int DoorHeightVoxels = 10;

        public static BuildingGenerationResult VoxelizeBuilding(
            VoxelWorldGrid world,
            Vector2[] footprintMeters,
            float heightMeters,
            string buildingType,
            string amenityTag,
            long osmId)
        {
            if (footprintMeters == null || footprintMeters.Length < 3)
                return new BuildingGenerationResult(0, 0, 0, world.LoadedChunkCount);

            int seed = OSMVoxelizer.StableHash(
                OSMVoxelizer.WorldVersion,
                OSMVoxelizer.GeneratorVersion,
                osmId,
                buildingType,
                amenityTag
            );

            var bounds = ComputeBounds(footprintMeters);
            int minX = Mathf.FloorToInt(bounds.Position.X / VoxelChunk.VoxelScale);
            int maxX = Mathf.CeilToInt(bounds.End.X / VoxelChunk.VoxelScale);
            int minZ = Mathf.FloorToInt(bounds.Position.Y / VoxelChunk.VoxelScale);
            int maxZ = Mathf.CeilToInt(bounds.End.Y / VoxelChunk.VoxelScale);
            int topY = Math.Max(4, Mathf.RoundToInt(heightMeters / VoxelChunk.VoxelScale));
            int floorCount = Math.Max(1, (topY - 1) / FloorHeightVoxels + 1);

            var wall = ResolveWallMaterial(buildingType, amenityTag);
            var rng = new DeterministicRng((uint)seed);

            GenerateGround(world, minX, maxX, minZ, maxZ);
            GenerateShell(
                world,
                footprintMeters,
                minX,
                maxX,
                minZ,
                maxZ,
                topY,
                wall,
                seed,
                buildingType,
                amenityTag
            );
            int rooms = GenerateInteriorPartitions(
                world,
                footprintMeters,
                minX,
                maxX,
                minZ,
                maxZ,
                topY,
                floorCount,
                wall,
                ref rng
            );
            GenerateContextLoot(
                world,
                footprintMeters,
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
                rooms,
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
            for (int x = minX - 3; x <= maxX + 3; x++)
            for (int z = minZ - 3; z <= maxZ + 3; z++)
            {
                if (world.GetVoxelGlobal(x, 0, z) == VoxelMaterial.Air)
                    world.SetVoxelGlobal(x, 0, z, VoxelMaterial.Sidewalk);
            }
        }

        private static void GenerateShell(
            VoxelWorldGrid world,
            Vector2[] polygon,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            VoxelMaterial wall,
            int seed,
            string buildingType,
            string amenityTag)
        {
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!IsVoxelInside(polygon, x, z))
                    continue;

                bool perimeter =
                    !IsVoxelInside(polygon, x + 1, z) ||
                    !IsVoxelInside(polygon, x - 1, z) ||
                    !IsVoxelInside(polygon, x, z + 1) ||
                    !IsVoxelInside(polygon, x, z - 1);

                for (int y = 1; y <= topY; y++)
                {
                    bool slab =
                        y == 1 ||
                        y == topY ||
                        (y > 1 && (y - 1) % FloorHeightVoxels == 0);

                    if (slab)
                    {
                        world.SetVoxelGlobal(x, y, z, VoxelMaterial.Concrete);
                        continue;
                    }

                    if (!perimeter)
                        continue;

                    int localFloorY = (y - 2) % FloorHeightVoxels;
                    bool windowBand = localFloorY >= 5 && localFloorY <= 10;
                    bool storefront =
                        (buildingType == "retail" || amenityTag == "pharmacy") &&
                        y >= 4 &&
                        y <= 11;
                    bool industrial =
                        buildingType == "industrial";

                    bool window =
                        !industrial &&
                        ((storefront && (x + z + seed) % 3 != 0) ||
                         (windowBand &&
                          ((x * 31 + z * 17 + seed) & 3) == 0));

                    world.SetVoxelGlobal(
                        x,
                        y,
                        z,
                        window ? VoxelMaterial.ReinforcedGlass : wall
                    );
                }
            }

            // Roof parapet follows the real polygon perimeter.
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!IsVoxelInside(polygon, x, z))
                    continue;

                bool perimeter =
                    !IsVoxelInside(polygon, x + 1, z) ||
                    !IsVoxelInside(polygon, x - 1, z) ||
                    !IsVoxelInside(polygon, x, z + 1) ||
                    !IsVoxelInside(polygon, x, z - 1);

                if (perimeter)
                {
                    world.SetVoxelGlobal(
                        x,
                        topY + 1,
                        z,
                        wall
                    );
                }
            }

            CutDeterministicEntrance(world, polygon, minX, maxX, minZ, maxZ, seed);
        }

        private static void CutDeterministicEntrance(
            VoxelWorldGrid world,
            Vector2[] polygon,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int seed)
        {
            int bestX = minX;
            int bestZ = minZ;
            bool found = false;

            for (int z = minZ; z <= maxZ && !found; z++)
            for (int x = minX; x <= maxX; x++)
            {
                if (!IsVoxelInside(polygon, x, z))
                    continue;

                if (IsVoxelInside(polygon, x, z - 1))
                    continue;

                bestX = x;
                bestZ = z;
                found = true;
                break;
            }

            if (!found)
                return;

            int shift = Math.Abs(seed % 3) - 1;
            bestX += shift;

            for (int x = bestX - 1; x <= bestX + 1; x++)
            for (int y = 2; y <= DoorHeightVoxels + 1; y++)
            {
                if (IsVoxelInside(polygon, x, bestZ))
                    world.SetVoxelGlobal(x, y, bestZ, VoxelMaterial.Air);
            }
        }

        private static int GenerateInteriorPartitions(
            VoxelWorldGrid world,
            Vector2[] polygon,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            int floorCount,
            VoxelMaterial wall,
            ref DeterministicRng rng)
        {
            int rooms = 0;
            int width = maxX - minX;
            int depth = maxZ - minZ;

            if (width < 8 || depth < 8)
                return rooms;

            for (int floor = 0; floor < floorCount; floor++)
            {
                int floorBase = 1 + floor * FloorHeightVoxels;
                int ceiling = Math.Min(topY, floorBase + FloorHeightVoxels);
                if (ceiling - floorBase < 5)
                    continue;

                bool splitX = rng.NextBool();
                int split = splitX
                    ? rng.NextInt(minX + 3, Math.Max(minX + 4, maxX - 2))
                    : rng.NextInt(minZ + 3, Math.Max(minZ + 4, maxZ - 2));

                int door = splitX
                    ? rng.NextInt(minZ + 2, Math.Max(minZ + 3, maxZ - 1))
                    : rng.NextInt(minX + 2, Math.Max(minX + 3, maxX - 1));

                if (splitX)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    for (int y = floorBase + 1; y < ceiling; y++)
                    {
                        if (!IsVoxelInside(polygon, split, z))
                            continue;

                        bool doorway = Math.Abs(z - door) <= 1 && y < floorBase + DoorHeightVoxels;
                        world.SetVoxelGlobal(split, y, z, doorway ? VoxelMaterial.Air : wall);
                    }
                }
                else
                {
                    for (int x = minX; x <= maxX; x++)
                    for (int y = floorBase + 1; y < ceiling; y++)
                    {
                        if (!IsVoxelInside(polygon, x, split))
                            continue;

                        bool doorway = Math.Abs(x - door) <= 1 && y < floorBase + DoorHeightVoxels;
                        world.SetVoxelGlobal(x, y, split, doorway ? VoxelMaterial.Air : wall);
                    }
                }

                rooms += 2;
            }

            return rooms;
        }

        private static void GenerateContextLoot(
            VoxelWorldGrid world,
            Vector2[] polygon,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int topY,
            string amenityTag,
            ref DeterministicRng rng)
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                int x = rng.NextInt(minX + 1, Math.Max(minX + 2, maxX));
                int z = rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ));

                if (!IsVoxelInside(polygon, x, z))
                    continue;

                for (int y = 3; y < Math.Min(topY, 14); y++)
                    world.SetVoxelGlobal(x, y, z, VoxelMaterial.CopperWiring);

                break;
            }

            if (amenityTag != "pharmacy")
                return;

            for (int attempt = 0; attempt < 32; attempt++)
            {
                int x = rng.NextInt(minX + 1, Math.Max(minX + 2, maxX));
                int z = rng.NextInt(minZ + 1, Math.Max(minZ + 2, maxZ));

                if (!IsVoxelInside(polygon, x, z))
                    continue;

                world.SetVoxelGlobal(x, Math.Min(5, topY - 1), z, VoxelMaterial.MedCache);
                break;
            }
        }

        private static Rect2 ComputeBounds(Vector2[] polygon)
        {
            float minX = polygon[0].X;
            float maxX = polygon[0].X;
            float minY = polygon[0].Y;
            float maxY = polygon[0].Y;

            foreach (var p in polygon)
            {
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }

            return new Rect2(
                new Vector2(minX, minY),
                new Vector2(maxX - minX, maxY - minY)
            );
        }

        private static bool IsVoxelInside(Vector2[] polygon, int voxelX, int voxelZ)
        {
            var point = new Vector2(
                (voxelX + 0.5f) * VoxelChunk.VoxelScale,
                (voxelZ + 0.5f) * VoxelChunk.VoxelScale
            );

            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];

                bool intersects =
                    ((a.Y > point.Y) != (b.Y > point.Y)) &&
                    (point.X < (b.X - a.X) * (point.Y - a.Y) /
                        ((b.Y - a.Y) == 0 ? float.Epsilon : (b.Y - a.Y)) + a.X);

                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        private static VoxelMaterial ResolveWallMaterial(string buildingType, string amenityTag)
        {
            if (amenityTag == "pharmacy" || buildingType == "retail")
                return VoxelMaterial.Brick;

            if (buildingType == "industrial")
                return VoxelMaterial.SteelBarricade;

            return VoxelMaterial.Concrete;
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

                return minInclusive + (int)(Next() % (uint)(maxExclusive - minInclusive));
            }
        }
    }
}
