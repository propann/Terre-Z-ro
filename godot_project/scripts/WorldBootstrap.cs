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
        [Export] public string DemoAmenity { get; set; } = "pharmacy";

        private readonly ConcurrentQueue<VoxelDeltaEvent> _remoteDeltas = new();
        private VoxelChunk _demoChunk;

        public override void _Ready()
        {
            var container = GetNode<Node3D>("VoxelWorldContainer");
            var packedChunk = GD.Load<PackedScene>("res://scenes/VoxelChunk.tscn");
            _demoChunk = packedChunk.Instantiate<VoxelChunk>();

            _demoChunk.Name = "DemoChunk_0_0_0";
            _demoChunk.H3Index = DemoH3Index;
            _demoChunk.ChunkCoord = Vector3I.Zero;
            container.AddChild(_demoChunk);

            OSMVoxelizer.VoxelizeBuilding(
                _demoChunk,
                new Rect2(new Vector2(0.8f, 0.8f), new Vector2(4.8f, 4.8f)),
                5.8f,
                DemoAmenity,
                DemoOsmId
            );
            _demoChunk.RebuildMeshGreedy();

            DeltaSyncManager.RemoteDeltaReceived += OnRemoteDeltaReceived;
            GD.Print($"[TERRE ZÉRO] zone prête h3={DemoH3Index} osm={DemoOsmId}");
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
            if (_demoChunk == null || delta.H3Index != _demoChunk.H3Index)
                return;

            if (delta.ChunkCoords == null || delta.ChunkCoords.Length != 3 ||
                delta.LocalVoxel == null || delta.LocalVoxel.Length != 3)
                return;

            var chunkCoord = new Vector3I(delta.ChunkCoords[0], delta.ChunkCoords[1], delta.ChunkCoords[2]);
            if (chunkCoord != _demoChunk.ChunkCoord)
                return;

            var material = delta.Action == "DESTROY"
                ? VoxelMaterial.Air
                : (VoxelMaterial)delta.MaterialId;

            _demoChunk.SetVoxel(
                delta.LocalVoxel[0],
                delta.LocalVoxel[1],
                delta.LocalVoxel[2],
                material,
                trackEdit: false
            );
            _demoChunk.RebuildMeshGreedy();
        }
    }
}
