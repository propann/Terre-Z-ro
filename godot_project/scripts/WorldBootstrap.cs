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
        private string _activeH3;
        private double _streamTimer;

        public override async void _Ready()
        {
            var container = GetNode<Node3D>("VoxelWorldContainer");
            _player = GetNode<Node3D>("Player");
            _activeH3 = DemoH3Index;

            if (UseRemoteWorldData)
            {
                try
                {
                    using var spatialClient = new WorldDataClient(WorldApiBaseUrl);
                    SpatialCellPayload resolved = await spatialClient.ResolveSpatialCellAsync(
                        AnchorLatitude,
                        AnchorLongitude
                    );

                    if (!string.IsNullOrWhiteSpace(resolved.H3Index))
                        _activeH3 = resolved.H3Index;

                    GD.Print(
                        $"[TERRE ZÉRO] GPS {AnchorLatitude:F6},{AnchorLongitude:F6} → H3 {_activeH3}"
                    );
                }
                catch (Exception ex)
                {
                    GD.PushWarning(
                        $"[TERRE ZÉRO] GPS→H3 indisponible : {ex.Message}. " +
                        $"Fallback {_activeH3}"
                    );
                }
            }

            _world = new VoxelWorldGrid(container, _activeH3);

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
            WorldCellPayload cell = await client.GetCellAsync(_activeH3);

            if (cell.Buildings == null || cell.Buildings.Count == 0)
                return false;

            var anchor = new GeoAnchor(AnchorLatitude, AnchorLongitude);
            int generated = 0;

            if (cell.Roads != null)
            {
                foreach (var road in cell.Roads)
                {
                    if (road.Geometry == null)
                        continue;

                    Vector2[] line = road.Geometry.ProjectLineString(anchor);
                    if (line.Length < 2)
                        continue;

                    OSMRoadVoxelizer.VoxelizeRoad(
                        _world,
                        line,
                        road.HighwayType,
                        road.Surface,
                        road.Lanes
                    );
                }
            }

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

            _world.RebuildDirtyChunks();

            GD.Print(
                $"[TERRE ZÉRO] cellule réelle {_activeH3} : " +
                $"{generated} bâtiments, {cell.Roads?.Count ?? 0} routes, " +
                $"{_world.LoadedChunkCount} chunks"
            );

            return true;
        }

        private async System.Threading.Tasks.Task ReplayPersistentDeltasAsync()
        {
            using var client = new WorldDataClient(WorldApiBaseUrl);
            CellDeltaPayload history = await client.GetDeltasAsync(_activeH3);

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
                $"[TERRE ZÉRO] deltas persistants rejoués h3={_activeH3} count={applied}"
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
                $"[TERRE ZÉRO] démo h3={_activeH3} " +
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
            if (_world == null || delta.H3Index != _activeH3)
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
            if (_world == null || delta.H3Index != _activeH3)
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
