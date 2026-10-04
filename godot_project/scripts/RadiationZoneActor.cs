using Godot;
using TerreZero.Gameplay;

namespace TerreZero.World
{
    public partial class RadiationZoneActor : Area3D
    {
        [Export] public float RadiusMeters { get; set; } = 3.5f;
        [Export] public int RadiationPerSecond { get; set; } = 3;

        private bool _playerInside;
        private float _tick;

        public override void _Ready()
        {
            EnsureVisuals();
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
        }

        public override void _Process(double delta)
        {
            if (!_playerInside)
                return;

            _tick += (float)delta;
            if (_tick < 1f)
                return;

            _tick -= 1f;
            GameState.Player.Radiation = Mathf.Clamp(
                GameState.Player.Radiation + RadiationPerSecond,
                0,
                100
            );

            if (GameState.Player.Radiation % 15 == 0)
                GlobalSaveStore.SaveAll();
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is TerreZero.World.Voxel.PlayerController)
                _playerInside = true;
        }

        private void OnBodyExited(Node3D body)
        {
            if (body is TerreZero.World.Voxel.PlayerController)
                _playerInside = false;
        }

        private void EnsureVisuals()
        {
            if (GetNodeOrNull<CollisionShape3D>("CollisionShape3D") == null)
            {
                AddChild(new CollisionShape3D
                {
                    Name = "CollisionShape3D",
                    Shape = new SphereShape3D
                    {
                        Radius = RadiusMeters
                    }
                });
            }

            if (GetNodeOrNull<MeshInstance3D>("ZoneMesh") == null)
            {
                var material = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = new Color(0.45f, 0.85f, 0.18f, 0.10f),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    EmissionEnabled = true,
                    Emission = new Color(0.35f, 0.95f, 0.12f, 1f),
                    EmissionEnergyMultiplier = 0.55f
                };

                AddChild(new MeshInstance3D
                {
                    Name = "ZoneMesh",
                    Mesh = new SphereMesh
                    {
                        Radius = RadiusMeters,
                        Height = RadiusMeters * 2f
                    },
                    MaterialOverride = material
                });
            }

            if (GetNodeOrNull<Label3D>("Label3D") == null)
            {
                AddChild(new Label3D
                {
                    Name = "Label3D",
                    Position = new Vector3(0f, RadiusMeters + 0.75f, 0f),
                    Text = "ZONE CONTAMINÉE",
                    FontSize = 24,
                    OutlineSize = 7,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    NoDepthTest = true,
                    Modulate = new Color(0.72f, 1f, 0.45f, 1f)
                });
            }
        }
    }
}
