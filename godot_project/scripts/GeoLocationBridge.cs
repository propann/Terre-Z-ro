using System;

namespace TerreZero.World.Geo
{
    public readonly struct GeoPositionSample
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public float AccuracyMeters { get; }
        public DateTime TimestampUtc { get; }

        public GeoPositionSample(
            double latitude,
            double longitude,
            float accuracyMeters,
            DateTime timestampUtc)
        {
            Latitude = latitude;
            Longitude = longitude;
            AccuracyMeters = accuracyMeters;
            TimestampUtc = timestampUtc;
        }
    }

    public static class GeoLocationBridge
    {
        public static event Action<GeoPositionSample> PositionChanged;

        public static GeoPositionSample? LastPosition { get; private set; }

        public static void Publish(
            double latitude,
            double longitude,
            float accuracyMeters = 0f)
        {
            if (latitude < -90 || latitude > 90 ||
                longitude < -180 || longitude > 180)
                return;

            var sample = new GeoPositionSample(
                latitude,
                longitude,
                Math.Max(0f, accuracyMeters),
                DateTime.UtcNow
            );

            LastPosition = sample;
            PositionChanged?.Invoke(sample);
        }
    }
}
