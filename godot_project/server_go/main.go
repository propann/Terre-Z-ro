// main.go - Serveur Backend Go pour Terre Zéro
// Spécification GDD : Faible empreinte mémoire, haute concurrence, requêtes PostGIS et streaming H3.

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

// Structure d'un Delta Voxel reçu du client
type VoxelDelta struct {
	H3Index    string    `json:"h3_index"`
	ChunkCoord [3]int    `json:"chunk_coords"`
	LocalVoxel [3]int    `json:"local_voxel"`
	Action     string    `json:"action"` // "DESTROY" ou "PLACE"
	MaterialID byte      `json:"material_id"`
	PlayerID   string    `json:"player_id"`
	Timestamp  time.Time `json:"timestamp"`
}

// Hub de WebSocket pour la diffusion temps réel par cellule H3
type H3Hub struct {
	mu          sync.RWMutex
	subscribers map[string]map[*websocket.Conn]bool // H3Index -> Connexions
	deltaStore  map[string][]VoxelDelta             // H3Index -> Deltas
}

var (
	hub = &H3Hub{
		subscribers: make(map[string]map[*websocket.Conn]bool),
		deltaStore:  make(map[string][]VoxelDelta),
	}
	upgrader = websocket.Upgrader{
		CheckOrigin: func(r *http.Request) bool { return true },
	}
)

func main() {
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	http.HandleFunc("/api/v1/health", handleHealth)
	http.HandleFunc("/api/v1/cells/", handleGetCellDeltas)
	http.HandleFunc("/ws/spatial", handleSpatialWebSocket)

	log.Printf("[TERRE ZÉRO] Serveur Backend Go démarré sur le port :%s", port)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatalf("Erreur serveur : %v", err)
	}
}

func handleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{
		"status":  "healthy",
		"service": "TerreZero-Backend-Go",
		"version": "1.0.0",
		"engine":  "Godot 4 / H3 Res 9",
	})
}

// Récupère l'historique des deltas d'une cellule H3 pour reconstruire le monde
func handleGetCellDeltas(w http.ResponseWriter, r *http.Request) {
	h3Index := r.URL.Path[len("/api/v1/cells/"):]
	if h3Index == "" {
		http.Error(w, "Index H3 manquant", http.StatusBadRequest)
		return
	}

	hub.mu.RLock()
	deltas := hub.deltaStore[h3Index]
	hub.mu.RUnlock()

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"h3_index": h3Index,
		"count":    len(deltas),
		"deltas":   deltas,
	})
}

// WebSocket pour la diffusion des impacts et destructions aux joueurs de la même cellule
func handleSpatialWebSocket(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("Erreur upgrade WS: %v", err)
		return
	}
	defer conn.Close()

	var currentCell string

	for {
		_, msg, err := conn.ReadMessage()
		if err != nil {
			break
		}

		var delta VoxelDelta
		if err := json.Unmarshal(msg, &delta); err == nil {
			delta.Timestamp = time.Now()
			currentCell = delta.H3Index

			// Sauvegarder dans le store
			hub.mu.Lock()
			hub.deltaStore[currentCell] = append(hub.deltaStore[currentCell], delta)
			clients := hub.subscribers[currentCell]
			hub.mu.Unlock()

			// Diffuser aux autres joueurs de la cellule H3
			for client := range clients {
				if client != conn {
					client.WriteMessage(websocket.TextMessage, msg)
				}
			}
		}
	}
}
