using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace TerreZero.World.Geo
{
    public sealed class DesktopLocationResult
    {
        public bool Resolved { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double AccuracyMeters { get; init; }
        public string Source { get; init; } = "fallback";
    }

    public static class DesktopLocationProvider
    {
        private static readonly Regex LatitudePattern =
            new(@"Latitude:\s*([-+]?\d+(?:[\.,]\d+)?)", RegexOptions.IgnoreCase);

        private static readonly Regex LongitudePattern =
            new(@"Longitude:\s*([-+]?\d+(?:[\.,]\d+)?)", RegexOptions.IgnoreCase);

        private static readonly Regex AccuracyPattern =
            new(@"Accuracy:\s*([-+]?\d+(?:[\.,]\d+)?)", RegexOptions.IgnoreCase);

        public static async Task<DesktopLocationResult> ResolveAsync(
            double fallbackLatitude,
            double fallbackLongitude,
            CancellationToken cancellationToken = default)
        {
            if (TryEnvironment(out var environment))
                return environment;

            if (!string.Equals(OS.GetName(), "Linux", StringComparison.OrdinalIgnoreCase))
            {
                return Fallback(fallbackLatitude, fallbackLongitude);
            }

            const string geoCluePath = "/usr/libexec/geoclue-2.0/demos/where-am-i";
            if (!File.Exists(geoCluePath))
                return Fallback(fallbackLatitude, fallbackLongitude);

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = geoCluePath,
                        Arguments = "-t 4500",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(6));

                await process.WaitForExitAsync(timeout.Token);

                string output = await stdoutTask;
                _ = await stderrTask;

                Match latMatch = LatitudePattern.Match(output);
                Match lonMatch = LongitudePattern.Match(output);
                Match accuracyMatch = AccuracyPattern.Match(output);

                if (!latMatch.Success || !lonMatch.Success)
                    return Fallback(fallbackLatitude, fallbackLongitude);

                if (!TryParseCoordinate(latMatch.Groups[1].Value, out double lat) ||
                    !TryParseCoordinate(lonMatch.Groups[1].Value, out double lon))
                {
                    return Fallback(fallbackLatitude, fallbackLongitude);
                }

                double accuracy = 0;
                if (accuracyMatch.Success)
                    TryParseCoordinate(accuracyMatch.Groups[1].Value, out accuracy);

                return new DesktopLocationResult
                {
                    Resolved = true,
                    Latitude = lat,
                    Longitude = lon,
                    AccuracyMeters = Math.Max(0, accuracy),
                    Source = "GeoClue"
                };
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[LOCATION] GeoClue indisponible : {ex.Message}");
                return Fallback(fallbackLatitude, fallbackLongitude);
            }
        }

        private static bool TryEnvironment(out DesktopLocationResult result)
        {
            string latText = System.Environment.GetEnvironmentVariable(
                "TERRE_ZERO_LATITUDE"
            );
            string lonText = System.Environment.GetEnvironmentVariable(
                "TERRE_ZERO_LONGITUDE"
            );

            if (TryParseCoordinate(latText, out double lat) &&
                TryParseCoordinate(lonText, out double lon))
            {
                result = new DesktopLocationResult
                {
                    Resolved = true,
                    Latitude = lat,
                    Longitude = lon,
                    AccuracyMeters = 0,
                    Source = "Environment"
                };
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryParseCoordinate(string value, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return double.TryParse(
                value.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result
            );
        }

        private static DesktopLocationResult Fallback(double latitude, double longitude) =>
            new()
            {
                Resolved = false,
                Latitude = latitude,
                Longitude = longitude,
                AccuracyMeters = 0,
                Source = "Configured fallback"
            };
    }
}
