package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"os"
	"regexp"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
	_ "github.com/jackc/pgx/v5/stdlib"
)

const maxMessageSize = 16 * 1024

var h3Pattern = regexp.MustCompile("^[0-9a-fA-F]{15,16}$")

type VoxelDelta struct {
	H3Index    string    `json:"h3_index"`
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
	out := append([]VoxelDelta(nil), s.deltas[h3]...)
	return out, nil
}

func (s *memoryStore) Close() error { return nil }
func (s *memoryStore) Name() string { return "memory" }

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
			h3_index, chunk_x, chunk_y, chunk_z,
			local_x, local_y, local_z,
			action, material_id, player_id, created_at
		) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11)`,
		d.H3Index,
		d.ChunkCoord[0], d.ChunkCoord[1], d.ChunkCoord[2],
		d.LocalVoxel[0], d.LocalVoxel[1], d.LocalVoxel[2],
		d.Action, d.MaterialID, d.PlayerID, d.Timestamp,
	)
	return err
}

func (s *postgresStore) List(ctx context.Context, h3 string) ([]VoxelDelta, error) {
	rows, err := s.db.QueryContext(ctx, `
		SELECT h3_index, chunk_x, chunk_y, chunk_z,
		       local_x, local_y, local_z,
		       action, material_id, player_id, created_at
		FROM voxel_deltas
		WHERE h3_index = $1
		ORDER BY created_at ASC`, h3)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var out []VoxelDelta
	for rows.Next() {
		var d VoxelDelta
		if err := rows.Scan(
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
		"version": "0.2.0",
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
