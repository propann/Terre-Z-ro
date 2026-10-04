using System;
using Godot;

namespace TerreZero.World.Generation
{
    public static class OSMBuildingSignDirector
    {
        public static void AddSign(
            Node3D parent,
            Vector2[] footprintMeters,
            string name,
            string buildingType,
            string amenity,
            string shop)
        {
            if (parent == null ||
                footprintMeters == null ||
                footprintMeters.Length < 2)
            {
                return;
            }

            string text = ResolveText(
                name,
                buildingType,
                amenity,
                shop
            );

            if (string.IsNullOrWhiteSpace(text))
                return;

            Vector2 centroid = ComputeCentroid(footprintMeters);
            Vector2 a = footprintMeters[0];
            Vector2 b = footprintMeters[1];
            Vector2 edgeMid = (a + b) * 0.5f;

            Vector2 outward = edgeMid - centroid;
            if (outward.LengthSquared() < 0.001f)
                outward = new Vector2(0f, -1f);
            else
                outward = outward.Normalized();

            Vector2 sign2D = edgeMid + outward * 0.35f;

            var label = new Label3D
            {
                Name = "OSMBuildingSign",
                Position = new Vector3(
                    sign2D.X,
                    2.35f,
                    sign2D.Y
                ),
                Text = text,
                FontSize = 28,
                OutlineSize = 10,
                PixelSize = 0.0038f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = false,
                Modulate = ResolveColor(
                    buildingType,
                    amenity,
                    shop
                )
            };

            parent.AddChild(label);
        }

        private static string ResolveText(
            string name,
            string buildingType,
            string amenity,
            string shop)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return Trim(name.ToUpperInvariant(), 28);

            if (!string.IsNullOrWhiteSpace(shop))
                return Trim(shop.Replace('_', ' ').ToUpperInvariant(), 24);

            if (!string.IsNullOrWhiteSpace(amenity))
            {
                return amenity switch
                {
                    "pharmacy" => "PHARMACIE",
                    "hospital" => "HÔPITAL",
                    "clinic" => "CLINIQUE",
                    "school" => "ÉCOLE",
                    "police" => "POLICE",
                    "fire_station" => "SECOURS",
                    _ => string.Empty
                };
            }

            if (buildingType == "industrial")
                return "ZONE INDUSTRIELLE";

            return string.Empty;
        }

        private static Color ResolveColor(
            string buildingType,
            string amenity,
            string shop)
        {
            if (amenity is "pharmacy" or "hospital" or "clinic")
                return new Color(0.38f, 0.90f, 0.58f);

            if (!string.IsNullOrWhiteSpace(shop))
                return new Color(0.92f, 0.66f, 0.28f);

            if (buildingType == "industrial")
                return new Color(0.62f, 0.68f, 0.70f);

            return new Color(0.78f, 0.82f, 0.78f);
        }

        private static Vector2 ComputeCentroid(Vector2[] polygon)
        {
            Vector2 sum = Vector2.Zero;

            foreach (Vector2 point in polygon)
                sum += point;

            return sum / polygon.Length;
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value[..Math.Max(1, maxLength - 1)] + "…";
        }
    }
}
