using System;
using Godot;

namespace TerreZero.Gameplay
{
    public partial class SurvivalController : Node
    {
        public event Action StateChanged;

        [Export] public float StaminaDrainPerSecond { get; set; } = 7f;
        [Export] public float StaminaRecoveryPerSecond { get; set; } = 10f;
        [Export] public float RadiationDamageThreshold { get; set; } = 70f;
        [Export] public float RadiationDamagePerSecond { get; set; } = 1.5f;

        private float _staminaAccumulator;
        private float _radiationAccumulator;

        public void TickMovement(double delta, bool moving, bool sprinting)
        {
            var player = GameState.Player;

            float staminaDelta = sprinting && moving
                ? -StaminaDrainPerSecond * (float)delta
                : StaminaRecoveryPerSecond * (float)delta;

            _staminaAccumulator += staminaDelta;

            if (Mathf.Abs(_staminaAccumulator) >= 1f)
            {
                int whole = Mathf.FloorToInt(Mathf.Abs(_staminaAccumulator));
                int direction = _staminaAccumulator > 0 ? 1 : -1;

                player.Stamina = Math.Clamp(
                    player.Stamina + whole * direction,
                    0,
                    player.MaxStamina
                );

                _staminaAccumulator -= whole * direction;
                StateChanged?.Invoke();
            }

            if (player.Radiation >= RadiationDamageThreshold)
            {
                _radiationAccumulator += RadiationDamagePerSecond * (float)delta;
                if (_radiationAccumulator >= 1f)
                {
                    int damage = Mathf.FloorToInt(_radiationAccumulator);
                    player.Damage(damage);
                    _radiationAccumulator -= damage;
                    StateChanged?.Invoke();
                }
            }
        }

        public bool CanSprint =>
            GameState.Player.Stamina > 0 &&
            !GameState.Player.IsDown;

        public void AddRadiation(int amount)
        {
            GameState.Player.Radiation = Math.Clamp(
                GameState.Player.Radiation + Math.Max(0, amount),
                0,
                100
            );
            StateChanged?.Invoke();
        }

        public void ReduceRadiation(int amount)
        {
            GameState.Player.Radiation = Math.Clamp(
                GameState.Player.Radiation - Math.Max(0, amount),
                0,
                100
            );
            StateChanged?.Invoke();
        }
    }
}