using System;
using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.World.Generation
{
    public static class OSMRoadVoxelizer
    {
        public static void VoxelizeRoad(
            VoxelWorldGrid world,
            Vector2[] polylineMeters,
            string highwayType,
            string surface,
            int lanes)
        {
            if (polylineMeters == null || polylineMeters.Length < 2)
                return;

            float widthMeters = ResolveWidthMeters(highwayType, lanes);
            float radiusVoxels = Math.Max(
                1f,
                widthMeters * 0.5f / VoxelChunk.VoxelScale
            );

            VoxelMaterial material = ResolveSurfaceMaterial(surface);

            for (int i = 0; i < polylineMeters.Length - 1; i++)
                RasterizeSegment(
                    world,
                    polylineMeters[i],
                    polylineMeters[i + 1],
                    radiusVoxels,
                    material
                );
        }

        private static void RasterizeSegment(
            VoxelWorldGrid world,
            Vector2 startMeters,
            Vector2 endMeters,
            float radiusVoxels,
            VoxelMaterial material)
        {
            Vector2 start = startMeters / VoxelChunk.VoxelScale;
            Vector2 end = endMeters / VoxelChunk.VoxelScale;
            Vector2 delta = end - start;

            int steps = Math.Max(
                1,
                Mathf.CeilToInt(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)))
            );

            float radiusSq = radiusVoxels * radiusVoxels;
            int integerRadius = Mathf.CeilToInt(radiusVoxels);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = start.Lerp(end, t);
                int cx = Mathf.RoundToInt(point.X);
                int cz = Mathf.RoundToInt(point.Y);

                for (int x = cx - integerRadius; x <= cx + integerRadius; x++)
                for (int z = cz - integerRadius; z <= cz + integerRadius; z++)
                {
                    float dx = x - point.X;
                    float dz = z - point.Y;
                    if ((dx * dx) + (dz * dz) > radiusSq)
                        continue;

                    world.SetVoxelGlobal(x, 0, z, material);
                }
            }
        }

        private static float ResolveWidthMeters(string highwayType, int lanes)
        {
            int safeLanes = Math.Max(1, lanes);

            return highwayType switch
            {
                "motorway" => Math.Max(7.0f, safeLanes * 3.5f),
                "trunk" => Math.Max(7.0f, safeLanes * 3.5f),
                "primary" => Math.Max(6.5f, safeLanes * 3.25f),
                "secondary" => Math.Max(6.0f, safeLanes * 3.0f),
                "tertiary" => Math.Max(5.5f, safeLanes * 2.75f),
                "residential" => Math.Max(4.5f, safeLanes * 2.5f),
                "service" => Math.Max(3.0f, safeLanes * 2.25f),
                "footway" => 1.8f,
                "path" => 1.4f,
                "cycleway" => 2.0f,
                _ => Math.Max(4.0f, safeLanes * 2.5f)
            };
        }

        private static VoxelMaterial ResolveSurfaceMaterial(string surface)
        {
            return surface switch
            {
                "grass" => VoxelMaterial.GrassOrganic,
                "paving_stones" => VoxelMaterial.Sidewalk,
                "concrete" => VoxelMaterial.Concrete,
                _ => VoxelMaterial.Asphalt
            };
        }
    }
}
