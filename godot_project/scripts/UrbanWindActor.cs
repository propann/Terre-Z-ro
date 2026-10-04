using Godot;

namespace TerreZero.World.Generation
{
    public partial class UrbanWindActor : Node3D
    {
        private float _strength;
        private float _directionDegrees;
        private float _phase;
        private Vector3 _baseRotation;

        public override void _Ready()
        {
            _baseRotation = RotationDegrees;
            _phase =
                Position.X * 0.37f +
                Position.Z * 0.23f;
        }

        public void ConfigureWind(
            float speedKmh,
            float directionDegrees)
        {
            _strength = Mathf.Clamp(speedKmh / 55f, 0f, 1f);
            _directionDegrees = directionDegrees;
        }

        public override void _Process(double delta)
        {
            _phase += (float)delta * Mathf.Lerp(0.7f, 2.1f, _strength);

            float wave = Mathf.Sin(_phase) * 5.5f * _strength;
            float direction = Mathf.DegToRad(_directionDegrees);

            RotationDegrees = _baseRotation + new Vector3(
                Mathf.Cos(direction) * wave,
                0f,
                Mathf.Sin(direction) * wave
            );
        }
    }
}
