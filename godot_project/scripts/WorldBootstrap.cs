using System;
using System.Collections.Concurrent;
using Godot;
using TerreZero.Network;
using TerreZero.World.Generation;
using TerreZero.World.Geo;
using TerreZero.World.Voxel;

namespace TerreZero.World
{
    public partial class WorldBootstrap : Node3D
    {
        [Export] public string DemoH3Index { get; set; } = "891fb466257ffff";
        [Export] public long DemoOsmId { get; set; } = 100001;
        [Export] public string DemoBuildingType { get; set; } = "commercial";
        [Export] public string DemoAmenity { get; set; } = "pharmacy";
        [Export] public int StreamRadiusChunks { get; set; } = 3;

        [Export] public bool UseRemoteWorldData { get; set; } = false;
        [Export] public string WorldApiBaseUrl { get; set; } = "http://127.0.0.1:8080";
        [Export] public double AnchorLatitude { get; set; } = 45.0;
        [Export] public double AnchorLongitude { get; set; } = 5.0;

        private readonly ConcurrentQueue<VoxelDeltaEvent> _remoteDeltas = new();
        private VoxelWorldGrid _world;
        private Node3D _player;
        private double _streamTimer;

        public override async void _Ready()
        {
            var container = GetNode<Node3D>("VoxelWorldContainer");
            _player = GetNode<Node3D>("Player");
            _world = new VoxelWorldGrid(container, DemoH3Index);

            bool generatedRemoteWorld = false;

            if (UseRemoteWorldData)
            {
                try
                {
                    generatedRemoteWorld = await GenerateRemoteWorldAsync();
                }
                catch (Exception ex)
                {
                    GD.PushWarning(
                        $"[TERRE ZÉRO] données monde indisponibles : {ex.Message}. " +
                        "Utilisation de la zone de démonstration."
                    );
                }
            }

            if (!generatedRemoteWorld)
                GenerateDemoWorld();

            if (UseRemoteWorldData)
            {
                try
                {
                    await ReplayPersistentDeltasAsync();
                }
                catch (Exception ex)
                {
                    GD.PushWarning($"[TERRE ZÉRO] replay des deltas impossible : {ex.Message}");
                }
            }

            _world.UpdateVisibility(_player.GlobalPosition, StreamRadiusChunks);
            DeltaSyncManager.RemoteDeltaReceived += OnRemoteDeltaReceived;
        }

        public override void _Process(double delta)
        {
            while (_remoteDeltas.TryDequeue(out var remote))
                ApplyRemoteDelta(remote);

            _streamTimer += delta;
            if (_streamTimer >= 0.25 && _world != null && _player != null)
            {
                _streamTimer = 0;
                _world.UpdateVisibility(_player.GlobalPosition, StreamRadiusChunks);
            }
        }

        public override void _ExitTree()
        {
            DeltaSyncManager.RemoteDeltaReceived -= OnRemoteDeltaReceived;
        }

        private async System.Threading.Tasks.Task<bool> GenerateRemoteWorldAsync()
        {
            using var client = new WorldDataClient(WorldApiBaseUrl);
            WorldCellPayload cell = await client.GetCellAsync(DemoH3Index);

            if (cell.Buildings == null || cell.Buildings.Count == 0)
                return false;

            var anchor = new GeoAnchor(AnchorLatitude, AnchorLongitude);
            int generated = 0;

            foreach (var building in cell.Buildings)
            {
                if (building.Geometry == null)
                    continue;

                Vector2[] polygon = building.Geometry.ProjectOuterRing(anchor);
                if (polygon.Length < 3)
                    continue;

                float height = building.HeightMeters > 0
                    ? building.HeightMeters
                    : Math.Max(3f, building.Levels * 3f);

                var result = OSMPolygonVoxelizer.VoxelizeBuilding(
                    _world,
                    polygon,
                    height,
                    building.BuildingType,
                    building.Amenity,
                    building.OSMID
                );

                generated++;

                GD.Print(
                    $"[TERRE ZÉRO] OSM {building.OSMID} " +
                    $"étages={result.Floors} pièces={result.Rooms} " +
                    $"seed={result.Seed}"
                );
            }

            if (generated == 0)
                return false;

            GD.Print(
                $"[TERRE ZÉRO] cellule réelle {DemoH3Index} : " +
                $"{generated} bâtiments, {_world.LoadedChunkCount} chunks"
            );

            return true;
        }

        private async System.Threading.Tasks.Task ReplayPersistentDeltasAsync()
        {
            using var client = new WorldDataClient(WorldApiBaseUrl);
            CellDeltaPayload history = await client.GetDeltasAsync(DemoH3Index);

            int applied = 0;
            foreach (var delta in history.Deltas)
            {
                if (delta == null)
                    continue;

                if (ApplyDelta(delta, rebuildImmediately: false))
                    applied++;
            }

            if (applied > 0)
                _world.RebuildDirtyChunks();

            GD.Print(
                $"[TERRE ZÉRO] deltas persistants rejoués h3={DemoH3Index} count={applied}"
            );
        }

        private void GenerateDemoWorld()
        {
            var generation = OSMVoxelizer.VoxelizeBuilding(
                _world,
                new Rect2(
                    new Vector2(0.0f, 0.0f),
                    new Vector2(20.0f, 30.0f)
                ),
                18.0f,
                DemoBuildingType,
                DemoAmenity,
                DemoOsmId
            );

            GD.Print(
                $"[TERRE ZÉRO] démo h3={DemoH3Index} " +
                $"osm={DemoOsmId} seed={generation.Seed} " +
                $"étages={generation.Floors} pièces={generation.Rooms} " +
                $"chunks={generation.TouchedChunks}"
            );
        }

        private void OnRemoteDeltaReceived(VoxelDeltaEvent delta)
        {
            _remoteDeltas.Enqueue(delta);
        }

        private void ApplyRemoteDelta(VoxelDeltaEvent delta)
        {
            if (_world == null || delta.H3Index != DemoH3Index)
                return;

            if (delta.ChunkCoords == null || delta.ChunkCoords.Length != 3 ||
                delta.LocalVoxel == null || delta.LocalVoxel.Length != 3)
                return;

            var chunkCoord = new Vector3I(
                delta.ChunkCoords[0],
                delta.ChunkCoords[1],
                delta.ChunkCoords[2]
            );

            var localVoxel = new Vector3I(
                delta.LocalVoxel[0],
                delta.LocalVoxel[1],
                delta.LocalVoxel[2]
            );

            var material = delta.Action == "DESTROY"
                ? VoxelMaterial.Air
                : (VoxelMaterial)delta.MaterialId;

            ApplyDelta(delta, rebuildImmediately: true);
        }

        private bool ApplyDelta(VoxelDeltaEvent delta, bool rebuildImmediately)
        {
            if (_world == null || delta.H3Index != DemoH3Index)
                return false;

            if (delta.WorldVersion != OSMVoxelizer.WorldVersion ||
                delta.GeneratorVersion != OSMVoxelizer.GeneratorVersion)
                return false;

            if (delta.ChunkCoords == null || delta.ChunkCoords.Length != 3 ||
                delta.LocalVoxel == null || delta.LocalVoxel.Length != 3)
                return false;

            var chunkCoord = new Vector3I(
                delta.ChunkCoords[0],
                delta.ChunkCoords[1],
                delta.ChunkCoords[2]
            );

            var localVoxel = new Vector3I(
                delta.LocalVoxel[0],
                delta.LocalVoxel[1],
                delta.LocalVoxel[2]
            );

            var material = delta.Action == "DESTROY"
                ? VoxelMaterial.Air
                : (VoxelMaterial)delta.MaterialId;

            var chunk = _world.GetOrCreateChunk(chunkCoord);
            chunk.SetVoxel(
                localVoxel.X,
                localVoxel.Y,
                localVoxel.Z,
                material,
                trackEdit: false
            );

            if (rebuildImmediately)
                chunk.RebuildMeshGreedy();

            return true;
        }
    }
}
