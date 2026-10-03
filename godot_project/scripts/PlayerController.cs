using System;
using Godot;

namespace Chimeres.Voxel
{
    public partial class PlayerController : CharacterBody3D
    {
        [Export] public float Speed = 5.0f;
        [Export] public float JumpVelocity = 4.5f;
        [Export] public float MouseSensitivity = 0.003f;
        [Export] public float MineReach = 5.0f;

        private Camera3D _camera;
        private RayCast3D _rayCast;
        private MeshInstance3D _blockHighlight;

        public VoxelMaterial SelectedMaterial { get; set; } = VoxelMaterial.SteelBarricade;
        public bool IsXrayActive { get; set; } = false;

        public override void _Ready()
        {
            _camera = GetNode<Camera3D>("Camera3D");
            _rayCast = GetNode<RayCast3D>("Camera3D/RayCast3D");
            _blockHighlight = GetNode<MeshInstance3D>("BlockHighlight");
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                _camera.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

                Vector3 rot = _camera.Rotation;
                rot.X = Mathf.Clamp(rot.X, -Mathf.Pi / 2.2f, Mathf.Pi / 2.2f);
                _camera.Rotation = rot;
            }

            if (Input.IsActionJustPressed("toggle_xray"))
            {
                IsXrayActive = !IsXrayActive;
                GD.Print($"[Voxel] Mode X-Ray Sonde : {(IsXrayActive ? "ACTIF" : "OFF")}");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 velocity = Velocity;

            if (!IsOnFloor())
            {
                velocity += GetGravity() * (float)delta;
            }

            if (Input.IsActionJustPressed("jump") && IsOnFloor())
            {
                velocity.Y = JumpVelocity;
            }

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
            if (_rayCast.IsColliding())
            {
                var collider = _rayCast.GetCollider();
                Vector3 hitPos = _rayCast.GetCollisionPoint();
                Vector3 hitNormal = _rayCast.GetCollisionNormal();

                if (collider is StaticBody3D staticBody && staticBody.GetParent() is VoxelChunk chunk)
                {
                    Vector3 localHit = chunk.ToLocal(hitPos);

                    // Minage / Destruction (Clic Gauche)
                    if (Input.IsActionPressed("mine_voxel"))
                    {
                        chunk.CarveSphere(localHit, 0.4f); // 40cm drill sphere
                    }
                    // Pose de bloc (Clic Droit)
                    else if (Input.IsActionJustPressed("place_voxel"))
                    {
                        Vector3 placePos = localHit + hitNormal * VoxelChunk.VoxelScale;
                        int vx = Mathf.RoundToInt(placePos.X / VoxelChunk.VoxelScale);
                        int vy = Mathf.RoundToInt(placePos.Y / VoxelChunk.VoxelScale);
                        int vz = Mathf.RoundToInt(placePos.Z / VoxelChunk.VoxelScale);

                        chunk.SetVoxel(vx, vy, vz, SelectedMaterial);
                        chunk.RebuildMeshGreedy();
                    }
                }
            }
        }
    }
}
