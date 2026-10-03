using System;
using Godot;

namespace TerreZero.World.Geo
{
    public readonly struct GeoCoordinate
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public sealed class GeoAnchor
    {
        private const double EarthRadiusMeters = 6378137.0;

        public GeoCoordinate Origin { get; }

        public GeoAnchor(double latitude, double longitude)
        {
            Origin = new GeoCoordinate(latitude, longitude);
        }

        // Projection locale équirectangulaire : adaptée à une zone de jeu de quartier.
        // X = est/ouest, Z = nord/sud dans Godot.
        public Vector2 ToLocalMeters(double latitude, double longitude)
        {
            double lat0 = DegreesToRadians(Origin.Latitude);
            double lat = DegreesToRadians(latitude);
            double lon0 = DegreesToRadians(Origin.Longitude);
            double lon = DegreesToRadians(longitude);

            double x = (lon - lon0) * Math.Cos((lat + lat0) * 0.5) * EarthRadiusMeters;
            double z = (lat - lat0) * EarthRadiusMeters;

            return new Vector2((float)x, (float)z);
        }

        public GeoCoordinate ToGeoCoordinate(Vector2 localMeters)
        {
            double lat0 = DegreesToRadians(Origin.Latitude);
            double latitude = lat0 + (localMeters.Y / EarthRadiusMeters);

            double averageLat = (latitude + lat0) * 0.5;
            double longitude =
                DegreesToRadians(Origin.Longitude) +
                (localMeters.X / (EarthRadiusMeters * Math.Cos(averageLat)));

            return new GeoCoordinate(
                RadiansToDegrees(latitude),
                RadiansToDegrees(longitude)
            );
        }

        private static double DegreesToRadians(double degrees) =>
            degrees * Math.PI / 180.0;

        private static double RadiansToDegrees(double radians) =>
            radians * 180.0 / Math.PI;
    }
}
