using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
        [Export] public float MaxAcceptedGpsAccuracyMeters { get; set; } = 60f;

        private readonly ConcurrentQueue<VoxelDeltaEvent> _remoteDeltas = new();

        private Node3D _container;
        private Node3D _player;
        private VoxelWorldGrid _world;
        private string _activeH3;
        private double _streamTimer;
        private bool _switchingCell;

        public override async void _Ready()
        {
            _container = GetNode<Node3D>("VoxelWorldContainer");
            _player = GetNode<Node3D>("Player");
            _activeH3 = DemoH3Index;

            DeltaSyncManager.RemoteDeltaReceived += OnRemoteDeltaReceived;
            GeoLocationBridge.PositionChanged += OnGpsPositionChanged;

            if (UseRemoteWorldData)
                await ResolveInitialCellAsync();

            await LoadActiveCellAsync(allowDemoFallback: true);
        }

        public override void _Process(double delta)
        {
            while (_remoteDeltas.TryDequeue(out var remote))
                ApplyDelta(remote, rebuildImmediately: true);

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
            GeoLocationBridge.PositionChanged -= OnGpsPositionChanged;
        }

        private async Task ResolveInitialCellAsync()
        {
            try
            {
                using var client = new WorldDataClient(WorldApiBaseUrl);
                SpatialCellPayload resolved = await client.ResolveSpatialCellAsync(
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
                    $"[TERRE ZÉRO] GPS→H3 indisponible : {ex.Message}. Fallback {_activeH3}"
                );
            }
        }

        private async void OnGpsPositionChanged(GeoPositionSample sample)
        {
            if (!UseRemoteWorldData || _switchingCell)
                return;

            if (sample.AccuracyMeters > 0 &&
                sample.AccuracyMeters > MaxAcceptedGpsAccuracyMeters)
                return;

            _switchingCell = true;
            try
            {
                using var client = new WorldDataClient(WorldApiBaseUrl);
                SpatialCellPayload resolved = await client.ResolveSpatialCellAsync(
                    sample.Latitude,
                    sample.Longitude
                );

                if (string.IsNullOrWhiteSpace(resolved.H3Index))
                    return;

                AnchorLatitude = sample.Latitude;
                AnchorLongitude = sample.Longitude;

                if (resolved.H3Index == _activeH3)
                    return;

                string previous = _activeH3;
                _activeH3 = resolved.H3Index;

                GD.Print(
                    $"[TERRE ZÉRO] changement cellule {previous} → {_activeH3}"
                );

                _world?.UnloadAllChunks();
                await LoadActiveCellAsync(allowDemoFallback: false);
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[TERRE ZÉRO] changement GPS/H3 impossible : {ex.Message}");
            }
            finally
            {
                _switchingCell = false;
            }
        }

        private async Task LoadActiveCellAsync(bool allowDemoFallback)
        {
            _world = new VoxelWorldGrid(_container, _activeH3);

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
                        $"[TERRE ZÉRO] données monde indisponibles : {ex.Message}"
                    );
                }
            }

            if (!generatedRemoteWorld && allowDemoFallback)
                GenerateDemoWorld();

            if (UseRemoteWorldData)
            {
                try
                {
                    await ReplayPersistentDeltasAsync();
                }
                catch (Exception ex)
                {
                    GD.PushWarning(
                        $"[TERRE ZÉRO] replay des deltas impossible : {ex.Message}"
                    );
                }

                await DeltaSyncManager.SubscribeAsync(_activeH3);
            }

            _world.UpdateVisibility(_player.GlobalPosition, StreamRadiusChunks);
        }

        private async Task<bool> GenerateRemoteWorldAsync()
        {
            using var client = new WorldDataClient(WorldApiBaseUrl);
            WorldCellPayload cell = await client.GetCellAsync(_activeH3);

            var anchor = new GeoAnchor(AnchorLatitude, AnchorLongitude);
            int generatedBuildings = 0;
            int generatedRoads = 0;

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
                    generatedRoads++;
                }
            }

            if (cell.Buildings != null)
            {
                foreach (var building in cell.Buildings)
                {
                    if (building.Geometry == null)
                        continue;

                    if (building.WorldVersion != OSMVoxelizer.WorldVersion ||
                        building.GeneratorVersion != OSMVoxelizer.GeneratorVersion)
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

                    generatedBuildings++;

                    GD.Print(
                        $"[TERRE ZÉRO] OSM {building.OSMID} " +
                        $"étages={result.Floors} pièces={result.Rooms} seed={result.Seed}"
                    );
                }
            }

            if (generatedBuildings == 0 && generatedRoads == 0)
                return false;

            _world.RebuildDirtyChunks();

            GD.Print(
                $"[TERRE ZÉRO] cellule {_activeH3} : " +
                $"{generatedBuildings} bâtiments, {generatedRoads} routes, " +
                $"{_world.LoadedChunkCount} chunks"
            );

            return true;
        }

        private async Task ReplayPersistentDeltasAsync()
        {
            using var client = new WorldDataClient(WorldApiBaseUrl);
            CellDeltaPayload history = await client.GetDeltasAsync(_activeH3);

            int applied = 0;
            foreach (var delta in history.Deltas)
            {
                if (delta != null && ApplyDelta(delta, rebuildImmediately: false))
                    applied++;
            }

            if (applied > 0)
                _world.RebuildDirtyChunks();

            GD.Print(
                $"[TERRE ZÉRO] deltas persistants h3={_activeH3} count={applied}"
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
