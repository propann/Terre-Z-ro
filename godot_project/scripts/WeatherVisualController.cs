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
            CurrentSurfaceWetness = Mathf.Clamp(
                Mathf.Max(rain, precipitation * 0.85f),
                0f,
                1f
            );
            bool isDay = current.IsDay == 1;

            ApplyEnvironment(cloud, precipitation, isDay, current.WeatherCode);
            ApplySun(cloud, isDay, current.Time);
            ApplyOverlay(rain, snow, current.WindSpeedKmh, current.WindDirectionDeg);

            _hud?.SetWeather(
                BuildWeatherLabel(current, weather.Source)
            );
        }

        private void ApplyEnvironment(
            float cloud,
            float precipitation,
            bool isDay,
            int weatherCode)
        {
            Godot.Environment env = _worldEnvironment?.Environment;
            if (env == null)
                return;

            float stormFactor = Mathf.Clamp(Mathf.Max(cloud * 0.75f, precipitation), 0f, 1f);

            env.AmbientLightEnergy = isDay
                ? Mathf.Lerp(1.0f, 0.48f, stormFactor)
                : Mathf.Lerp(0.24f, 0.12f, stormFactor);

            env.FogEnabled = true;
            env.FogDensity = Mathf.Lerp(
                isDay ? 0.006f : 0.012f,
                0.035f,
                stormFactor
            );

            env.FogLightColor = isDay
                ? new Color(
                    Mathf.Lerp(0.34f, 0.16f, stormFactor),
                    Mathf.Lerp(0.42f, 0.20f, stormFactor),
                    Mathf.Lerp(0.52f, 0.26f, stormFactor)
                )
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
            string localTime)
        {
            if (_sun == null)
                return;

            float hour = ParseLocalHour(localTime);
            float daylight = isDay
                ? Mathf.Clamp(Mathf.Sin((hour - 6f) / 12f * Mathf.Pi), 0.15f, 1f)
                : 0.06f;

            _sun.LightEnergy = isDay
                ? Mathf.Lerp(0.45f, 1.75f, daylight) * Mathf.Lerp(1f, 0.58f, cloud)
                : 0.08f;

            _sun.LightColor = isDay
                ? new Color(
                    1.0f,
                    Mathf.Lerp(0.72f, 0.94f, daylight),
                    Mathf.Lerp(0.58f, 0.86f, daylight)
                )
                : new Color(0.28f, 0.36f, 0.55f);

            float sunPitch = Mathf.Lerp(-8f, -68f, daylight);
            float sunYaw = Mathf.Lerp(-110f, 110f, Mathf.Clamp((hour - 6f) / 12f, 0f, 1f));
            _sun.RotationDegrees = new Vector3(sunPitch, sunYaw, 0f);
        }

        private void ApplyOverlay(
            float rain,
            float snow,
            double windSpeedKmh,
            double windDirectionDeg)
        {
            if (_overlay == null)
                return;

            float direction = Mathf.Sin(Mathf.DegToRad((float)windDirectionDeg));
            float strength = Mathf.Clamp((float)(windSpeedKmh / 45.0), 0f, 1f);
            _overlay.Configure(rain, snow, direction * strength);
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

            return $"{condition}  {current.TemperatureC:F0}°C  •  VENT {current.WindSpeedKmh:F0} KM/H  •  {provider}";
        }
    }
}
