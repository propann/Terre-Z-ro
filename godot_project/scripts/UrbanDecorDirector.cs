using System;
using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.World.Generation
{
    public static class UrbanDecorDirector
    {
        private const int AttemptCount = 44;
        private const float RadiusMeters = 28f;

        public static void Populate(
            Node3D parent,
            VoxelWorldGrid world,
            string h3Index,
            Vector3 origin,
            string context)
        {
            if (parent == null || world == null)
                return;

            foreach (Node child in parent.GetChildren())
                child.QueueFree();

            uint seed = StableHash(
                $"{h3Index}|{context}|urban-decor-v1"
            );
            var rng = new DeterministicRng(seed);

            string normalizedContext =
                (context ?? string.Empty).ToLowerInvariant();

            bool industrial =
                normalizedContext.Contains("industrial") ||
                normalizedContext.Contains("railway");

            bool natural =
                normalizedContext.Contains("park") ||
                normalizedContext.Contains("wood") ||
                normalizedContext.Contains("natural");

            int lampTarget = industrial ? 3 : natural ? 2 : 7;
            int barrierTarget = industrial ? 10 : natural ? 2 : 5;
            int debrisTarget = industrial ? 16 : natural ? 6 : 11;
            int plantTarget = natural ? 22 : industrial ? 5 : 12;

            int lamps = 0;
            int barriers = 0;
            int debris = 0;
            int plants = 0;

            for (int attempt = 0; attempt < AttemptCount; attempt++)
            {
                float angle = rng.NextFloat() * Mathf.Tau;
                float radius = 5f + rng.NextFloat() * (RadiusMeters - 5f);

                Vector3 position = origin + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                int gx = Mathf.FloorToInt(
                    position.X / VoxelChunk.VoxelScale
                );
                int gz = Mathf.FloorToInt(
                    position.Z / VoxelChunk.VoxelScale
                );

                VoxelMaterial ground = world.GetVoxelGlobal(gx, 0, gz);
                VoxelMaterial above = world.GetVoxelGlobal(gx, 1, gz);

                if (above != VoxelMaterial.Air)
                    continue;

                position.Y = VoxelChunk.VoxelScale;

                if (ground == VoxelMaterial.Sidewalk && lamps < lampTarget)
                {
                    AddLampPost(
                        parent,
                        position,
                        rng.NextFloat() * 360f
                    );
                    lamps++;
                    continue;
                }

                if (ground == VoxelMaterial.Sidewalk && barriers < barrierTarget)
                {
                    AddBarrier(
                        parent,
                        position,
                        rng.NextFloat() * 360f
                    );
                    barriers++;
                    continue;
                }

                if ((ground == VoxelMaterial.Asphalt ||
                     ground == VoxelMaterial.Sidewalk) &&
                    debris < debrisTarget)
                {
                    AddDebris(
                        parent,
                        position,
                        rng
                    );
                    debris++;
                    continue;
                }

                if ((ground == VoxelMaterial.GrassOrganic ||
                     ground == VoxelMaterial.Sidewalk) &&
                    plants < plantTarget)
                {
                    AddPlant(
                        parent,
                        position,
                        rng
                    );
                    plants++;
                }
            }

            PopulatePuddles(
                parent,
                world,
                origin,
                ref rng
            );
        }

        private static void PopulatePuddles(
            Node3D parent,
            VoxelWorldGrid world,
            Vector3 origin,
            ref DeterministicRng rng)
        {
            int created = 0;

            for (int attempt = 0; attempt < 28 && created < 10; attempt++)
            {
                float angle = rng.NextFloat() * Mathf.Tau;
                float radius = 4f + rng.NextFloat() * 22f;

                Vector3 position = origin + new Vector3(
                    Mathf.Cos(angle) * radius,
                    VoxelChunk.VoxelScale + 0.012f,
                    Mathf.Sin(angle) * radius
                );

                int gx = Mathf.FloorToInt(
                    position.X / VoxelChunk.VoxelScale
                );
                int gz = Mathf.FloorToInt(
                    position.Z / VoxelChunk.VoxelScale
                );

                VoxelMaterial ground =
                    world.GetVoxelGlobal(gx, 0, gz);

                if (ground is not
                    (VoxelMaterial.Asphalt or VoxelMaterial.Sidewalk))
                {
                    continue;
                }

                var material = new StandardMaterial3D
                {
                    Transparency =
                        BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor =
                        new Color(0.06f, 0.11f, 0.14f, 0f),
                    Roughness = 0.08f,
                    Metallic = 0.05f
                };

                var puddle = new MeshInstance3D
                {
                    Name = "WeatherPuddle",
                    Position = position,
                    RotationDegrees = new Vector3(
                        0f,
                        rng.NextFloat() * 360f,
                        0f
                    ),
                    Mesh = new PlaneMesh
                    {
                        Size = new Vector2(
                            rng.NextRange(0.55f, 1.45f),
                            rng.NextRange(0.28f, 0.82f)
                        )
                    },
                    MaterialOverride = material,
                    Visible = false
                };

                parent.AddChild(puddle);
                created++;
            }
        }

        public static void SetWetness(
            Node3D parent,
            float wetness)
        {
            if (parent == null)
                return;

            float value = Mathf.Clamp(wetness, 0f, 1f);

            foreach (Node child in parent.GetChildren())
                ApplyWetnessRecursive(child, value);
        }

        private static void ApplyWetnessRecursive(
            Node node,
            float wetness)
        {
            if (node is MeshInstance3D mesh &&
                node.Name == "WeatherPuddle" &&
                mesh.MaterialOverride is StandardMaterial3D material)
            {
                mesh.Visible = wetness > 0.12f;

                Color color = material.AlbedoColor;
                color.A = Mathf.Lerp(0f, 0.52f, wetness);
                material.AlbedoColor = color;
                material.Roughness = Mathf.Lerp(
                    0.20f,
                    0.035f,
                    wetness
                );
            }

            foreach (Node child in node.GetChildren())
                ApplyWetnessRecursive(child, wetness);
        }

        public static void SetWind(
            Node3D parent,
            float speedKmh,
            float directionDegrees)
        {
            if (parent == null)
                return;

            foreach (Node child in parent.GetChildren())
                ApplyWindRecursive(
                    child,
                    speedKmh,
                    directionDegrees
                );
        }

        private static void ApplyWindRecursive(
            Node node,
            float speedKmh,
            float directionDegrees)
        {
            if (node is UrbanWindActor windActor)
            {
                windActor.ConfigureWind(
                    speedKmh,
                    directionDegrees
                );
            }

            foreach (Node child in node.GetChildren())
                ApplyWindRecursive(
                    child,
                    speedKmh,
                    directionDegrees
                );
        }

        public static void SetStreetLights(
            Node3D parent,
            bool enabled,
            float stormIntensity = 0f)
        {
            if (parent == null)
                return;

            float energy = enabled
                ? Mathf.Lerp(0.85f, 1.45f, Mathf.Clamp(stormIntensity, 0f, 1f))
                : 0f;

            foreach (Node child in parent.GetChildren())
                ApplyStreetLightRecursive(child, energy);
        }

        private static void ApplyStreetLightRecursive(
            Node node,
            float energy)
        {
            if (node is OmniLight3D light &&
                node.Name == "StreetLight")
            {
                light.LightEnergy = energy;
            }

            foreach (Node child in node.GetChildren())
                ApplyStreetLightRecursive(child, energy);
        }

        private static void AddLampPost(
            Node3D parent,
            Vector3 position,
            float yawDegrees)
        {
            var root = new Node3D
            {
                Name = "LampPost",
                Position = position,
                RotationDegrees = new Vector3(0f, yawDegrees, 0f)
            };
            parent.AddChild(root);

            var darkMetal = MakeMaterial(
                new Color(0.12f, 0.14f, 0.15f),
                metallic: 0.72f,
                roughness: 0.58f
            );

            root.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.045f,
                    BottomRadius = 0.06f,
                    Height = 2.8f
                },
                Position = new Vector3(0f, 1.4f, 0f),
                MaterialOverride = darkMetal
            });

            root.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.48f, 0.07f, 0.07f)
                },
                Position = new Vector3(0.19f, 2.72f, 0f),
                MaterialOverride = darkMetal
            });

            var glow = MakeMaterial(
                new Color(0.82f, 0.62f, 0.28f),
                metallic: 0.05f,
                roughness: 0.35f,
                emission: 1.4f
            );

            root.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.22f, 0.12f, 0.16f)
                },
                Position = new Vector3(0.42f, 2.66f, 0f),
                MaterialOverride = glow
            });

            root.AddChild(new OmniLight3D
            {
                Name = "StreetLight",
                Position = new Vector3(0.42f, 2.55f, 0f),
                LightColor = new Color(1.0f, 0.67f, 0.28f),
                LightEnergy = 0f,
                OmniRange = 5.2f,
                ShadowEnabled = false
            });
        }

        private static void AddBarrier(
            Node3D parent,
            Vector3 position,
            float yawDegrees)
        {
            var root = new Node3D
            {
                Name = "StreetBarrier",
                Position = position,
                RotationDegrees = new Vector3(0f, yawDegrees, 0f)
            };
            parent.AddChild(root);

            var metal = MakeMaterial(
                new Color(0.23f, 0.26f, 0.27f),
                metallic: 0.55f,
                roughness: 0.68f
            );

            var warning = MakeMaterial(
                new Color(0.62f, 0.34f, 0.12f),
                metallic: 0.18f,
                roughness: 0.74f
            );

            for (int i = -1; i <= 1; i++)
            {
                root.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(0.09f, 0.65f, 0.09f)
                    },
                    Position = new Vector3(i * 0.52f, 0.33f, 0f),
                    MaterialOverride = metal
                });
            }

            root.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = new Vector3(1.2f, 0.18f, 0.10f)
                },
                Position = new Vector3(0f, 0.48f, 0f),
                MaterialOverride = warning
            });
        }

        private static void AddDebris(
            Node3D parent,
            Vector3 position,
            DeterministicRng rng)
        {
            var root = new Node3D
            {
                Name = "Debris",
                Position = position,
                RotationDegrees = new Vector3(
                    rng.NextRange(-8f, 8f),
                    rng.NextFloat() * 360f,
                    rng.NextRange(-8f, 8f)
                )
            };
            parent.AddChild(root);

            int pieces = 2 + rng.NextInt(0, 3);
            for (int i = 0; i < pieces; i++)
            {
                Color color = i % 2 == 0
                    ? new Color(0.24f, 0.23f, 0.21f)
                    : new Color(0.31f, 0.20f, 0.13f);

                root.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(
                            rng.NextRange(0.18f, 0.55f),
                            rng.NextRange(0.06f, 0.22f),
                            rng.NextRange(0.16f, 0.48f)
                        )
                    },
                    Position = new Vector3(
                        rng.NextRange(-0.32f, 0.32f),
                        rng.NextRange(0.04f, 0.12f),
                        rng.NextRange(-0.32f, 0.32f)
                    ),
                    RotationDegrees = new Vector3(
                        rng.NextRange(-20f, 20f),
                        rng.NextFloat() * 360f,
                        rng.NextRange(-20f, 20f)
                    ),
                    MaterialOverride = MakeMaterial(
                        color,
                        metallic: 0.22f,
                        roughness: 0.88f
                    )
                });
            }
        }

        private static void AddPlant(
            Node3D parent,
            Vector3 position,
            DeterministicRng rng)
        {
            var root = new UrbanWindActor
            {
                Name = "UrbanPlant",
                Position = position,
                RotationDegrees = new Vector3(
                    0f,
                    rng.NextFloat() * 360f,
                    0f
                )
            };
            parent.AddChild(root);

            var green = MakeMaterial(
                new Color(
                    rng.NextRange(0.12f, 0.24f),
                    rng.NextRange(0.28f, 0.43f),
                    rng.NextRange(0.13f, 0.23f)
                ),
                metallic: 0f,
                roughness: 0.96f
            );

            int stems = 3 + rng.NextInt(0, 4);
            for (int i = 0; i < stems; i++)
            {
                float angle = i * Mathf.Tau / stems +
                              rng.NextRange(-0.2f, 0.2f);
                float height = rng.NextRange(0.28f, 0.72f);

                root.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(0.045f, height, 0.12f)
                    },
                    Position = new Vector3(
                        Mathf.Cos(angle) * 0.12f,
                        height * 0.5f,
                        Mathf.Sin(angle) * 0.12f
                    ),
                    RotationDegrees = new Vector3(
                        rng.NextRange(-18f, 18f),
                        Mathf.RadToDeg(angle),
                        rng.NextRange(-18f, 18f)
                    ),
                    MaterialOverride = green
                });
            }
        }

        private static StandardMaterial3D MakeMaterial(
            Color color,
            float metallic,
            float roughness,
            float emission = 0f)
        {
            var material = new StandardMaterial3D
            {
                AlbedoColor = color,
                Metallic = metallic,
                Roughness = roughness
            };

            if (emission > 0f)
            {
                material.EmissionEnabled = true;
                material.Emission = color;
                material.EmissionEnergyMultiplier = emission;
            }

            return material;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }

                return hash == 0 ? 0x9E3779B9u : hash;
            }
        }

        private struct DeterministicRng
        {
            private uint _state;

            public DeterministicRng(uint seed)
            {
                _state = seed == 0 ? 0xA341316Cu : seed;
            }

            public uint Next()
            {
                uint x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }

            public float NextFloat() =>
                (Next() & 0x00FFFFFFu) / 16777215f;

            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive)
                    return minInclusive;

                return minInclusive +
                    (int)(Next() % (uint)(maxExclusive - minInclusive));
            }

            public float NextRange(float min, float max) =>
                Mathf.Lerp(min, max, NextFloat());
        }
    }
}
