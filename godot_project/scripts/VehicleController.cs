// VehicleController.cs - Véhicules Terre Zéro

using Godot;

namespace TerreZero.Vehicles
{
    public enum VehicleChassis
    {
        MotoScrap,
        BuggyLeger,
        CamionBlinde
    }

    public enum VehicleDriveMode
    {
        ManualJoystick,
        AutopilotConvoy
    }

    public partial class VehicleController : Node3D
    {
        [Export] public VehicleChassis ChassisType = VehicleChassis.BuggyLeger;
        [Export] public VehicleDriveMode DriveMode = VehicleDriveMode.ManualJoystick;
        [Export] public float MaxSpeed = 18.0f;
        [Export] public float FuelPercent = 100.0f;
        [Export] public float CurrentHealth = 250.0f;

        public bool HasProwRam = true;
        public bool HasRoofTurret = true;
        public bool HasCargoFlatbed = true;

        private Vector3 _velocity = Vector3.Zero;

        public override void _PhysicsProcess(double delta)
        {
            if (FuelPercent <= 0.0f)
                return;

            if (DriveMode == VehicleDriveMode.AutopilotConvoy)
                FollowNomadAvatar(delta);
            else
                HandleManualInput(delta);

            if (_velocity.LengthSquared() > 0.1f)
                FuelPercent = Mathf.Max(0.0f, FuelPercent - (float)(0.05f * delta));
        }

        private void FollowNomadAvatar(double delta)
        {
            // Le routage OSM réel sera branché ici.
        }

        private void HandleManualInput(double delta)
        {
            Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            Vector3 targetDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            _velocity = targetDir != Vector3.Zero
                ? targetDir * MaxSpeed
                : _velocity.MoveToward(Vector3.Zero, (float)(10.0f * delta));

            GlobalPosition += _velocity * (float)delta;
        }

        public void OnVoxelObstacleCollision(Vector3I voxelCoord)
        {
            if (HasProwRam)
            {
                GD.Print($"[VÉHICULE] obstacle voxel broyé {voxelCoord}");
                return;
            }

            CurrentHealth -= 15.0f;
            _velocity *= 0.2f;
        }
    }
}
