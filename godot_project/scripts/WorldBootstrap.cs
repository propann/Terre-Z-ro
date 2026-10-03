using System.Collections.Concurrent;
using Godot;
using TerreZero.Network;
using TerreZero.World.Generation;
using TerreZero.World.Voxel;

namespace TerreZero.World
{
    public partial class WorldBootstrap : Node3D
    {
        [Export] public string DemoH3Index { get; set; } = "891fb466257ffff";
        [Export] public long DemoOsmId { get; set; } = 100001;
        [Export] public string DemoBuildingType { get; set; } = "commercial";
        [Export] public string DemoAmenity { get; set; } = "pharmacy";

        private readonly ConcurrentQueue<VoxelDeltaEvent> _remoteDeltas = new();
        private VoxelWorldGrid _world;

        public override void _Ready()
        {
            var container = GetNode<Node3D>("VoxelWorldContainer");
            _world = new VoxelWorldGrid(container, DemoH3Index);

            // Empreinte volontairement supérieure à un chunk de 6,4 m :
            // 20 x 30 m, 18 m de haut. Cela valide le découpage X/Y/Z.
            var generation = OSMVoxelizer.VoxelizeBuilding(
                _world,
                new Rect2(new Vector2(0.0f, 0.0f), new Vector2(20.0f, 30.0f)),
                18.0f,
                DemoBuildingType,
                DemoAmenity,
                DemoOsmId
            );

            DeltaSyncManager.RemoteDeltaReceived += OnRemoteDeltaReceived;

            GD.Print(
                $"[TERRE ZÉRO] zone prête h3={DemoH3Index} " +
                $"osm={DemoOsmId} seed={generation.Seed} " +
                $"étages={generation.Floors} pièces={generation.Rooms} " +
                $"chunks={generation.TouchedChunks}"
            );
        }

        public override void _Process(double delta)
        {
            while (_remoteDeltas.TryDequeue(out var remote))
                ApplyRemoteDelta(remote);
        }

        public override void _ExitTree()
        {
            DeltaSyncManager.RemoteDeltaReceived -= OnRemoteDeltaReceived;
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

            _world.ApplyDelta(chunkCoord, localVoxel, material);
        }
    }
}
