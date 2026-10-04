package main

import (
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
)

func TestValidH3(t *testing.T) {
	if !validH3("891fb466257ffff") {
		t.Fatal("expected valid H3-like cell")
	}
	if validH3("not-a-cell") {
		t.Fatal("expected invalid H3 cell")
	}
}

func TestValidateDelta(t *testing.T) {
	valid := VoxelDelta{
		WorldVersion: worldVersion,
		GeneratorVersion: generatorVersion,
		H3Index:    "891fb466257ffff",
		ChunkCoord: [3]int{0, 0, 0},
		LocalVoxel: [3]int{12, 3, 31},
		Action:     "DESTROY",
		PlayerID:   "player-test",
	}
	if err := validateDelta(valid); err != nil {
		t.Fatalf("valid delta rejected: %v", err)
	}

	invalid := valid
	invalid.LocalVoxel[2] = 32
	if err := validateDelta(invalid); err == nil {
		t.Fatal("out-of-range voxel should be rejected")
	}

	wrongVersion := valid
	wrongVersion.GeneratorVersion = generatorVersion - 1
	if err := validateDelta(wrongVersion); err == nil {
		t.Fatal("stale generator version should be rejected")
	}
}

func TestWorldCellEndpointMemoryFallback(t *testing.T) {
	previousStore := store
	store = newMemoryStore()
	defer func() { store = previousStore }()

	request := httptest.NewRequest(
		http.MethodGet,
		"/api/v1/world/cells/891fb466257ffff",
		nil,
	)
	recorder := httptest.NewRecorder()

	handleGetWorldCell(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", recorder.Code)
	}

	body := recorder.Body.String()
	if !strings.Contains(body, "\"h3_index\":\"891fb466257ffff\"") {
		t.Fatalf("missing H3 cell in response: %s", body)
	}
	if !strings.Contains(body, "\"buildings\":[]") {
		t.Fatalf("expected empty buildings in memory fallback: %s", body)
	}
	if !strings.Contains(body, "\"roads\":[]") {
		t.Fatalf("expected empty roads in memory fallback: %s", body)
	}
}
