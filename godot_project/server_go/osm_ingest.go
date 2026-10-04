package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
	"strconv"
	"strings"
	"sync"
	"time"

	h3 "github.com/uber/h3-go/v4"
)

const defaultOverpassURL = "https://overpass-api.de/api/interpreter"

var (
	osmImportMu    sync.Mutex
	overpassClient = &http.Client{Timeout: 25 * time.Second}
)

type geoBounds struct {
	South float64
	West  float64
	North float64
	East  float64
}

type overpassResponse struct {
	Elements []overpassElement `json:"elements"`
}

type overpassElement struct {
	Type     string            `json:"type"`
	ID       int64             `json:"id"`
	Tags     map[string]string `json:"tags"`
	Geometry []overpassPoint   `json:"geometry"`
}

type overpassPoint struct {
	Lat float64 `json:"lat"`
	Lon float64 `json:"lon"`
}

type importedBuilding struct {
	OSMID        int64
	Name         string
	BuildingType string
	Amenity      string
	Shop         string
	Levels       int
	HeightMeters float64
	GeoJSON      string
}

type importedRoad struct {
	OSMID       int64
	HighwayType string
	Surface     string
	Lanes       int
	GeoJSON     string
}

type parsedOSMCell struct {
	Buildings []importedBuilding
	Roads     []importedRoad
}

func ensureOSMCacheSchema(
	ctx context.Context,
	db *sql.DB,
) error {
	_, err := db.ExecContext(
		ctx,
		`CREATE TABLE IF NOT EXISTS osm_cell_cache (
			h3_index VARCHAR(16) PRIMARY KEY,
			fetched_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
			building_count INT NOT NULL DEFAULT 0,
			road_count INT NOT NULL DEFAULT 0,
			source VARCHAR(64) NOT NULL DEFAULT 'overpass'
		)`,
	)
	return err
}

func (s *postgresStore) EnsureWorldCell(
	ctx context.Context,
	h3Index string,
	force bool,
) error {
	if strings.EqualFold(strings.TrimSpace(os.Getenv("OSM_IMPORT_DISABLED")), "true") {
		return nil
	}

	osmImportMu.Lock()
	defer osmImportMu.Unlock()

	fresh, err := s.isOSMCellFresh(ctx, h3Index)
	if err != nil {
		return err
	}
	if fresh && !force {
		return nil
	}

	bounds, err := h3CellBounds(h3Index)
	if err != nil {
		return err
	}

	query := buildOverpassQuery(bounds)
	payload, err := fetchOverpass(ctx, query)
	if err != nil {
		return err
	}

	parsed, err := parseOverpass(payload)
	if err != nil {
		return err
	}

	return s.replaceOSMCell(ctx, h3Index, parsed)
}

func (s *postgresStore) isOSMCellFresh(
	ctx context.Context,
	h3Index string,
) (bool, error) {
	var fetchedAt time.Time
	err := s.db.QueryRowContext(
		ctx,
		`SELECT fetched_at
		   FROM osm_cell_cache
		  WHERE h3_index = $1`,
		h3Index,
	).Scan(&fetchedAt)

	if errors.Is(err, sql.ErrNoRows) {
		return false, nil
	}
	if err != nil {
		return false, err
	}

	return time.Since(fetchedAt) < osmCacheTTL(), nil
}

func osmCacheTTL() time.Duration {
	raw := strings.TrimSpace(os.Getenv("OSM_CACHE_TTL_HOURS"))
	if raw == "" {
		return 7 * 24 * time.Hour
	}

	hours, err := strconv.Atoi(raw)
	if err != nil || hours < 1 {
		return 7 * 24 * time.Hour
	}

	return time.Duration(hours) * time.Hour
}

func h3CellBounds(index string) (geoBounds, error) {
	cell := h3.CellFromString(index)
	if !cell.IsValid() {
		return geoBounds{}, fmt.Errorf("invalid H3 cell: %s", index)
	}

	boundary, err := cell.Boundary()
	if err != nil {
		return geoBounds{}, err
	}
	if len(boundary) == 0 {
		return geoBounds{}, fmt.Errorf("empty H3 boundary: %s", index)
	}

	bounds := geoBounds{
		South: boundary[0].Lat,
		North: boundary[0].Lat,
		West:  boundary[0].Lng,
		East:  boundary[0].Lng,
	}

	for _, point := range boundary[1:] {
		if point.Lat < bounds.South {
			bounds.South = point.Lat
		}
		if point.Lat > bounds.North {
			bounds.North = point.Lat
		}
		if point.Lng < bounds.West {
			bounds.West = point.Lng
		}
		if point.Lng > bounds.East {
			bounds.East = point.Lng
		}
	}

	// A small pad keeps crossing ways/buildings from being clipped by the
	// rectangular Overpass filter. We still cache everything under one H3 cell.
	latPad := (bounds.North - bounds.South) * 0.15
	lonPad := (bounds.East - bounds.West) * 0.15
	bounds.South -= latPad
	bounds.North += latPad
	bounds.West -= lonPad
	bounds.East += lonPad

	return bounds, nil
}

func buildOverpassQuery(bounds geoBounds) string {
	bbox := fmt.Sprintf(
		"%.7f,%.7f,%.7f,%.7f",
		bounds.South,
		bounds.West,
		bounds.North,
		bounds.East,
	)

	return fmt.Sprintf(
		"[out:json][timeout:20];"+
			"(way[\"building\"](%s);"+
			"way[\"highway\"](%s););"+
			"out tags geom;",
		bbox,
		bbox,
	)
}

func fetchOverpass(ctx context.Context, query string) ([]byte, error) {
	endpoint := strings.TrimSpace(os.Getenv("OVERPASS_API_URL"))
	if endpoint == "" {
		endpoint = defaultOverpassURL
	}

	form := url.Values{}
	form.Set("data", query)

	req, err := http.NewRequestWithContext(
		ctx,
		http.MethodPost,
		endpoint,
		strings.NewReader(form.Encode()),
	)
	if err != nil {
		return nil, err
	}

	req.Header.Set(
		"Content-Type",
		"application/x-www-form-urlencoded",
	)

	userAgent := strings.TrimSpace(os.Getenv("OSM_USER_AGENT"))
	if userAgent == "" {
		userAgent = "TerreZero/0.4 (+https://github.com/propann/Terre-Z-ro)"
	}
	req.Header.Set("User-Agent", userAgent)

	resp, err := overpassClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(io.LimitReader(resp.Body, 2048))
		return nil, fmt.Errorf(
			"overpass status %d: %s",
			resp.StatusCode,
			strings.TrimSpace(string(body)),
		)
	}

	return io.ReadAll(io.LimitReader(resp.Body, 8<<20))
}

func parseOverpass(data []byte) (parsedOSMCell, error) {
	var response overpassResponse
	if err := json.Unmarshal(data, &response); err != nil {
		return parsedOSMCell{}, err
	}

	result := parsedOSMCell{
		Buildings: []importedBuilding{},
		Roads:     []importedRoad{},
	}

	for _, element := range response.Elements {
		if element.Type != "way" || len(element.Geometry) < 2 {
			continue
		}

		if buildingType := strings.TrimSpace(element.Tags["building"]); buildingType != "" {
			geoJSON, ok := polygonGeoJSON(element.Geometry)
			if !ok {
				continue
			}

			levels := parsePositiveInt(element.Tags["building:levels"], 2)
			height := parseMeters(element.Tags["height"])
			if height <= 0 {
				height = float64(levels) * 3.0
			}

			result.Buildings = append(
				result.Buildings,
				importedBuilding{
					OSMID:        element.ID,
					Name:         strings.TrimSpace(element.Tags["name"]),
					BuildingType: buildingType,
					Amenity:      strings.TrimSpace(element.Tags["amenity"]),
					Shop:         strings.TrimSpace(element.Tags["shop"]),
					Levels:       levels,
					HeightMeters: height,
					GeoJSON:      geoJSON,
				},
			)
			continue
		}

		if highwayType := strings.TrimSpace(element.Tags["highway"]); highwayType != "" {
			geoJSON, ok := lineGeoJSON(element.Geometry)
			if !ok {
				continue
			}

			surface := strings.TrimSpace(element.Tags["surface"])
			if surface == "" {
				surface = "asphalt"
			}

			result.Roads = append(
				result.Roads,
				importedRoad{
					OSMID:       element.ID,
					HighwayType: highwayType,
					Surface:     surface,
					Lanes:       parsePositiveInt(element.Tags["lanes"], 2),
					GeoJSON:     geoJSON,
				},
			)
		}
	}

	return result, nil
}

func polygonGeoJSON(points []overpassPoint) (string, bool) {
	if len(points) < 3 {
		return "", false
	}

	coordinates := make([][]float64, 0, len(points)+1)
	for _, point := range points {
		coordinates = append(
			coordinates,
			[]float64{point.Lon, point.Lat},
		)
	}

	first := coordinates[0]
	last := coordinates[len(coordinates)-1]
	if first[0] != last[0] || first[1] != last[1] {
		coordinates = append(
			coordinates,
			[]float64{first[0], first[1]},
		)
	}

	if len(coordinates) < 4 {
		return "", false
	}

	raw, err := json.Marshal(map[string]any{
		"type":        "Polygon",
		"coordinates": []any{coordinates},
	})
	if err != nil {
		return "", false
	}

	return string(raw), true
}

func lineGeoJSON(points []overpassPoint) (string, bool) {
	if len(points) < 2 {
		return "", false
	}

	coordinates := make([][]float64, 0, len(points))
	for _, point := range points {
		coordinates = append(
			coordinates,
			[]float64{point.Lon, point.Lat},
		)
	}

	raw, err := json.Marshal(map[string]any{
		"type":        "LineString",
		"coordinates": coordinates,
	})
	if err != nil {
		return "", false
	}

	return string(raw), true
}

func parsePositiveInt(value string, fallback int) int {
	value = strings.TrimSpace(value)
	if value == "" {
		return fallback
	}

	if separator := strings.IndexAny(value, ";,"); separator >= 0 {
		value = value[:separator]
	}

	parsed, err := strconv.Atoi(strings.TrimSpace(value))
	if err != nil || parsed <= 0 {
		return fallback
	}
	return parsed
}

func parseMeters(value string) float64 {
	value = strings.TrimSpace(strings.ToLower(value))
	if value == "" {
		return 0
	}

	value = strings.TrimSuffix(value, "meters")
	value = strings.TrimSuffix(value, "meter")
	value = strings.TrimSuffix(value, "metres")
	value = strings.TrimSuffix(value, "metre")
	value = strings.TrimSuffix(value, "m")
	value = strings.TrimSpace(value)

	parsed, err := strconv.ParseFloat(value, 64)
	if err != nil || parsed <= 0 {
		return 0
	}

	return parsed
}

func (s *postgresStore) replaceOSMCell(
	ctx context.Context,
	h3Index string,
	cell parsedOSMCell,
) error {
	tx, err := s.db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(
		ctx,
		`DELETE FROM osm_buildings WHERE h3_index = $1`,
		h3Index,
	); err != nil {
		return err
	}

	if _, err := tx.ExecContext(
		ctx,
		`DELETE FROM osm_roads WHERE h3_index = $1`,
		h3Index,
	); err != nil {
		return err
	}

	for _, building := range cell.Buildings {
		_, err := tx.ExecContext(
			ctx,
			`INSERT INTO osm_buildings (
				osm_id, geom, h3_index, name, building_type,
				amenity, shop, levels, height_meters,
				world_version, generator_version, generation_seed
			) VALUES (
				$1,
				ST_SetSRID(ST_GeomFromGeoJSON($2), 4326),
				$3,$4,$5,$6,$7,$8,$9,$10,$11,$12
			)
			ON CONFLICT (osm_id) DO UPDATE SET
				geom = EXCLUDED.geom,
				h3_index = EXCLUDED.h3_index,
				name = EXCLUDED.name,
				building_type = EXCLUDED.building_type,
				amenity = EXCLUDED.amenity,
				shop = EXCLUDED.shop,
				levels = EXCLUDED.levels,
				height_meters = EXCLUDED.height_meters,
				world_version = EXCLUDED.world_version,
				generator_version = EXCLUDED.generator_version,
				generation_seed = EXCLUDED.generation_seed`,
			building.OSMID,
			building.GeoJSON,
			h3Index,
			building.Name,
			building.BuildingType,
			building.Amenity,
			building.Shop,
			building.Levels,
			building.HeightMeters,
			worldVersion,
			generatorVersion,
			building.OSMID,
		)
		if err != nil {
			return err
		}
	}

	for _, road := range cell.Roads {
		_, err := tx.ExecContext(
			ctx,
			`INSERT INTO osm_roads (
				osm_id, geom, h3_index, highway_type, surface, lanes
			) VALUES (
				$1,
				ST_SetSRID(ST_GeomFromGeoJSON($2), 4326),
				$3,$4,$5,$6
			)
			ON CONFLICT (osm_id) DO UPDATE SET
				geom = EXCLUDED.geom,
				h3_index = EXCLUDED.h3_index,
				highway_type = EXCLUDED.highway_type,
				surface = EXCLUDED.surface,
				lanes = EXCLUDED.lanes`,
			road.OSMID,
			road.GeoJSON,
			h3Index,
			road.HighwayType,
			road.Surface,
			road.Lanes,
		)
		if err != nil {
			return err
		}
	}

	if _, err := tx.ExecContext(
		ctx,
		`INSERT INTO osm_cell_cache (
			h3_index, fetched_at, building_count, road_count, source
		) VALUES ($1, NOW(), $2, $3, 'overpass')
		ON CONFLICT (h3_index) DO UPDATE SET
			fetched_at = EXCLUDED.fetched_at,
			building_count = EXCLUDED.building_count,
			road_count = EXCLUDED.road_count,
			source = EXCLUDED.source`,
		h3Index,
		len(cell.Buildings),
		len(cell.Roads),
	); err != nil {
		return err
	}

	return tx.Commit()
}
