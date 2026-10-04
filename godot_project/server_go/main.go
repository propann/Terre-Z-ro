package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"log"
	"math"
	"net/http"
	"os"
	"regexp"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
	_ "github.com/jackc/pgx/v5/stdlib"
	h3 "github.com/uber/h3-go/v4"
)

const (
	maxMessageSize  = 16 * 1024
	worldVersion    = 1
	generatorVersion = 4
)

var h3Pattern = regexp.MustCompile("^[0-9a-fA-F]{15,16}$")

type VoxelDelta struct {
	WorldVersion     int       `json:"world_version"`
	GeneratorVersion int       `json:"generator_version"`
	H3Index          string    `json:"h3_index"`
	ChunkCoord [3]int    `json:"chunk_coords"`
	LocalVoxel [3]int    `json:"local_voxel"`
	Action     string    `json:"action"`
	MaterialID byte      `json:"material_id"`
	PlayerID   string    `json:"player_id"`
	Timestamp  time.Time `json:"timestamp"`
}

type spatialMessage struct {
	Type string `json:"type,omitempty"`
	VoxelDelta
}

type DeltaStore interface {
	Save(context.Context, VoxelDelta) error
	List(context.Context, string) ([]VoxelDelta, error)
	Close() error
	Name() string
}

type memoryStore struct {
	mu     sync.RWMutex
	deltas map[string][]VoxelDelta
}

func newMemoryStore() *memoryStore {
	return &memoryStore{deltas: make(map[string][]VoxelDelta)}
}

func (s *memoryStore) Save(_ context.Context, delta VoxelDelta) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.deltas[delta.H3Index] = append(s.deltas[delta.H3Index], delta)
	return nil
}

func (s *memoryStore) List(_ context.Context, h3 string) ([]VoxelDelta, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	source := s.deltas[h3]
	out := make([]VoxelDelta, 0, len(source))
	for _, delta := range source {
		if delta.WorldVersion == worldVersion &&
			delta.GeneratorVersion == generatorVersion {
			out = append(out, delta)
		}
	}
	return out, nil
}

func (s *memoryStore) Close() error { return nil }
func (s *memoryStore) Name() string { return "memory" }

type WorldBuilding struct {
	OSMID          int64           `json:"osm_id"`
	Name           string          `json:"name,omitempty"`
	BuildingType   string          `json:"building_type"`
	Amenity        string          `json:"amenity,omitempty"`
	Shop           string          `json:"shop,omitempty"`
	Levels         int             `json:"levels"`
	HeightMeters   float64         `json:"height_meters"`
	WorldVersion   int             `json:"world_version"`
	GeneratorVersion int           `json:"generator_version"`
	Geometry       json.RawMessage `json:"geometry"`
}

type WorldRoad struct {
	OSMID       int64           `json:"osm_id"`
	HighwayType string          `json:"highway_type"`
	Surface     string          `json:"surface"`
	Lanes       int             `json:"lanes"`
	Geometry    json.RawMessage `json:"geometry"`
}

type WorldCell struct {
	H3Index   string          `json:"h3_index"`
	Buildings []WorldBuilding `json:"buildings"`
	Roads     []WorldRoad     `json:"roads"`
}

type StartLocationRequest struct {
	MachineLatitude  float64 `json:"machine_latitude"`
	MachineLongitude float64 `json:"machine_longitude"`
	StartLatitude    float64 `json:"start_latitude"`
	StartLongitude   float64 `json:"start_longitude"`
	RadiusKm         float64 `json:"radius_km"`
}

type StartLocationResponse struct {
	Allowed    bool    `json:"allowed"`
	DistanceKm float64 `json:"distance_km"`
	RadiusKm   float64 `json:"radius_km"`
	H3Index    string  `json:"h3_index,omitempty"`
	Latitude   float64 `json:"latitude,omitempty"`
	Longitude  float64 `json:"longitude,omitempty"`
}

type postgresStore struct {
	db *sql.DB
}

func newPostgresStore(ctx context.Context, url string) (*postgresStore, error) {
	db, err := sql.Open("pgx", url)
	if err != nil {
		return nil, err
	}
	if err := db.PingContext(ctx); err != nil {
		_ = db.Close()
		return nil, err
	}
	return &postgresStore{db: db}, nil
}

func (s *postgresStore) Save(ctx context.Context, d VoxelDelta) error {
	_, err := s.db.ExecContext(ctx, `
		INSERT INTO voxel_deltas (
			world_version, generator_version,
			h3_index, chunk_x, chunk_y, chunk_z,
			local_x, local_y, local_z,
			action, material_id, player_id, created_at
		) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13)`,
		d.WorldVersion, d.GeneratorVersion,
		d.H3Index,
		d.ChunkCoord[0], d.ChunkCoord[1], d.ChunkCoord[2],
		d.LocalVoxel[0], d.LocalVoxel[1], d.LocalVoxel[2],
		d.Action, d.MaterialID, d.PlayerID, d.Timestamp,
	)
	return err
}

func (s *postgresStore) List(ctx context.Context, h3 string) ([]VoxelDelta, error) {
	rows, err := s.db.QueryContext(ctx, `
		SELECT world_version, generator_version,
		       h3_index, chunk_x, chunk_y, chunk_z,
		       local_x, local_y, local_z,
		       action, material_id, player_id, created_at
		FROM voxel_deltas
		WHERE h3_index = $1
		  AND world_version = $2
		  AND generator_version = $3
		ORDER BY created_at ASC`, h3, worldVersion, generatorVersion)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var out []VoxelDelta
	for rows.Next() {
		var d VoxelDelta
		if err := rows.Scan(
			&d.WorldVersion,
			&d.GeneratorVersion,
			&d.H3Index,
			&d.ChunkCoord[0], &d.ChunkCoord[1], &d.ChunkCoord[2],
			&d.LocalVoxel[0], &d.LocalVoxel[1], &d.LocalVoxel[2],
			&d.Action, &d.MaterialID, &d.PlayerID, &d.Timestamp,
		); err != nil {
			return nil, err
		}
		out = append(out, d)
	}
	return out, rows.Err()
}

func (s *postgresStore) LoadWorldCell(ctx context.Context, h3 string) (WorldCell, error) {
	cell := WorldCell{
		H3Index:   h3,
		Buildings: []WorldBuilding{},
		Roads:     []WorldRoad{},
	}

	buildingRows, err := s.db.QueryContext(ctx, `
		SELECT osm_id,
		       COALESCE(name, ''),
		       COALESCE(building_type, 'yes'),
		       COALESCE(amenity, ''),
		       COALESCE(shop, ''),
		       COALESCE(levels, 2),
		       COALESCE(height_meters, 6.0),
		       COALESCE(world_version, 1),
		       COALESCE(generator_version, 4),
		       ST_AsGeoJSON(geom)
		FROM osm_buildings
		WHERE h3_index = $1
		ORDER BY osm_id ASC`, h3)
	if err != nil {
		return cell, err
	}

	for buildingRows.Next() {
		var building WorldBuilding
		var geoJSON string
		if err := buildingRows.Scan(
			&building.OSMID,
			&building.Name,
			&building.BuildingType,
			&building.Amenity,
			&building.Shop,
			&building.Levels,
			&building.HeightMeters,
			&building.WorldVersion,
			&building.GeneratorVersion,
			&geoJSON,
		); err != nil {
			buildingRows.Close()
			return cell, err
		}
		building.Geometry = json.RawMessage(geoJSON)
		cell.Buildings = append(cell.Buildings, building)
	}
	if err := buildingRows.Close(); err != nil {
		return cell, err
	}
	if err := buildingRows.Err(); err != nil {
		return cell, err
	}

	roadRows, err := s.db.QueryContext(ctx, `
		SELECT osm_id,
		       highway_type,
		       COALESCE(surface, 'asphalt'),
		       COALESCE(lanes, 2),
		       ST_AsGeoJSON(geom)
		FROM osm_roads
		WHERE h3_index = $1
		ORDER BY osm_id ASC`, h3)
	if err != nil {
		return cell, err
	}

	for roadRows.Next() {
		var road WorldRoad
		var geoJSON string
		if err := roadRows.Scan(
			&road.OSMID,
			&road.HighwayType,
			&road.Surface,
			&road.Lanes,
			&geoJSON,
		); err != nil {
			roadRows.Close()
			return cell, err
		}
		road.Geometry = json.RawMessage(geoJSON)
		cell.Roads = append(cell.Roads, road)
	}
	if err := roadRows.Close(); err != nil {
		return cell, err
	}
	if err := roadRows.Err(); err != nil {
		return cell, err
	}

	return cell, nil
}

func (s *postgresStore) Close() error { return s.db.Close() }
func (s *postgresStore) Name() string { return "postgres" }

type H3Hub struct {
	mu          sync.RWMutex
	subscribers map[string]map[*websocket.Conn]struct{}
}

func newHub() *H3Hub {
	return &H3Hub{subscribers: make(map[string]map[*websocket.Conn]struct{})}
}

func (h *H3Hub) subscribe(cell string, conn *websocket.Conn) {
	h.mu.Lock()
	defer h.mu.Unlock()
	if h.subscribers[cell] == nil {
		h.subscribers[cell] = make(map[*websocket.Conn]struct{})
	}
	h.subscribers[cell][conn] = struct{}{}
}

func (h *H3Hub) unsubscribe(cell string, conn *websocket.Conn) {
	if cell == "" {
		return
	}
	h.mu.Lock()
	defer h.mu.Unlock()
	delete(h.subscribers[cell], conn)
	if len(h.subscribers[cell]) == 0 {
		delete(h.subscribers, cell)
	}
}

func (h *H3Hub) broadcast(cell string, sender *websocket.Conn, payload []byte) {
	h.mu.RLock()
	clients := make([]*websocket.Conn, 0, len(h.subscribers[cell]))
	for client := range h.subscribers[cell] {
		if client != sender {
			clients = append(clients, client)
		}
	}
	h.mu.RUnlock()

	for _, client := range clients {
		if err := client.WriteMessage(websocket.TextMessage, payload); err != nil {
			log.Printf("broadcast %s: %v", cell, err)
		}
	}
}

var (
	hub   = newHub()
	store DeltaStore
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		allowed := strings.TrimSpace(os.Getenv("ALLOWED_ORIGINS"))
		if allowed == "" {
			return true
		}
		origin := r.Header.Get("Origin")
		for _, candidate := range strings.Split(allowed, ",") {
			if strings.TrimSpace(candidate) == origin {
				return true
			}
		}
		return false
	},
}

func main() {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	store = initStore(ctx)
	defer store.Close()

	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/api/v1/health", handleHealth)
	mux.HandleFunc("/api/v1/cells/", handleGetCellDeltas)
	mux.HandleFunc("/api/v1/world/cells/", handleGetWorldCell)
	mux.HandleFunc("/api/v1/spatial/cell", handleResolveSpatialCell)
	mux.HandleFunc("/api/v1/spatial/start", handleValidateStartLocation)
	mux.HandleFunc("/api/v1/weather", handleWeather)
	mux.HandleFunc("/ws/spatial", handleSpatialWebSocket)

	server := &http.Server{
		Addr:              ":" + port,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}

	log.Printf("[TERRE ZÉRO] backend :%s store=%s", port, store.Name())
	if err := server.ListenAndServe(); !errors.Is(err, http.ErrServerClosed) {
		log.Fatal(err)
	}
}

func initStore(ctx context.Context) DeltaStore {
	databaseURL := strings.TrimSpace(os.Getenv("DATABASE_URL"))
	if databaseURL == "" {
		log.Printf("[TERRE ZÉRO] DATABASE_URL absent: persistance mémoire")
		return newMemoryStore()
	}
	s, err := newPostgresStore(ctx, databaseURL)
	if err != nil {
		log.Printf("[TERRE ZÉRO] PostgreSQL indisponible (%v), fallback mémoire", err)
		return newMemoryStore()
	}
	return s
}

func handleHealth(w http.ResponseWriter, _ *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{
		"status":  "healthy",
		"service": "terre-zero-backend",
		"version": "0.3.0",
		"store":   store.Name(),
	})
}

func handleGetCellDeltas(w http.ResponseWriter, r *http.Request) {
	h3 := strings.TrimSpace(strings.TrimPrefix(r.URL.Path, "/api/v1/cells/"))
	if !validH3(h3) {
		http.Error(w, "index H3 invalide", http.StatusBadRequest)
		return
	}
	deltas, err := store.List(r.Context(), h3)
	if err != nil {
		http.Error(w, "lecture des deltas impossible", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{
		"h3_index": h3,
		"count":    len(deltas),
		"deltas":   deltas,
	})
}

func handleResolveSpatialCell(w http.ResponseWriter, r *http.Request) {
	lat, err := strconv.ParseFloat(r.URL.Query().Get("lat"), 64)
	if err != nil || lat < -90 || lat > 90 {
		http.Error(w, "latitude invalide", http.StatusBadRequest)
		return
	}

	lon, err := strconv.ParseFloat(r.URL.Query().Get("lon"), 64)
	if err != nil || lon < -180 || lon > 180 {
		http.Error(w, "longitude invalide", http.StatusBadRequest)
		return
	}

	cell, err := h3.LatLngToCell(h3.NewLatLng(lat, lon), 9)
	if err != nil {
		http.Error(w, "conversion H3 impossible", http.StatusInternalServerError)
		return
	}

	writeJSON(w, http.StatusOK, map[string]any{
		"h3_index":   cell.String(),
		"resolution": 9,
		"latitude":   lat,
		"longitude":  lon,
	})
}

func handleValidateStartLocation(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "méthode non autorisée", http.StatusMethodNotAllowed)
		return
	}

	var request StartLocationRequest
	if err := json.NewDecoder(r.Body).Decode(&request); err != nil {
		http.Error(w, "requête invalide", http.StatusBadRequest)
		return
	}

	if request.MachineLatitude < -90 || request.MachineLatitude > 90 ||
		request.StartLatitude < -90 || request.StartLatitude > 90 ||
		request.MachineLongitude < -180 || request.MachineLongitude > 180 ||
		request.StartLongitude < -180 || request.StartLongitude > 180 {
		http.Error(w, "coordonnées invalides", http.StatusBadRequest)
		return
	}

	if request.RadiusKm != 5 && request.RadiusKm != 10 {
		http.Error(w, "rayon autorisé: 5 ou 10 km", http.StatusBadRequest)
		return
	}

	distanceKm := haversineKm(
		request.MachineLatitude,
		request.MachineLongitude,
		request.StartLatitude,
		request.StartLongitude,
	)

	response := StartLocationResponse{
		Allowed:    distanceKm <= request.RadiusKm,
		DistanceKm: distanceKm,
		RadiusKm:   request.RadiusKm,
	}

	if !response.Allowed {
		writeJSON(w, http.StatusOK, response)
		return
	}

	cell, err := h3.LatLngToCell(
		h3.NewLatLng(request.StartLatitude, request.StartLongitude),
		9,
	)
	if err != nil {
		http.Error(w, "conversion H3 impossible", http.StatusInternalServerError)
		return
	}

	response.H3Index = cell.String()
	response.Latitude = request.StartLatitude
	response.Longitude = request.StartLongitude
	writeJSON(w, http.StatusOK, response)
}

func haversineKm(lat1, lon1, lat2, lon2 float64) float64 {
	const earthRadiusKm = 6371.0088

	lat1Rad := lat1 * 3.141592653589793 / 180
	lat2Rad := lat2 * 3.141592653589793 / 180
	dLat := (lat2 - lat1) * 3.141592653589793 / 180
	dLon := (lon2 - lon1) * 3.141592653589793 / 180

	a := sinSquared(dLat/2) +
		cosine(lat1Rad)*cosine(lat2Rad)*sinSquared(dLon/2)

	return 2 * earthRadiusKm * arcsineSqrt(a)
}

func sinSquared(value float64) float64 {
	s := sine(value)
	return s * s
}

func sine(value float64) float64 {
	return math.Sin(value)
}

func cosine(value float64) float64 {
	return math.Cos(value)
}

func arcsineSqrt(value float64) float64 {
	return math.Asin(math.Sqrt(value))
}

func handleGetWorldCell(w http.ResponseWriter, r *http.Request) {
	h3 := strings.TrimSpace(strings.TrimPrefix(r.URL.Path, "/api/v1/world/cells/"))
	if !validH3(h3) {
		http.Error(w, "index H3 invalide", http.StatusBadRequest)
		return
	}

	pg, ok := store.(*postgresStore)
	if !ok {
		writeJSON(w, http.StatusOK, WorldCell{
			H3Index:   h3,
			Buildings: []WorldBuilding{},
			Roads:     []WorldRoad{},
		})
		return
	}

	cell, err := pg.LoadWorldCell(r.Context(), h3)
	if err != nil {
		log.Printf("world cell %s: %v", h3, err)
		http.Error(w, "lecture de la cellule monde impossible", http.StatusInternalServerError)
		return
	}

	writeJSON(w, http.StatusOK, cell)
}

func handleSpatialWebSocket(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("websocket upgrade: %v", err)
		return
	}
	defer conn.Close()
	conn.SetReadLimit(maxMessageSize)

	var currentCell string
	defer func() { hub.unsubscribe(currentCell, conn) }()

	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			return
		}

		var message spatialMessage
		if err := json.Unmarshal(payload, &message); err != nil {
			writeWSError(conn, "invalid_json")
			continue
		}

		if message.Type == "subscribe" {
			if !validH3(message.H3Index) {
				writeWSError(conn, "invalid_h3")
				continue
			}
			hub.unsubscribe(currentCell, conn)
			currentCell = message.H3Index
			hub.subscribe(currentCell, conn)
			_ = conn.WriteJSON(map[string]string{"type": "subscribed", "h3_index": currentCell})
			continue
		}

		delta := message.VoxelDelta
		if err := validateDelta(delta); err != nil {
			writeWSError(conn, err.Error())
			continue
		}
		if currentCell != delta.H3Index {
			hub.unsubscribe(currentCell, conn)
			currentCell = delta.H3Index
			hub.subscribe(currentCell, conn)
		}

		delta.Timestamp = time.Now().UTC()
		if err := store.Save(r.Context(), delta); err != nil {
			writeWSError(conn, "persistence_failed")
			continue
		}

		out, _ := json.Marshal(spatialMessage{Type: "delta", VoxelDelta: delta})
		hub.broadcast(currentCell, conn, out)
		_ = conn.WriteJSON(map[string]any{"type": "ack", "timestamp": delta.Timestamp})
	}
}

func validateDelta(d VoxelDelta) error {
	if d.WorldVersion != worldVersion {
		return errors.New("invalid_world_version")
	}
	if d.GeneratorVersion != generatorVersion {
		return errors.New("invalid_generator_version")
	}
	if !validH3(d.H3Index) {
		return errors.New("invalid_h3")
	}
	if d.Action != "DESTROY" && d.Action != "PLACE" {
		return errors.New("invalid_action")
	}
	if d.PlayerID == "" || len(d.PlayerID) > 64 {
		return errors.New("invalid_player")
	}
	for _, v := range d.LocalVoxel {
		if v < 0 || v >= 32 {
			return errors.New("invalid_local_voxel")
		}
	}
	return nil
}

func validH3(value string) bool {
	return h3Pattern.MatchString(value)
}

func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}

func writeWSError(conn *websocket.Conn, code string) {
	_ = conn.WriteJSON(map[string]string{"type": "error", "code": code})
}
