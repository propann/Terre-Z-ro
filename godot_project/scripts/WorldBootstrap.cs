using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Godot;
using TerreZero.Chimeres;
using TerreZero.Bunker;
using TerreZero.Gameplay;
using TerreZero.Network;
using TerreZero.UI;
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

        // Pour le prototype PC, ce point est fourni par le mécanisme de localisation
        // du lanceur / OS. Le joueur ne joue pas obligatoirement exactement ici :
        // il sélectionne un départ à 5 ou 10 km maximum.
        [Export] public double MachineLatitude { get; set; } = 45.0;
        [Export] public double MachineLongitude { get; set; } = 5.0;

        public double AnchorLatitude { get; private set; } = 45.0;
        public double AnchorLongitude { get; private set; } = 5.0;

        private readonly ConcurrentQueue<VoxelDeltaEvent> _remoteDeltas = new();

        private Node3D _container;
        private Node3D _chimereContainer;
        private Node3D _lootContainer;
        private Node3D _player;
        private PlayerController _playerController;
        private StartLocationUI _startUI;
        private GameHUD _hud;
        private ChimereBattleUI _battleUI;
        private ChimereTrainingUI _trainingUI;
        private readonly ChimereEncounterDirector _encounters = new();
        private PackedScene _chimereActorScene;
        private ChimereWorldActor _activeEncounterActor;
        private VoxelWorldGrid _world;
        private string _activeH3;
        private double _streamTimer;
        private bool _startValidated;
        private bool _loadingStart;
        private bool _overlayOpen;

        public override async void _Ready()
        {
            _container = GetNode<Node3D>("VoxelWorldContainer");
            _chimereContainer = GetNode<Node3D>("ChimereWorldContainer");
            _lootContainer = new Node3D { Name = "LootWorldContainer" };
            AddChild(_lootContainer);
            _chimereActorScene = GD.Load<PackedScene>("res://scenes/ChimereWorldActor.tscn");
            _player = GetNode<Node3D>("Player");
            _playerController = _player as PlayerController;
            _startUI = GetNodeOrNull<StartLocationUI>("StartLocationUI");
            _hud = GetNodeOrNull<GameHUD>("GameHUD");
            _battleUI = GetNodeOrNull<ChimereBattleUI>("ChimereBattleUI");
            _trainingUI = GetNodeOrNull<ChimereTrainingUI>("ChimereTrainingUI");
            _activeH3 = DemoH3Index;

            GlobalSaveStore.LoadAll();
            BunkerSaveStore.Load();
            ExplorationState.Load();

            if (_battleUI != null)
            {
                _battleUI.BattleClosed += OnBattleClosed;
                _battleUI.ChimereCaptured += OnChimereCaptured;
            }

            if (_trainingUI != null)
                _trainingUI.Closed += OnTrainingClosed;

            DeltaSyncManager.RemoteDeltaReceived += OnRemoteDeltaReceived;
            if (_playerController != null)
                _playerController.ScannerChanged += OnScannerChanged;

            if (UseRemoteWorldData && _startUI != null)
            {
                _startUI.Visible = true;
                _startUI.ConfigureMachinePoint(MachineLatitude, MachineLongitude);
                _startUI.StartConfirmed += OnStartConfirmed;
                _hud?.Hide();
                _playerController?.SetGameplayEnabled(false);
                return;
            }

            if (_startUI != null)
                _startUI.Hide();

            await LoadActiveCellAsync(allowDemoFallback: true);
            EnterGameplay();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_overlayOpen || _playerController == null || !_playerController.GameplayEnabled)
                return;

            if (Input.IsActionJustPressed("encounter_test"))
            {
                StartEncounter();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (Input.IsActionJustPressed("chimere_training"))
            {
                OpenTraining();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.Keycode == Key.H)
                {
                    bool healed = GameplayProgressionService.TryUseMedGel();
                    _hud?.SetHint(
                        healed
                            ? "SOIN : GEL MÉDICAL UTILISÉ"
                            : "SOIN : AUCUN GEL MÉDICAL"
                    );
                    GlobalSaveStore.SaveAll();
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (key.Keycode == Key.F2)
                {
                    CraftingService.TryCraft(
                        "capture_basic",
                        atBunker: false,
                        out string craftMessage
                    );
                    _hud?.SetHint($"CRAFT : {craftMessage}");
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (key.Keycode == Key.F3)
                {
                    BunkerService.TryBuildRefinery(out string bunkerMessage);
                    _hud?.SetHint($"BUNKER : {bunkerMessage}");
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (key.Keycode == Key.F4)
                {
                    BunkerService.TryExpandStorage(out string storageMessage);
                    _hud?.SetHint($"BUNKER : {storageMessage}");
                    GetViewport().SetInputAsHandled();
                }
            }
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
            if (_playerController != null)
                _playerController.ScannerChanged -= OnScannerChanged;

            if (_startUI != null)
                _startUI.StartConfirmed -= OnStartConfirmed;

            if (_battleUI != null)
            {
                _battleUI.BattleClosed -= OnBattleClosed;
                _battleUI.ChimereCaptured -= OnChimereCaptured;
            }

            if (_trainingUI != null)
                _trainingUI.Closed -= OnTrainingClosed;

            if (_chimereContainer != null)
            {
                foreach (Node child in _chimereContainer.GetChildren())
                {
                    if (child is ChimereWorldActor actor)
                        actor.EncounterRequested -= OnWorldEncounterRequested;
                }
            }

            GlobalSaveStore.SaveAll();
            BunkerSaveStore.Save();
            ExplorationState.Save();
        }

        private async void OnStartConfirmed(double latitude, double longitude, int radiusKm)
        {
            if (_loadingStart || _startUI == null)
                return;

            _loadingStart = true;

            try
            {
                if (!_startUI.PermissionGranted)
                {
                    _startUI.ShowValidationResult(
                        false,
                        "AUTORISATION REQUISE — le point machine sert uniquement à limiter la zone."
                    );
                    return;
                }

                using var client = new WorldDataClient(WorldApiBaseUrl);
                StartLocationResponse start = await client.ValidateStartLocationAsync(
                    MachineLatitude,
                    MachineLongitude,
                    latitude,
                    longitude,
                    radiusKm
                );

                if (!start.Allowed || string.IsNullOrWhiteSpace(start.H3Index))
                {
                    _startUI.ShowValidationResult(
                        false,
                        $"POINT HORS PÉRIMÈTRE — {start.DistanceKm:F1} km / limite {start.RadiusKm:F0} km."
                    );
                    return;
                }

                _activeH3 = start.H3Index;
                AnchorLatitude = start.Latitude;
                AnchorLongitude = start.Longitude;
                _startValidated = true;

                _startUI.ShowValidationResult(
                    true,
                    $"SECTEUR VALIDÉ — {start.DistanceKm:F1} km du point machine. Génération du monde…"
                );

                _world?.UnloadAllChunks();
                await LoadActiveCellAsync(allowDemoFallback: true);

                _hud?.SetSector(_activeH3);
                _startUI.EnterGame();
                EnterGameplay();
            }
            catch (Exception ex)
            {
                _startUI.ShowValidationResult(
                    false,
                    $"CONNEXION AU MONDE IMPOSSIBLE — {ex.Message}"
                );
            }
            finally
            {
                _loadingStart = false;
            }
        }

        private void StartEncounter()
        {
            if (_battleUI == null || _overlayOpen)
                return;

            string context = ResolveEncounterContext();
            int seed = (_activeH3 ?? string.Empty).GetHashCode() ^ System.Environment.TickCount;
            ChimereCombatant wild = _encounters.CreateEncounter(_activeH3, context);
            StartEncounter(wild, null, seed);
        }

        private void StartEncounter(
            ChimereCombatant wild,
            ChimereWorldActor actor,
            int seed)
        {
            if (_battleUI == null || _overlayOpen || wild == null)
                return;

            _activeEncounterActor = actor;
            _overlayOpen = true;
            _playerController?.SetGameplayEnabled(false);
            _hud?.Hide();
            string context = actor?.ContextTag ?? ResolveEncounterContext();
            _battleUI.StartBattle(wild, seed, context);
        }

        private void OnWorldEncounterRequested(
            ChimereWorldActor actor,
            ChimereCombatant wild)
        {
            int seed =
                (_activeH3 ?? string.Empty).GetHashCode() ^
                wild.SpeciesId.GetHashCode() ^
                wild.Level;

            StartEncounter(wild, actor, seed);
        }

        private void OpenTraining()
        {
            if (_trainingUI == null || _overlayOpen)
                return;

            _overlayOpen = true;
            _playerController?.SetGameplayEnabled(false);
            _hud?.Hide();
            _trainingUI.Open();
        }

        private void OnBattleClosed()
        {
            if (_activeEncounterActor != null)
            {
                if (_battleUI != null && _battleUI.EncounterResolved)
                    _activeEncounterActor.Consume();
                else
                    _activeEncounterActor.Reactivate();

                _activeEncounterActor = null;
            }

            _overlayOpen = false;
            GlobalSaveStore.SaveAll();
            EnterGameplay();
        }

        private void OnTrainingClosed()
        {
            _overlayOpen = false;
            GlobalSaveStore.SaveAll();
            EnterGameplay();
        }

        private void OnChimereCaptured(ChimereCombatant chimere)
        {
            _hud?.SetHint(
                $"CAPTURE : {chimere.Name.ToUpperInvariant()}  •  T : DRESSAGE  •  C : RENCONTRE"
            );
        }

        private string ResolveEncounterContext()
        {
            if (string.IsNullOrWhiteSpace(_activeH3))
                return "residential";

            char last = char.ToLowerInvariant(_activeH3[^1]);
            return last switch
            {
                '0' or '1' or '2' or '3' => "industrial railway",
                '4' or '5' or '6' or '7' => "park natural wood",
                _ => "residential urban"
            };
        }

        private void EnterGameplay()
        {
            _hud?.Show();
            _hud?.SetSector(_activeH3);
            _hud?.SetHint("CLIC G : EXTRAIRE • CLIC D : CONSTRUIRE • F : X-RAY • C : CHIMÈRE • T : DRESSAGE • H : SOIN");
            _playerController?.SetGameplayEnabled(true);
        }

        private async Task LoadActiveCellAsync(bool allowDemoFallback)
        {
            ExplorationState.DiscoverCell(_activeH3);
            _world = new VoxelWorldGrid(_container, _activeH3);

            bool generatedRemoteWorld = false;
            bool canUseRemoteWorld = UseRemoteWorldData && _startValidated;

            if (canUseRemoteWorld)
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

            if (canUseRemoteWorld)
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
            SpawnWorldChimeres();
            SpawnLootCaches();
        }

        private void SpawnWorldChimeres()
        {
            if (_chimereContainer == null || _chimereActorScene == null || _player == null)
                return;

            foreach (Node child in _chimereContainer.GetChildren())
                child.QueueFree();

            string context = ResolveEncounterContext();
            Vector3 origin = _player.GlobalPosition;
            Vector3[] offsets =
            {
                new Vector3(8f, 0f, 5f),
                new Vector3(-10f, 0f, 7f),
                new Vector3(5f, 0f, -12f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                ChimereCombatant wild =
                    _encounters.CreateEncounter(_activeH3, $"{context}:{i}");

                var actor = _chimereActorScene.Instantiate<ChimereWorldActor>();
                actor.ContextTag = context;
                actor.EncounterSeed = i + 1;
                actor.Position = origin + offsets[i];
                actor.EncounterRequested += OnWorldEncounterRequested;

                _chimereContainer.AddChild(actor);
                actor.Configure(wild);
            }
        }

        private void SpawnLootCaches()
        {
            if (_lootContainer == null || _player == null)
                return;

            foreach (Node child in _lootContainer.GetChildren())
                child.QueueFree();

            string context = ResolveEncounterContext();
            Vector3 origin = _player.GlobalPosition;

            Vector3[] offsets =
            {
                new Vector3(4f, 0f, 9f),
                new Vector3(-7f, 0f, -5f),
                new Vector3(12f, 0f, -2f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var cache = new LootContainerActor
                {
                    Name = $"LootCache_{i + 1}",
                    SourceId = $"{_activeH3}:cache:{i}",
                    Context = context,
                    Seed = (_activeH3 ?? string.Empty).GetHashCode() ^ (i * 7919),
                    Position = origin + offsets[i]
                };

                cache.Looted += OnLootCacheLooted;
                _lootContainer.AddChild(cache);
            }
        }

        private void OnLootCacheLooted(
            LootContainerActor cache,
            LootBundle bundle)
        {
            string summary = bundle.Entries.Count == 0
                ? "CACHE VIDE"
                : string.Join(
                    " • ",
                    bundle.Entries.ConvertAll(entry =>
                    {
                        ItemDefinition item = ItemCatalog.Get(entry.ItemId);
                        return $"{item?.Name ?? entry.ItemId} x{entry.Quantity}";
                    })
                );

            _hud?.SetHint($"LOOT : {summary}");
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

        private void OnScannerChanged(bool active)
        {
            _hud?.SetScanner(active);
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
