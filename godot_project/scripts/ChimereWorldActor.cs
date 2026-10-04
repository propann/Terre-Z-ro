using System;
using Godot;

namespace TerreZero.Chimeres
{
    public partial class ChimereWorldActor : Area3D
    {
        public event Action<ChimereWorldActor, ChimereCombatant> EncounterRequested;

        [Export] public string ContextTag { get; set; } = "residential";
        [Export] public int EncounterSeed { get; set; } = 1;

        public ChimereCombatant Wild { get; private set; }
        private MeshInstance3D _body;
        private MeshInstance3D _core;
        private Label3D _label;
        private float _phase;
        private bool _consumed;

        public override void _Ready()
        {
            _body = GetNode<MeshInstance3D>("Body");
            _core = GetNode<MeshInstance3D>("Core");
            _label = GetNode<Label3D>("Label3D");
            BodyEntered += OnBodyEntered;
            _phase = EncounterSeed * 0.173f;
        }

        public override void _Process(double delta)
        {
            if (_consumed)
                return;

            _phase += (float)delta;
            float bob = Mathf.Sin(_phase * 2.2f) * 0.09f;
            _body.Position = new Vector3(0, 0.75f + bob, 0);
            _core.Position = new Vector3(0, 0.82f + bob, 0);

            RotateY((float)delta * 0.28f);
        }

        public void Configure(ChimereCombatant wild)
        {
            Wild = wild;
            if (!IsNodeReady())
                return;

            _label.Text = $"{wild.Name}\nNIV {wild.Level}";
            Color color = AffinityColor(wild.Affinity);

            _body.MaterialOverride = MakeMaterial(color, 0.85f);
            _core.MaterialOverride = MakeMaterial(color.Lightened(0.35f), 1.6f);
            Scale = ScaleForRole(wild.Role);
        }

        public void Reactivate()
        {
            if (_consumed)
                return;

            Monitoring = true;
            Visible = true;
        }

        public void Consume()
        {
            _consumed = true;
            Monitoring = false;
            Visible = false;
            QueueFree();
        }

        private void OnBodyEntered(Node3D body)
        {
            if (_consumed || Wild == null)
                return;

            if (body is TerreZero.World.Voxel.PlayerController)
            {
                Monitoring = false;
                EncounterRequested?.Invoke(this, Wild);
            }
        }

        private static StandardMaterial3D MakeMaterial(Color color, float emissionEnergy)
        {
            return new StandardMaterial3D
            {
                AlbedoColor = color,
                Metallic = 0.25f,
                Roughness = 0.55f,
                EmissionEnabled = true,
                Emission = color * 0.45f,
                EmissionEnergyMultiplier = emissionEnergy
            };
        }

        private static Vector3 ScaleForRole(ChimereCombatRole role) =>
            role switch
            {
                ChimereCombatRole.Guardian => new Vector3(1.25f, 1.15f, 1.25f),
                ChimereCombatRole.Breaker => new Vector3(1.15f, 1.05f, 1.15f),
                ChimereCombatRole.Capturer => new Vector3(0.88f, 0.95f, 0.88f),
                _ => Vector3.One
            };

        public static Color AffinityColor(ChimereAffinity affinity) =>
            affinity switch
            {
                ChimereAffinity.Organic => new Color("70a86a"),
                ChimereAffinity.Scrap => new Color("9c8060"),
                ChimereAffinity.Electric => new Color("e8d44a"),
                ChimereAffinity.Toxic => new Color("8fcf48"),
                ChimereAffinity.Spectral => new Color("8b77d8"),
                ChimereAffinity.Mineral => new Color("8793a0"),
                ChimereAffinity.Thermal => new Color("df6a3c"),
                ChimereAffinity.Hydro => new Color("54a9d8"),
                ChimereAffinity.Unstable => new Color("d54f9b"),
                ChimereAffinity.Radiant => new Color("e7dda3"),
                _ => Colors.White
            };
    }
}
