using System;
using Godot;
using TerreZero.Gameplay;

namespace TerreZero.World
{
    public partial class LootContainerActor : Area3D
    {
        public event Action<LootContainerActor, LootBundle> Looted;

        [Export] public string SourceId { get; set; } = "loot";
        [Export] public string Context { get; set; } = "urban";
        [Export] public int Seed { get; set; } = 1;

        private Label3D _label;
        private bool _opened;

        public override void _Ready()
        {
            EnsureVisuals();
            _label = GetNode<Label3D>("Label3D");
            BodyEntered += OnBodyEntered;

            if (ExplorationState.IsLootOpened(SourceId))
            {
                _opened = true;
                Monitoring = false;
                _label.Text = "CACHE // VIDE";
                DimOpenedCache();
            }
            else
            {
                _label.Text = "CACHE // NON OUVERTE";
            }
        }

        private void EnsureVisuals()
        {
            if (GetNodeOrNull<CollisionShape3D>("CollisionShape3D") == null)
            {
                AddChild(new CollisionShape3D
                {
                    Name = "CollisionShape3D",
                    Shape = new BoxShape3D
                    {
                        Size = new Vector3(1.2f, 0.8f, 1.0f)
                    }
                });
            }

            if (GetNodeOrNull<MeshInstance3D>("MeshInstance3D") == null)
            {
                var material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.18f, 0.22f, 0.20f, 1f),
                    Metallic = 0.5f,
                    Roughness = 0.65f,
                    EmissionEnabled = true,
                    Emission = new Color(0.18f, 0.10f, 0.03f, 1f),
                    EmissionEnergyMultiplier = 0.45f
                };

                AddChild(new MeshInstance3D
                {
                    Name = "MeshInstance3D",
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(1.2f, 0.8f, 1.0f)
                    },
                    MaterialOverride = material
                });
            }

            if (GetNodeOrNull<Label3D>("Label3D") == null)
            {
                AddChild(new Label3D
                {
                    Name = "Label3D",
                    Position = new Vector3(0f, 0.75f, 0f),
                    Text = "CACHE",
                    FontSize = 24,
                    OutlineSize = 6,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    NoDepthTest = true
                });
            }
        }

        private void DimOpenedCache()
        {
            var mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
            if (mesh?.MaterialOverride is StandardMaterial3D material)
            {
                material.AlbedoColor = new Color(0.10f, 0.12f, 0.11f, 1f);
                material.EmissionEnergyMultiplier = 0.08f;
            }
        }

        private void OnBodyEntered(Node3D body)
        {
            if (_opened || body is not TerreZero.World.Voxel.PlayerController)
                return;

            _opened = true;
            Monitoring = false;

            LootBundle bundle = LootDirector.Generate(
                SourceId,
                Context,
                Seed
            );

            int added = GameplayProgressionService.ApplyLoot(bundle);
            _label.Text = added > 0
                ? $"CACHE RÉCUPÉRÉE // {added} OBJET(S)"
                : "CACHE // INVENTAIRE PLEIN";

            ExplorationState.MarkLootOpened(SourceId);
            GlobalSaveStore.SaveAll();
            Looted?.Invoke(this, bundle);

            DimOpenedCache();

            var tween = CreateTween();
            tween.TweenProperty(this, "scale", Vector3.One * 0.75f, 0.18f);
        }
    }
}