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
        [Export] public string PlayerId = "local-player";
        [Export] public string ServerWebSocketUrl = "ws://127.0.0.1:8080/ws/spatial";

        private Camera3D _camera;
        private RayCast3D _rayCast;
        private MeshInstance3D _blockHighlight;

        public VoxelMaterial SelectedMaterial { get; set; } = VoxelMaterial.SteelBarricade;
        public bool IsXrayActive { get; set; }

        public override void _Ready()
        {
            _camera = GetNode<Camera3D>("Camera3D");
            _rayCast = GetNode<RayCast3D>("Camera3D/RayCast3D");
            _blockHighlight = GetNodeOrNull<MeshInstance3D>("BlockHighlight");
            _rayCast.TargetPosition = new Vector3(0, 0, -MineReach);

            DeltaSyncManager.Configure(ServerWebSocketUrl, PlayerId);
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
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
                GD.Print($"[TERRE ZÉRO] scanner X-Ray {(IsXrayActive ? "ACTIF" : "OFF")}");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
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
            ProcessVoxelInteraction();
        }

        private void ProcessVoxelInteraction()
        {
            if (!_rayCast.IsColliding())
                return;

            var collider = _rayCast.GetCollider();
            if (collider is not StaticBody3D staticBody || staticBody.GetParent() is not VoxelChunk chunk)
                return;

            Vector3 localHit = chunk.ToLocal(_rayCast.GetCollisionPoint());
            Vector3 hitNormal = _rayCast.GetCollisionNormal();

            if (Input.IsActionJustPressed("mine_voxel"))
            {
                int destroyed = chunk.CarveSphere(localHit, 0.4f);
                if (destroyed > 0)
                    PublishPendingEdits(chunk);
            }
            else if (Input.IsActionJustPressed("place_voxel"))
            {
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
            foreach (var edit in chunk.DrainPendingEdits())
            {
                bool destroy = edit.NewMaterial == VoxelMaterial.Air;
                byte materialId = destroy ? (byte)0 : (byte)edit.NewMaterial;

                _ = DeltaSyncManager.RecordVoxelModificationAsync(
                    chunk.H3Index,
                    chunk.ChunkCoord,
                    edit.Position,
                    destroy,
                    materialId,
                    PlayerId
                );
            }
        }
    }
}
