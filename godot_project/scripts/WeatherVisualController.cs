using System;
using System.Globalization;
using Godot;
using TerreZero.UI;
using TerreZero.World.Generation;

namespace TerreZero.World.Weather
{
    public partial class WeatherVisualController : Node
    {
        private WorldEnvironment _worldEnvironment;
        private DirectionalLight3D _sun;
        private GameHUD _hud;
        private WeatherOverlay _overlay;
        private WeatherPayload _lastWeather;

        public float CurrentSurfaceWetness { get; private set; }
        public float CurrentSnowCover { get; private set; }
        public bool CurrentIsDay { get; private set; } = true;
        public float CurrentStormIntensity { get; private set; }
        public float CurrentWindSpeedKmh { get; private set; }
        public float CurrentWindDirectionDegrees { get; private set; }

        public void Initialize(
            WorldEnvironment worldEnvironment,
            DirectionalLight3D sun,
            GameHUD hud)
        {
            _worldEnvironment = worldEnvironment;
            _sun = sun;
            _hud = hud;

            var layer = new CanvasLayer
            {
                Name = "WeatherOverlayLayer",
                Layer = 12
            };
            AddChild(layer);

            _overlay = new WeatherOverlay
            {
                Name = "WeatherOverlay",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            layer.AddChild(_overlay);
        }

        public void Apply(WeatherPayload weather)
        {
            if (weather?.Current == null)
                return;

            _lastWeather = weather;
            WeatherCurrentPayload current = weather.Current;

            float cloud = Mathf.Clamp((float)(current.CloudCoverPercent / 100.0), 0f, 1f);
            float precipitation = Mathf.Clamp((float)(current.PrecipitationMM / 2.0), 0f, 1f);
            float rain = Mathf.Clamp((float)((current.RainMM + current.ShowersMM) / 2.0), 0f, 1f);
            float snow = Mathf.Clamp((float)(current.SnowfallCM / 1.5), 0f, 1f);
            CurrentSnowCover = Mathf.Clamp(
                (float)(current.SnowfallCM / 0.8),
                0f,
                1f
            );
            CurrentSurfaceWetness = Mathf.Clamp(
                Mathf.Max(rain, precipitation * 0.85f),
                0f,
                1f
            );
            bool isDay = current.IsDay == 1;
            CurrentIsDay = isDay;
            CurrentWindSpeedKmh = (float)current.WindSpeedKmh;
            CurrentWindDirectionDegrees = (float)current.WindDirectionDeg;

            CurrentStormIntensity =
                current.WeatherCode >= 95 &&
                current.WeatherCode <= 99
                    ? 1f
                    : Mathf.Clamp(
                        (float)(current.CloudCoverPercent / 100.0) *
                        (float)(current.PrecipitationMM / 2.0),
                        0f,
                        1f
                    );

            ApplyEnvironment(
                cloud,
                precipitation,
                isDay,
                current.WeatherCode,
                current.RelativeHumidityPercent,
                current.TemperatureC
            );
            ApplySun(
                cloud,
                isDay,
                current.Time,
                weather.Latitude
            );
            ApplyOverlay(
                rain,
                snow,
                current.WindSpeedKmh,
                current.WindDirectionDeg,
                current.WeatherCode
            );

            _hud?.SetWeather(
                BuildWeatherLabel(current, weather.Source)
            );
        }

        private void ApplyEnvironment(
            float cloud,
            float precipitation,
            bool isDay,
            int weatherCode,
            double humidityPercent,
            double temperatureC)
        {
            Godot.Environment env = _worldEnvironment?.Environment;
            if (env == null)
                return;

            float stormFactor = Mathf.Clamp(
                Mathf.Max(cloud * 0.75f, precipitation),
                0f,
                1f
            );

            float humidity = Mathf.Clamp(
                (float)((humidityPercent - 55.0) / 45.0),
                0f,
                1f
            );

            float explicitFog =
                weatherCode is 45 or 48 ? 1f : 0f;

            float fogFactor = Mathf.Clamp(
                Mathf.Max(
                    explicitFog,
                    Mathf.Max(stormFactor * 0.75f, humidity * 0.55f)
                ),
                0f,
                1f
            );

            float cold = Mathf.Clamp(
                (float)((8.0 - temperatureC) / 18.0),
                0f,
                1f
            );
            float heat = Mathf.Clamp(
                (float)((temperatureC - 24.0) / 16.0),
                0f,
                1f
            );

            env.AmbientLightEnergy = isDay
                ? Mathf.Lerp(1.0f, 0.48f, stormFactor)
                : Mathf.Lerp(0.24f, 0.12f, stormFactor);

            env.FogEnabled = true;
            env.FogDensity = Mathf.Lerp(
                isDay ? 0.0045f : 0.010f,
                explicitFog > 0.5f ? 0.060f : 0.038f,
                fogFactor
            );

            Color dayFog = new Color(
                Mathf.Lerp(0.34f, 0.16f, stormFactor),
                Mathf.Lerp(0.42f, 0.20f, stormFactor),
                Mathf.Lerp(0.52f, 0.26f, stormFactor)
            );

            dayFog = dayFog.Lerp(
                new Color(0.28f, 0.38f, 0.48f),
                cold * 0.32f
            );
            dayFog = dayFog.Lerp(
                new Color(0.52f, 0.38f, 0.26f),
                heat * 0.22f
            );

            env.FogLightColor = isDay
                ? dayFog
                : new Color(0.05f, 0.07f, 0.12f);

            if (env.Sky?.SkyMaterial is ProceduralSkyMaterial sky)
            {
                if (isDay)
                {
                    sky.SkyTopColor = new Color(
                        Mathf.Lerp(0.16f, 0.055f, cloud),
                        Mathf.Lerp(0.32f, 0.075f, cloud),
                        Mathf.Lerp(0.55f, 0.11f, cloud)
                    );
                    sky.SkyHorizonColor = new Color(
                        Mathf.Lerp(0.72f, 0.26f, cloud),
                        Mathf.Lerp(0.58f, 0.29f, cloud),
                        Mathf.Lerp(0.40f, 0.33f, cloud)
                    );
                    sky.GroundHorizonColor = new Color(
                        Mathf.Lerp(0.24f, 0.10f, cloud),
                        Mathf.Lerp(0.26f, 0.12f, cloud),
                        Mathf.Lerp(0.25f, 0.14f, cloud)
                    );
                }
                else
                {
                    sky.SkyTopColor = new Color(0.015f, 0.025f, 0.055f);
                    sky.SkyHorizonColor = new Color(0.045f, 0.06f, 0.10f);
                    sky.GroundHorizonColor = new Color(0.025f, 0.03f, 0.05f);
                }
            }
        }

        private void ApplySun(
            float cloud,
            bool isDay,
            string localTime,
            double latitude)
        {
            if (_sun == null)
                return;

            if (!TryComputeSolarAngles(
                    localTime,
                    latitude,
                    out float elevationDegrees,
                    out float azimuthDegrees))
            {
                float hour = ParseLocalHour(localTime);
                float fallbackDaylight = isDay
                    ? Mathf.Clamp(
                        Mathf.Sin((hour - 6f) / 12f * Mathf.Pi),
                        0.15f,
                        1f
                    )
                    : 0.06f;

                elevationDegrees = Mathf.Lerp(
                    8f,
                    68f,
                    fallbackDaylight
                );
                azimuthDegrees = Mathf.Lerp(
                    70f,
                    290f,
                    Mathf.Clamp((hour - 6f) / 12f, 0f, 1f)
                );
            }

            float daylight = isDay
                ? Mathf.Clamp(
                    Mathf.Sin(Mathf.DegToRad(
                        Mathf.Clamp(elevationDegrees, 0f, 90f)
                    )),
                    0.08f,
                    1f
                )
                : 0.04f;

            _sun.LightEnergy = isDay
                ? Mathf.Lerp(0.35f, 1.85f, daylight) *
                  Mathf.Lerp(1f, 0.58f, cloud)
                : 0.07f;

            float warmHorizon = 1f - Mathf.Clamp(
                elevationDegrees / 32f,
                0f,
                1f
            );

            _sun.LightColor = isDay
                ? new Color(
                    1.0f,
                    Mathf.Lerp(0.70f, 0.95f, 1f - warmHorizon),
                    Mathf.Lerp(0.52f, 0.88f, 1f - warmHorizon)
                )
                : new Color(0.28f, 0.36f, 0.55f);

            float pitch = -Mathf.Clamp(
                elevationDegrees,
                3f,
                89f
            );

            _sun.RotationDegrees = new Vector3(
                pitch,
                azimuthDegrees - 180f,
                0f
            );
        }

        private static bool TryComputeSolarAngles(
            string localTime,
            double latitude,
            out float elevationDegrees,
            out float azimuthDegrees)
        {
            elevationDegrees = 35f;
            azimuthDegrees = 180f;

            if (!DateTime.TryParse(
                    localTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime local))
            {
                return false;
            }

            double latRad = latitude * Math.PI / 180.0;
            int day = local.DayOfYear;

            double declinationDeg =
                23.44 *
                Math.Sin(
                    2.0 * Math.PI *
                    (284.0 + day) /
                    365.0
                );

            double declinationRad =
                declinationDeg * Math.PI / 180.0;

            double hour =
                local.Hour +
                local.Minute / 60.0 +
                local.Second / 3600.0;

            double hourAngleDeg =
                15.0 * (hour - 12.0);
            double hourAngleRad =
                hourAngleDeg * Math.PI / 180.0;

            double sinElevation =
                Math.Sin(latRad) * Math.Sin(declinationRad) +
                Math.Cos(latRad) * Math.Cos(declinationRad) *
                Math.Cos(hourAngleRad);

            double elevationRad = Math.Asin(
                Math.Clamp(sinElevation, -1.0, 1.0)
            );

            double azimuthRad = Math.Atan2(
                Math.Sin(hourAngleRad),
                Math.Cos(hourAngleRad) * Math.Sin(latRad) -
                Math.Tan(declinationRad) * Math.Cos(latRad)
            );

            double azimuthDeg =
                (azimuthRad * 180.0 / Math.PI + 180.0) % 360.0;

            elevationDegrees =
                (float)(elevationRad * 180.0 / Math.PI);
            azimuthDegrees = (float)azimuthDeg;
            return true;
        }

        private void ApplyOverlay(
            float rain,
            float snow,
            double windSpeedKmh,
            double windDirectionDeg,
            int weatherCode)
        {
            if (_overlay == null)
                return;

            float direction = Mathf.Sin(Mathf.DegToRad((float)windDirectionDeg));
            float strength = Mathf.Clamp((float)(windSpeedKmh / 45.0), 0f, 1f);
            float storm = weatherCode >= 95 && weatherCode <= 99 ? 1f : 0f;
            _overlay.Configure(rain, snow, direction * strength, storm);
        }

        private static float ParseLocalHour(string time)
        {
            if (DateTime.TryParse(
                time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime parsed))
            {
                return parsed.Hour + parsed.Minute / 60f;
            }

            return 12f;
        }

        private static string BuildWeatherLabel(
            WeatherCurrentPayload current,
            string source)
        {
            string condition = current.WeatherCode switch
            {
                0 => "CLAIR",
                1 or 2 => "ÉCLAIRCIES",
                3 => "COUVERT",
                45 or 48 => "BROUILLARD",
                >= 51 and <= 57 => "BRUINE",
                >= 61 and <= 67 => "PLUIE",
                >= 71 and <= 77 => "NEIGE",
                >= 80 and <= 82 => "AVERSES",
                >= 85 and <= 86 => "NEIGE",
                >= 95 and <= 99 => "ORAGE",
                _ => "VARIABLE"
            };

            string provider = string.IsNullOrWhiteSpace(source)
                ? "MÉTÉO RÉELLE"
                : source;

            string localClock = "--:--";
            if (DateTime.TryParse(
                current.Time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime localTime))
            {
                localClock = localTime.ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture
                );
            }

            return $"{condition}  {current.TemperatureC:F0}°C  •  " +
                   $"VENT {current.WindSpeedKmh:F0} KM/H  •  " +
                   $"{localClock}  •  {provider}";
        }
    }
}
