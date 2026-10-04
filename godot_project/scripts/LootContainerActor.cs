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
            _label = GetNode<Label3D>("Label3D");
            BodyEntered += OnBodyEntered;
            _label.Text = "CACHE // NON OUVERTE";
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

            GlobalSaveStore.SaveAll();
            Looted?.Invoke(this, bundle);

            var tween = CreateTween();
            tween.TweenProperty(this, "scale", Vector3.One * 0.75f, 0.18f);
            tween.TweenProperty(this, "modulate:a", 0.25f, 0.25f);
        }
    }
}