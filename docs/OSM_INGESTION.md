# Terre Zéro — OSM ingestion pipeline

## Goal

Populate the playable world automatically from the player's validated H3 cell.

Flow:

~~~text
validated player start
      ↓
H3 resolution 9
      ↓
GET /api/v1/world/cells/{h3}
      ↓
PostGIS cache check
      ↓
small Overpass query when cache is missing/stale
      ↓
normalized osm_buildings / osm_roads
      ↓
Godot deterministic generator v4
~~~

## Cache

`osm_cell_cache` stores:

- H3 index;
- fetch timestamp;
- building count;
- road count;
- source.

Default refresh interval is 7 days.

Override:

~~~bash
export OSM_CACHE_TTL_HOURS=24
~~~

Disable automatic imports:

~~~bash
export OSM_IMPORT_DISABLED=true
~~~

## Overpass provider

Default development endpoint:

~~~text
https://overpass-api.de/api/interpreter
~~~

Override:

~~~bash
export OVERPASS_API_URL=https://your-overpass.example/api/interpreter
~~~

Set an identifying user agent when using a shared server:

~~~bash
export OSM_USER_AGENT='TerreZero-dev/contact@example.com'
~~~

The importer serializes requests with a process-wide lock and caches results. Public Overpass instances are suitable for development/small-volume use, not a production game backend at scale.

## Manual refresh

Force one cell to refresh:

~~~bash
curl -X POST http://127.0.0.1:8080/api/v1/osm/refresh/891fb466257ffff
~~~

## Query scope

The importer derives a bounding box from the H3 cell boundary and adds a small padding so crossing ways are not cut by the Overpass filter.

Current query imports:

- ways with `building=*`;
- ways with `highway=*`;
- tags;
- full way geometry.

## Normalization

Buildings store:

- OSM id;
- Polygon geometry;
- name;
- building type;
- amenity;
- shop;
- levels;
- height;
- WorldVersion;
- GeneratorVersion.

Roads store:

- OSM id;
- LineString geometry;
- highway type;
- surface;
- lanes.

## Failure mode

If Overpass is unavailable:

- existing PostGIS rows are preserved;
- the backend logs the import failure;
- the world endpoint still serves cached data;
- Godot can continue with its existing fallback behavior.

## Current limitation

Version 1 of the importer handles OSM `way` buildings and roads.

Relation/multipolygon buildings are deliberately deferred because the current Godot polygon voxelizer also handles only one outer Polygon ring. Multipolygon support should be implemented end-to-end rather than partially.

## Attribution

The in-game HUD displays:

~~~text
DONNÉES CARTO © OPENSTREETMAP CONTRIBUTORS
~~~

## Production direction

For a real deployment with many users, replace the public Overpass endpoint with one of:

- a self-hosted Overpass instance;
- regional OSM extracts plus an ingestion job;
- a commercial OSM data provider.

The Godot client does not depend on the provider. It only consumes the normalized Terre Zéro backend contract.