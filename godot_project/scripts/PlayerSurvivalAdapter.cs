using Godot;
using TerreZero.Gameplay;

namespace TerreZero.World.Voxel
{
    public partial class PlayerSurvivalAdapter : Node
    {
        [Export] public float SprintMultiplier { get; set; } = 1.55f;

        private PlayerController _player;
        private SurvivalController _survival;
        private float _baseSpeed;
        private Vector3 _lastPosition;
        private double _unsavedDistanceKm;

        public override void _Ready()
        {
            _player = GetParent<PlayerController>();
            _baseSpeed = _player.Speed;
            _lastPosition = _player.GlobalPosition;

            _survival = new SurvivalController();
            AddChild(_survival);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_player == null || !_player.GameplayEnabled)
                return;

            Vector2 input = Input.GetVector(
                "move_left",
                "move_right",
                "move_forward",
                "move_backward"
            );

            bool moving = input.LengthSquared() > 0.01f;
            bool sprintRequested = Input.IsActionPressed("sprint");
            bool sprinting =
                moving &&
                sprintRequested &&
                _survival.CanSprint;

            _player.Speed = sprinting
                ? _baseSpeed * SprintMultiplier
                : _baseSpeed;

            _survival.TickMovement(delta, moving, sprinting);

            float movedMeters = _player.GlobalPosition.DistanceTo(_lastPosition);
            _lastPosition = _player.GlobalPosition;

            if (movedMeters > 0f && movedMeters < 25f)
            {
                double km = movedMeters / 1000.0;
                GameplayProgressionService.RegisterDistance(km);
                _unsavedDistanceKm += km;

                if (_unsavedDistanceKm >= 0.10)
                {
                    _unsavedDistanceKm = 0;
                    GlobalSaveStore.SaveAll();
                }
            }

            if (GameState.Player.IsDown)
            {
                _player.Speed = 0f;
                _player.SetGameplayEnabled(false);
            }
        }
    }
}