// VehicleController.cs - Système de Véhicules & Conduite Hybride OSM (Godot 4 C#)
// Spécification Terre Zéro : Piste de roulement continue sur vecteurs OSM (highway=*),
// percutage des obstacles voxel et convoi autonome nomade suivant le joueur à pied.

using System;
using System.Collections.Generic;
using Godot;

namespace TerreZero.Vehicles
{
    public enum VehicleChassis
    {
        MotoScrap,   // Rapide, faible emport (30 kg), très maniable
        BuggyLeger,  // Équilibré, emport moyen (120 kg), pare-buffle
        CamionBlindé // Lourd 6x6, emport massif (800 kg), tourelle de toit
    }

    public enum VehicleDriveMode
    {
        ManualJoystick,   // Contrôle libre au joystick virtuel sur écran
        AutopilotConvoy   // Suit automatiquement l'avatar du joueur en convoi nomade IRL
    }

    public class VehicleController : Node3D
    {
        [Export] public VehicleChassis ChassisType = VehicleChassis.BuggyLeger;
        [Export] public VehicleDriveMode DriveMode = VehicleDriveMode.ManualJoystick;
        [Export] public float MaxSpeed = 18.0f; // ~65 km/h
        [Export] public float FuelPercent = 100.0f;
        [Export] public float CurrentHealth = 250.0f;

        // Modules Voxel Ancrés
        public bool HasProwRam = true;       // Lame de déblaiement en acier (broie les barricades voxel)
        public bool HasRoofTurret = true;    // Nid de mitrailleuse / siège de Chimère
        public bool HasCargoFlatbed = true;  // Bennes de fret voxel (x10 transport)

        private Vector3 _velocity = Vector3.Zero;

        public override void _PhysicsProcess(double delta)
        {
            if (FuelPercent <= 0.0f)
            {
                GD.Print("[VÉHICULE] Panne sèche de carburant !");
                return;
            }

            if (DriveMode == VehicleDriveMode.AutopilotConvoy)
            {
                // Mode Pilote Automatique Nomade : Suit les coordonnées Kalman GPS du joueur
                FollowNomadAvatar(delta);
            }
            else
            {
                // Mode Manuel : Joystick virtuel
                HandleManualInput(delta);
            }

            // Consommation de carburant en marche
            if (_velocity.LengthSquared() > 0.1f)
            {
                FuelPercent = Mathf.Max(0.0f, FuelPercent - (float)(0.05f * delta));
            }
        }

        private void FollowNomadAvatar(double delta)
        {
            // Le véhicule se déplace automatiquement le long du vecteur route OSM
            // vers la position GPS lissée de l'avatar avec une distance de sécurité de 5m.
        }

        private void HandleManualInput(double delta)
        {
            Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            Vector3 targetDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            if (targetDir != Vector3.Zero)
            {
                _velocity = targetDir * MaxSpeed;
            }
            else
            {
                _velocity = _velocity.MoveToward(Vector3.Zero, (float)(10.0f * delta));
            }

            GlobalPosition += _velocity * (float)delta;
        }

        // Détection de collision avec barricades voxel : broyage sans s'arrêter si Lame installée
        public void OnVoxelObstacleCollision(Vector3I voxelCoord)
        {
            if (HasProwRam)
            {
                GD.Print($"[VÉHICULE] Obstacle voxel broyé aux coordonnées {voxelCoord} !");
                // Émettre l'événement Delta DESTROY au serveur
            }
            else
            {
                CurrentHealth -= 15.0f;
                _velocity *= 0.2f; // Freinage brutal sans lame de proue
            }
        }
    }
}
