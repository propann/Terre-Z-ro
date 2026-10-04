using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using TerreZero.World.Geo;
using TerreZero.Network;

namespace TerreZero.World.Generation
{
    public sealed class StartLocationRequest
    {
        [JsonPropertyName("machine_latitude")]
        public double MachineLatitude { get; set; }

        [JsonPropertyName("machine_longitude")]
        public double MachineLongitude { get; set; }

        [JsonPropertyName("start_latitude")]
        public double StartLatitude { get; set; }

        [JsonPropertyName("start_longitude")]
        public double StartLongitude { get; set; }

        [JsonPropertyName("radius_km")]
        public double RadiusKm { get; set; }
    }

    public sealed class StartLocationResponse
    {
        [JsonPropertyName("allowed")]
        public bool Allowed { get; set; }

        [JsonPropertyName("distance_km")]
        public double DistanceKm { get; set; }

        [JsonPropertyName("radius_km")]
        public double RadiusKm { get; set; }

        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public sealed class SpatialCellPayload
    {
        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; } = string.Empty;

        [JsonPropertyName("resolution")]
        public int Resolution { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public sealed class WorldCellPayload
    {
        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; } = string.Empty;

        [JsonPropertyName("buildings")]
        public List<WorldBuildingPayload> Buildings { get; set; } = new();

        [JsonPropertyName("roads")]
        public List<WorldRoadPayload> Roads { get; set; } = new();
    }

    public sealed class WorldBuildingPayload
    {
        [JsonPropertyName("osm_id")]
        public long OSMID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("building_type")]
        public string BuildingType { get; set; } = "yes";

        [JsonPropertyName("amenity")]
        public string Amenity { get; set; } = string.Empty;

        [JsonPropertyName("levels")]
        public int Levels { get; set; } = 2;

        [JsonPropertyName("height_meters")]
        public float HeightMeters { get; set; } = 6f;

        [JsonPropertyName("world_version")]
        public int WorldVersion { get; set; } = 1;

        [JsonPropertyName("generator_version")]
        public int GeneratorVersion { get; set; } = 3;

        [JsonPropertyName("geometry")]
        public GeoJsonGeometry Geometry { get; set; }
    }

    public sealed class WorldRoadPayload
    {
        [JsonPropertyName("osm_id")]
        public long OSMID { get; set; }

        [JsonPropertyName("highway_type")]
        public string HighwayType { get; set; } = string.Empty;

        [JsonPropertyName("surface")]
        public string Surface { get; set; } = "asphalt";

        [JsonPropertyName("lanes")]
        public int Lanes { get; set; } = 2;

        [JsonPropertyName("geometry")]
        public GeoJsonGeometry Geometry { get; set; }
    }

    public sealed class GeoJsonGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("coordinates")]
        public JsonElement Coordinates { get; set; }

        public Vector2[] ProjectLineString(GeoAnchor anchor)
        {
            if (!string.Equals(Type, "LineString", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<Vector2>();

            if (Coordinates.ValueKind != JsonValueKind.Array)
                return Array.Empty<Vector2>();

            var points = new List<Vector2>(Coordinates.GetArrayLength());
            foreach (var coordinate in Coordinates.EnumerateArray())
            {
                if (coordinate.ValueKind != JsonValueKind.Array || coordinate.GetArrayLength() < 2)
                    continue;

                double longitude = coordinate[0].GetDouble();
                double latitude = coordinate[1].GetDouble();
                points.Add(anchor.ToLocalMeters(latitude, longitude));
            }

            return points.ToArray();
        }

        public Vector2[] ProjectOuterRing(GeoAnchor anchor)
        {
            if (!string.Equals(Type, "Polygon", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<Vector2>();

            if (Coordinates.ValueKind != JsonValueKind.Array || Coordinates.GetArrayLength() == 0)
                return Array.Empty<Vector2>();

            var outerRing = Coordinates[0];
            if (outerRing.ValueKind != JsonValueKind.Array)
                return Array.Empty<Vector2>();

            var points = new List<Vector2>(outerRing.GetArrayLength());
            foreach (var coordinate in outerRing.EnumerateArray())
            {
                if (coordinate.ValueKind != JsonValueKind.Array || coordinate.GetArrayLength() < 2)
                    continue;

                double longitude = coordinate[0].GetDouble();
                double latitude = coordinate[1].GetDouble();
                points.Add(anchor.ToLocalMeters(latitude, longitude));
            }

            if (points.Count > 1 && points[0].IsEqualApprox(points[^1]))
                points.RemoveAt(points.Count - 1);

            return points.ToArray();
        }
    }

    public sealed class CellDeltaPayload
    {
        [JsonPropertyName("h3_index")]
        public string H3Index { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("deltas")]
        public List<VoxelDeltaEvent> Deltas { get; set; } = new();
    }

    public sealed class WorldDataClient : IDisposable
    {
        private readonly System.Net.Http.HttpClient _http;

        public WorldDataClient(string baseUrl)
        {
            _http = new System.Net.Http.HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<StartLocationResponse> ValidateStartLocationAsync(
            double machineLatitude,
            double machineLongitude,
            double startLatitude,
            double startLongitude,
            double radiusKm,
            CancellationToken cancellationToken = default)
        {
            var request = new StartLocationRequest
            {
                MachineLatitude = machineLatitude,
                MachineLongitude = machineLongitude,
                StartLatitude = startLatitude,
                StartLongitude = startLongitude,
                RadiusKm = radiusKm
            };

            string json = JsonSerializer.Serialize(request);
            using var content = new System.Net.Http.StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            using var response = await _http.PostAsync(
                "api/v1/spatial/start",
                content,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<StartLocationResponse>(
                stream,
                cancellationToken: cancellationToken
            );

            return payload ?? new StartLocationResponse();
        }

        public async Task<SpatialCellPayload> ResolveSpatialCellAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            string lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lon = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            using var response = await _http.GetAsync(
                $"api/v1/spatial/cell?lat={Uri.EscapeDataString(lat)}&lon={Uri.EscapeDataString(lon)}",
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<SpatialCellPayload>(
                stream,
                cancellationToken: cancellationToken
            );

            return payload ?? new SpatialCellPayload
            {
                Latitude = latitude,
                Longitude = longitude,
                Resolution = 9
            };
        }

        public async Task<WorldCellPayload> GetCellAsync(
            string h3Index,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync(
                $"api/v1/world/cells/{Uri.EscapeDataString(h3Index)}",
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<WorldCellPayload>(
                stream,
                cancellationToken: cancellationToken
            );

            return payload ?? new WorldCellPayload { H3Index = h3Index };
        }

        public async Task<CellDeltaPayload> GetDeltasAsync(
            string h3Index,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync(
                $"api/v1/cells/{Uri.EscapeDataString(h3Index)}",
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<CellDeltaPayload>(
                stream,
                cancellationToken: cancellationToken
            );

            return payload ?? new CellDeltaPayload { H3Index = h3Index };
        }

        public void Dispose() => _http.Dispose();
    }
}
