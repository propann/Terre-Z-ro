using Godot;
using TerreZero.World.Generation;
using TerreZero.World.Voxel;

namespace TerreZero.World
{
    public partial class WorldBootstrap : Node3D
    {
        [Export] public string DemoH3Index { get; set; } = "891fb466257ffff";
        [Export] public long DemoOsmId { get; set; } = 100001;
        [Export] public string DemoAmenity { get; set; } = "pharmacy";

        public override void _Ready()
        {
            var container = GetNode<Node3D>("VoxelWorldContainer");
            var packedChunk = GD.Load<PackedScene>("res://scenes/VoxelChunk.tscn");
            var chunk = packedChunk.Instantiate<VoxelChunk>();

            chunk.Name = "DemoChunk_0_0_0";
            chunk.H3Index = DemoH3Index;
            chunk.ChunkCoord = Vector3I.Zero;
            container.AddChild(chunk);

            // Première zone verticale : une empreinte déterministe dans un chunk 6,4 m.
            OSMVoxelizer.VoxelizeBuilding(
                chunk,
                new Rect2(new Vector2(0.8f, 0.8f), new Vector2(4.8f, 4.8f)),
                5.8f,
                DemoAmenity,
                DemoOsmId
            );
            chunk.RebuildMeshGreedy();

            GD.Print($"[TERRE ZÉRO] zone prête h3={DemoH3Index} osm={DemoOsmId}");
        }
    }
}
