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
        private Node3D _accentRoot;
        private float _phase;
        private bool _consumed;

        public override void _Ready()
        {
            _body = GetNode<MeshInstance3D>("Body");
            _core = GetNode<MeshInstance3D>("Core");
            _label = GetNode<Label3D>("Label3D");

            _accentRoot = new Node3D { Name = "GeneratedAccents" };
            AddChild(_accentRoot);

            BodyEntered += OnBodyEntered;
            _phase = EncounterSeed * 0.173f;

            if (Wild != null)
                ApplyVisuals();
        }

        public override void _Process(double delta)
        {
            if (_consumed)
                return;

            _phase += (float)delta;
            float bob = Mathf.Sin(_phase * 2.2f) * 0.09f;
            _body.Position = new Vector3(0, 0.75f + bob, 0);
            _core.Position = new Vector3(0, 0.82f + bob, 0);
            _accentRoot.Position = new Vector3(0, bob, 0);

            float rotationSpeed = Wild?.Rarity == ChimereRarity.Alpha ? 0.18f : 0.28f;
            RotateY((float)delta * rotationSpeed);
        }

        public void Configure(ChimereCombatant wild)
        {
            Wild = wild;
            if (IsNodeReady())
                ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (Wild == null)
                return;

            _label.Text =
                Wild.Rarity == ChimereRarity.Common
                    ? $"{Wild.Name}\nNIV {Wild.Level}"
                    : $"{Wild.Name}\n{Wild.Rarity.ToString().ToUpperInvariant()} • NIV {Wild.Level}";

            Color color = AffinityColor(Wild.Affinity);
            ChimereVisualProfile profile = ChimereVisualCatalog.Get(Wild.SpeciesId);

            _body.Mesh = CreateBodyMesh(profile.Shape);
            _body.Scale = profile.BodyScale;
            _body.MaterialOverride = MakeMaterial(color, 0.85f);

            _core.Scale = Vector3.One * profile.CoreScale;
            _core.MaterialOverride = MakeMaterial(
                Wild.Rarity == ChimereRarity.Alpha
                    ? Colors.White
                    : color.Lightened(0.35f),
                Wild.Rarity == ChimereRarity.Alpha ? 2.8f : 1.6f
            );

            ClearAccents();
            BuildAccents(profile, color);

            float rarityScale = Wild.Rarity switch
            {
                ChimereRarity.Rare => 1.10f,
                ChimereRarity.Alpha => 1.35f,
                _ => 1.0f
            };

            Scale = ScaleForRole(Wild.Role) * rarityScale;
        }

        private void ClearAccents()
        {
            foreach (Node child in _accentRoot.GetChildren())
                child.QueueFree();
        }

        private void BuildAccents(
            ChimereVisualProfile profile,
            Color color)
        {
            StandardMaterial3D accentMaterial =
                MakeMaterial(color.Lightened(0.18f), 1.2f);

            switch (profile.Shape)
            {
                case ChimereBodyShape.Beast:
                    AddHorn(new Vector3(-0.42f, 1.25f, -0.10f), new Vector3(0.15f, 0.35f, 0.15f), accentMaterial);
                    AddHorn(new Vector3(0.42f, 1.25f, -0.10f), new Vector3(0.15f, 0.35f, 0.15f), accentMaterial);
                    AddBox(new Vector3(0, 0.65f, -0.72f), new Vector3(0.55f, 0.24f, 0.55f), accentMaterial);
                    break;

                case ChimereBodyShape.Wisp:
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * Mathf.Tau / 4f;
                        AddSphere(
                            new Vector3(Mathf.Cos(angle) * 0.55f, 0.85f + i * 0.08f, Mathf.Sin(angle) * 0.55f),
                            0.12f,
                            accentMaterial
                        );
                    }
                    break;

                case ChimereBodyShape.Stag:
                    AddHorn(new Vector3(-0.28f, 1.55f, 0), new Vector3(0.13f, 0.60f, 0.13f), accentMaterial);
                    AddHorn(new Vector3(0.28f, 1.55f, 0), new Vector3(0.13f, 0.60f, 0.13f), accentMaterial);
                    AddHorn(new Vector3(-0.50f, 1.80f, 0), new Vector3(0.10f, 0.35f, 0.10f), accentMaterial);
                    AddHorn(new Vector3(0.50f, 1.80f, 0), new Vector3(0.10f, 0.35f, 0.10f), accentMaterial);
                    break;

                case ChimereBodyShape.Raptor:
                    AddBox(new Vector3(-0.55f, 0.85f, 0), new Vector3(0.50f, 0.16f, 0.12f), accentMaterial);
                    AddBox(new Vector3(0.55f, 0.85f, 0), new Vector3(0.50f, 0.16f, 0.12f), accentMaterial);
                    AddHorn(new Vector3(0, 0.72f, 0.78f), new Vector3(0.12f, 0.45f, 0.12f), accentMaterial);
                    break;

                case ChimereBodyShape.Serpent:
                    AddSphere(new Vector3(0, 1.25f, 0), 0.28f, accentMaterial);
                    AddSphere(new Vector3(0, 0.35f, 0), 0.22f, accentMaterial);
                    AddHorn(new Vector3(0, 1.65f, 0), new Vector3(0.11f, 0.38f, 0.11f), accentMaterial);
                    break;

                case ChimereBodyShape.Furnace:
                    AddBox(new Vector3(-0.45f, 0.55f, 0), new Vector3(0.20f, 0.75f, 0.20f), accentMaterial);
                    AddBox(new Vector3(0.45f, 0.55f, 0), new Vector3(0.20f, 0.75f, 0.20f), accentMaterial);
                    AddHorn(new Vector3(0, 1.45f, 0), new Vector3(0.20f, 0.42f, 0.20f), accentMaterial);
                    break;

                case ChimereBodyShape.Fungal:
                    AddSphere(new Vector3(0, 1.25f, 0), 0.55f, accentMaterial);
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * Mathf.Tau / 5f;
                        AddSphere(
                            new Vector3(Mathf.Cos(angle) * 0.62f, 0.65f, Mathf.Sin(angle) * 0.62f),
                            0.14f,
                            accentMaterial
                        );
                    }
                    break;

                case ChimereBodyShape.Prism:
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * Mathf.Tau / 5f;
                        AddHorn(
                            new Vector3(Mathf.Cos(angle) * 0.42f, 1.25f, Mathf.Sin(angle) * 0.42f),
                            new Vector3(0.10f, 0.55f, 0.10f),
                            accentMaterial
                        );
                    }
                    break;

                case ChimereBodyShape.Golem:
                    AddBox(new Vector3(-0.62f, 0.72f, 0), new Vector3(0.42f, 0.70f, 0.52f), accentMaterial);
                    AddBox(new Vector3(0.62f, 0.72f, 0), new Vector3(0.42f, 0.70f, 0.52f), accentMaterial);
                    AddBox(new Vector3(0, 1.45f, 0), new Vector3(0.55f, 0.38f, 0.45f), accentMaterial);
                    break;
            }
        }

        private void AddSphere(
            Vector3 position,
            float radius,
            Material material)
        {
            var mesh = new SphereMesh
            {
                Radius = radius,
                Height = radius * 2f
            };

            AddAccent(mesh, position, Vector3.One, material);
        }

        private void AddBox(
            Vector3 position,
            Vector3 size,
            Material material)
        {
            var mesh = new BoxMesh { Size = size };
            AddAccent(mesh, position, Vector3.One, material);
        }

        private void AddHorn(
            Vector3 position,
            Vector3 size,
            Material material)
        {
            var mesh = new CylinderMesh
            {
                TopRadius = 0.02f,
                BottomRadius = size.X,
                Height = size.Y
            };

            AddAccent(mesh, position, Vector3.One, material);
        }

        private void AddAccent(
            Mesh mesh,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var instance = new MeshInstance3D
            {
                Mesh = mesh,
                Position = position,
                Scale = scale,
                MaterialOverride = material
            };

            _accentRoot.AddChild(instance);
        }

        private static Mesh CreateBodyMesh(ChimereBodyShape shape) =>
            shape switch
            {
                ChimereBodyShape.Beast => new CapsuleMesh { Radius = 0.46f, Height = 1.25f },
                ChimereBodyShape.Wisp => new SphereMesh { Radius = 0.46f, Height = 0.92f },
                ChimereBodyShape.Stag => new CapsuleMesh { Radius = 0.38f, Height = 1.55f },
                ChimereBodyShape.Raptor => new CapsuleMesh { Radius = 0.34f, Height = 1.20f },
                ChimereBodyShape.Serpent => new CapsuleMesh { Radius = 0.30f, Height = 1.75f },
                ChimereBodyShape.Furnace => new CylinderMesh { TopRadius = 0.38f, BottomRadius = 0.52f, Height = 1.25f },
                ChimereBodyShape.Fungal => new CylinderMesh { TopRadius = 0.30f, BottomRadius = 0.45f, Height = 1.10f },
                ChimereBodyShape.Prism => new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.48f, Height = 1.35f },
                ChimereBodyShape.Golem => new BoxMesh { Size = new Vector3(0.95f, 1.15f, 0.78f) },
                _ => new CapsuleMesh { Radius = 0.42f, Height = 1.20f }
            };

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

        private static StandardMaterial3D MakeMaterial(
            Color color,
            float emissionEnergy)
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
