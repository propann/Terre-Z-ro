-- schema.sql - Terre Zéro / PostgreSQL 16 + PostGIS
-- Le GPS ancre le joueur. OSM définit l'enveloppe géographique.
-- Les intérieurs sont reconstruits de manière déterministe à partir des versions + osm_id.

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS osm_buildings (
    osm_id BIGINT PRIMARY KEY,
    geom GEOMETRY(Polygon, 4326) NOT NULL,
    h3_index VARCHAR(16) NOT NULL,
    name VARCHAR(255),
    building_type VARCHAR(64) DEFAULT 'yes',
    amenity VARCHAR(64),
    shop VARCHAR(64),
    levels INT DEFAULT 2,
    height_meters FLOAT DEFAULT 6.0,
    world_version INT NOT NULL DEFAULT 1,
    generator_version INT NOT NULL DEFAULT 3,
    generation_seed BIGINT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_buildings_geom ON osm_buildings USING GIST (geom);
CREATE INDEX IF NOT EXISTS idx_buildings_h3 ON osm_buildings (h3_index);

CREATE TABLE IF NOT EXISTS osm_roads (
    osm_id BIGINT PRIMARY KEY,
    geom GEOMETRY(LineString, 4326) NOT NULL,
    h3_index VARCHAR(16) NOT NULL,
    highway_type VARCHAR(64) NOT NULL,
    surface VARCHAR(64) DEFAULT 'asphalt',
    lanes INT DEFAULT 2
);
CREATE INDEX IF NOT EXISTS idx_roads_geom ON osm_roads USING GIST (geom);
CREATE INDEX IF NOT EXISTS idx_roads_h3 ON osm_roads (h3_index);

CREATE TABLE IF NOT EXISTS voxel_deltas (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    world_version INT NOT NULL DEFAULT 1,
    generator_version INT NOT NULL DEFAULT 3,
    h3_index VARCHAR(16) NOT NULL,
    chunk_x INT NOT NULL,
    chunk_y INT NOT NULL,
    chunk_z INT NOT NULL,
    local_x INT NOT NULL CHECK (local_x BETWEEN 0 AND 31),
    local_y INT NOT NULL CHECK (local_y BETWEEN 0 AND 31),
    local_z INT NOT NULL CHECK (local_z BETWEEN 0 AND 31),
    action VARCHAR(16) NOT NULL CHECK (action IN ('DESTROY', 'PLACE')),
    material_id SMALLINT NOT NULL CHECK (material_id BETWEEN 0 AND 255),
    player_id VARCHAR(64) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_deltas_h3 ON voxel_deltas (h3_index);
CREATE INDEX IF NOT EXISTS idx_deltas_chunk ON voxel_deltas (
    world_version,
    generator_version,
    h3_index,
    chunk_x,
    chunk_y,
    chunk_z
);

CREATE TABLE IF NOT EXISTS players (
    player_id VARCHAR(64) PRIMARY KEY,
    last_geom GEOMETRY(Point, 4326),
    h3_current VARCHAR(16),
    level INT DEFAULT 1,
    xp INT DEFAULT 0,
    inventory JSONB DEFAULT '[]'::jsonb,
    last_speed_kmh FLOAT DEFAULT 0.0,
    is_banned BOOLEAN DEFAULT FALSE,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_players_h3 ON players (h3_current);

CREATE TABLE IF NOT EXISTS bunkers (
    player_id VARCHAR(64) PRIMARY KEY REFERENCES players(player_id),
    anchor_geom GEOMETRY(Point, 4326) NOT NULL,
    energy_kw INT DEFAULT 40,
    defense_points INT DEFAULT 120,
    storage_json JSONB DEFAULT '{}'::jsonb,
    turrets_count INT DEFAULT 0,
    anchored_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS chimeres (
    id VARCHAR(32) PRIMARY KEY,
    player_id VARCHAR(64) REFERENCES players(player_id),
    type VARCHAR(32) NOT NULL,
    name VARCHAR(128) NOT NULL,
    level INT DEFAULT 1,
    current_hp INT NOT NULL,
    max_hp INT NOT NULL,
    assigned_role VARCHAR(32) DEFAULT 'None',
    captured_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
