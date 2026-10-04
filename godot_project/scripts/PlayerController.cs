using System;
using Godot;
using TerreZero.Network;

namespace TerreZero.World.Voxel
{
    public partial class PlayerController : CharacterBody3D
    {
        [Export] public float Speed = 5.0f;
        [Export] public float JumpVelocity = 4.5f;
        [Export] public float MouseSensitivity = 0.003f;
        [Export] public float MineReach = 5.0f;
        [Export] public float BaseFov = 75.0f;
        [Export] public float SprintFov = 82.0f;
        [Export] public float HeadBobAmplitude = 0.035f;
        [Export] public float HeadBobFrequency = 10.0f;
        [Export] public string PlayerId = "local-player";
        [Export] public string ServerWebSocketUrl = "ws://127.0.0.1:8080/ws/spatial";

        private Camera3D _camera;
        private RayCast3D _rayCast;
        private MeshInstance3D _blockHighlight;
        private Vector3 _cameraBasePosition;
        private float _headBobPhase;

        public VoxelMaterial SelectedMaterial { get; set; } = VoxelMaterial.SteelBarricade;
        public bool IsXrayActive { get; set; }
        public event Action<bool> ScannerChanged;
        public event Action<VoxelMaterial> MaterialChanged;
        public bool GameplayEnabled { get; private set; } = true;

        public void SetGameplayEnabled(bool enabled)
        {
            GameplayEnabled = enabled;
            SetPhysicsProcess(enabled);
            SetProcessUnhandledInput(enabled);

            if (enabled)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else
            {
                Velocity = Vector3.Zero;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }

        public override void _Ready()
        {
            _camera = GetNode<Camera3D>("Camera3D");
            _rayCast = GetNode<RayCast3D>("Camera3D/RayCast3D");
            _blockHighlight = GetNodeOrNull<MeshInstance3D>("BlockHighlight");
            _cameraBasePosition = _camera.Position;
            _camera.Fov = BaseFov;
            _rayCast.TargetPosition = new Vector3(0, 0, -MineReach);

            DeltaSyncManager.Configure(ServerWebSocketUrl, PlayerId);
            SetGameplayEnabled(true);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey key &&
                key.Pressed &&
                !key.Echo &&
                key.Keycode == Key.Escape)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                return;
            }

            if (@event is InputEventMouseButton mouseButton &&
                mouseButton.Pressed &&
                mouseButton.ButtonIndex == MouseButton.Left &&
                Input.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                return;
            }

            if (@event is InputEventMouseButton wheel &&
                wheel.Pressed &&
                Input.MouseMode == Input.MouseModeEnum.Captured &&
                (wheel.ButtonIndex == MouseButton.WheelUp ||
                 wheel.ButtonIndex == MouseButton.WheelDown))
            {
                CycleBuildMaterial(
                    wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1
                );
                return;
            }

            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                _camera.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

                Vector3 rotation = _camera.Rotation;
                rotation.X = Mathf.Clamp(rotation.X, -Mathf.Pi / 2.2f, Mathf.Pi / 2.2f);
                _camera.Rotation = rotation;
            }

            if (Input.IsActionJustPressed("toggle_xray"))
            {
                IsXrayActive = !IsXrayActive;
                ScannerChanged?.Invoke(IsXrayActive);
                GD.Print($"[TERRE ZÉRO] scanner X-Ray {(IsXrayActive ? "ACTIF" : "OFF")}");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!GameplayEnabled)
                return;

            Vector3 velocity = Velocity;

            if (!IsOnFloor())
                velocity += GetGravity() * (float)delta;

            if (Input.IsActionJustPressed("jump") && IsOnFloor())
                velocity.Y = JumpVelocity;

            Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
            Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
            }

            Velocity = velocity;
            MoveAndSlide();
            UpdateCameraMotion(delta);
            ProcessVoxelInteraction();
        }

        private void UpdateCameraMotion(double delta)
        {
            if (_camera == null)
                return;

            Vector2 planarVelocity = new(
                Velocity.X,
                Velocity.Z
            );
            float speed = planarVelocity.Length();

            bool moving =
                IsOnFloor() &&
                speed > 0.15f;

            if (moving)
            {
                float speedFactor = Mathf.Clamp(
                    speed / Math.Max(0.01f, Speed),
                    0.45f,
                    1.35f
                );

                _headBobPhase +=
                    (float)delta *
                    HeadBobFrequency *
                    speedFactor;
            }
            else
            {
                _headBobPhase = Mathf.Lerp(
                    _headBobPhase,
                    0f,
                    (float)delta * 5f
                );
            }

            float bobY = moving
                ? Mathf.Sin(_headBobPhase) *
                  HeadBobAmplitude
                : 0f;

            float bobX = moving
                ? Mathf.Cos(_headBobPhase * 0.5f) *
                  HeadBobAmplitude *
                  0.45f
                : 0f;

            Vector3 targetPosition =
                _cameraBasePosition +
                new Vector3(bobX, bobY, 0f);

            _camera.Position = _camera.Position.Lerp(
                targetPosition,
                Mathf.Clamp((float)delta * 12f, 0f, 1f)
            );

            float speedRatio = Mathf.Clamp(
                (speed - 5f) / 3f,
                0f,
                1f
            );

            float targetFov = Mathf.Lerp(
                BaseFov,
                SprintFov,
                speedRatio
            );

            _camera.Fov = Mathf.Lerp(
                _camera.Fov,
                targetFov,
                Mathf.Clamp((float)delta * 7f, 0f, 1f)
            );
        }

        private void CycleBuildMaterial(int direction)
        {
            VoxelMaterial[] buildPalette =
            {
                VoxelMaterial.SteelBarricade,
                VoxelMaterial.Concrete,
                VoxelMaterial.Brick,
                VoxelMaterial.ReinforcedGlass,
                VoxelMaterial.TurretBase
            };

            int index = Array.IndexOf(
                buildPalette,
                SelectedMaterial
            );

            if (index < 0)
                index = 0;

            index = (index + direction) % buildPalette.Length;
            if (index < 0)
                index += buildPalette.Length;

            SelectedMaterial = buildPalette[index];
            MaterialChanged?.Invoke(SelectedMaterial);
        }

        private void ProcessVoxelInteraction()
        {
            if (!_rayCast.IsColliding())
                return;

            var collider = _rayCast.GetCollider();
            if (collider is not StaticBody3D staticBody || staticBody.GetParent() is not VoxelChunk chunk)
                return;

            Vector3 collisionPoint = _rayCast.GetCollisionPoint();
            Vector3 localHit = chunk.ToLocal(collisionPoint);
            Vector3 hitNormal = _rayCast.GetCollisionNormal();

            if (Input.IsActionJustPressed("mine_voxel"))
            {
                int destroyed = chunk.CarveSphere(localHit, 0.4f);
                if (destroyed > 0)
                    PublishPendingEdits(chunk);
            }
            else if (Input.IsActionJustPressed("place_voxel"))
            {
                if (chunk.WorldGrid != null)
                {
                    Vector3 worldPlace = collisionPoint + hitNormal * (VoxelChunk.VoxelScale * 0.55f);
                    chunk.WorldGrid.PlaceVoxelAtWorldPosition(worldPlace, SelectedMaterial, trackEdit: true);
                    PublishPendingEdits(chunk);
                    return;
                }

                Vector3 placePos = localHit + hitNormal * VoxelChunk.VoxelScale;
                int vx = Mathf.RoundToInt(placePos.X / VoxelChunk.VoxelScale);
                int vy = Mathf.RoundToInt(placePos.Y / VoxelChunk.VoxelScale);
                int vz = Mathf.RoundToInt(placePos.Z / VoxelChunk.VoxelScale);

                chunk.SetVoxel(vx, vy, vz, SelectedMaterial, trackEdit: true);
                chunk.RebuildMeshGreedy();
                PublishPendingEdits(chunk);
            }
        }

        private void PublishPendingEdits(VoxelChunk chunk)
        {
            if (chunk.WorldGrid != null)
            {
                foreach (var worldEdit in chunk.WorldGrid.DrainPendingEdits())
                    PublishEdit(chunk.H3Index, worldEdit.ChunkCoord, worldEdit.Edit);
                return;
            }

            foreach (var edit in chunk.DrainPendingEdits())
                PublishEdit(chunk.H3Index, chunk.ChunkCoord, edit);
        }

        private void PublishEdit(string h3Index, Vector3I chunkCoord, VoxelEdit edit)
        {
            bool destroy = edit.NewMaterial == VoxelMaterial.Air;
            byte materialId = destroy ? (byte)0 : (byte)edit.NewMaterial;

            _ = DeltaSyncManager.RecordVoxelModificationAsync(
                h3Index,
                chunkCoord,
                edit.Position,
                destroy,
                materialId,
                PlayerId
            );
        }
    }
}
