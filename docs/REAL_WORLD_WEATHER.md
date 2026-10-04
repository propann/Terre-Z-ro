# Terre Zéro — Real-world weather and atmosphere

## Principle

Weather uses exactly the same fixed real-world anchor as the generated world.

~~~text
machine approximate point
        ↓
player-selected start (5/10 km validation)
        ↓
fixed latitude/longitude anchor
        ├── H3 world
        └── real weather
~~~

## Backend contract

Endpoint:

~~~text
GET /api/v1/weather?lat=<latitude>&lon=<longitude>
~~~

Current fields include temperature, apparent temperature, humidity, precipitation, rain, showers, snowfall, cloud cover, weather code, wind, gusts, day/night and provider-local time.

The backend currently uses Open-Meteo behind a replaceable adapter and caches a coordinate result for ten minutes. `WEATHER_API_BASE_URL` can override the provider URL for testing or a future provider.

## Godot rendering

`WeatherVisualController` controls:

- procedural sky colors;
- ambient light;
- directional sun intensity/color/direction;
- fog density and color;
- rain;
- snow;
- wind slant;
- thunder flashes;
- voxel surface wetness;
- HUD condition/temperature/wind/provider attribution.

These are visual effects only and do not change deterministic world generation.

## Wet surfaces

Rain creates a 0..1 wetness value. Voxel chunks lower roughness, slightly increase metallic response and darken while wet. Wetness survives chunk rebuilds.

## Scanner coexistence

X-Ray uses an emissive orange voxel treatment while preserving the current weather wetness internally. Switching X-Ray off restores the current wet appearance.

## Failure mode

Weather never blocks world loading. On provider/backend failure the existing environment remains valid, HUD reports weather unavailable and a later refresh retries.

## Refresh

- backend cache: 10 minutes;
- Godot refresh: 10 minutes;
- validated new start: immediate refresh.

## Privacy

Desktop machine location is queried only after explicit consent. Weather uses the selected world anchor and does not continuously track the desktop position.