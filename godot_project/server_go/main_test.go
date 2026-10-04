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


func TestResolveSpatialCell(t *testing.T) {
	request := httptest.NewRequest(
		http.MethodGet,
		"/api/v1/spatial/cell?lat=45.75&lon=4.85",
		nil,
	)
	recorder := httptest.NewRecorder()

	handleResolveSpatialCell(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	body := recorder.Body.String()
	if !strings.Contains(body, "\"resolution\":9") {
		t.Fatalf("expected H3 resolution 9: %s", body)
	}
	if !strings.Contains(body, "\"h3_index\":") {
		t.Fatalf("expected H3 index in response: %s", body)
	}
}

func TestResolveSpatialCellRejectsInvalidCoordinates(t *testing.T) {
	request := httptest.NewRequest(
		http.MethodGet,
		"/api/v1/spatial/cell?lat=200&lon=4.85",
		nil,
	)
	recorder := httptest.NewRecorder()

	handleResolveSpatialCell(recorder, request)

	if recorder.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", recorder.Code)
	}
}


func TestValidateStartLocation(t *testing.T) {
	body := strings.NewReader(`{
		"machine_latitude":45.75,
		"machine_longitude":4.85,
		"start_latitude":45.77,
		"start_longitude":4.87,
		"radius_km":5
	}`)
	request := httptest.NewRequest(
		http.MethodPost,
		"/api/v1/spatial/start",
		body,
	)
	recorder := httptest.NewRecorder()

	handleValidateStartLocation(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d: %s", recorder.Code, recorder.Body.String())
	}

	response := recorder.Body.String()
	if !strings.Contains(response, "\"allowed\":true") {
		t.Fatalf("expected start location to be allowed: %s", response)
	}
	if !strings.Contains(response, "\"h3_index\":") {
		t.Fatalf("expected H3 index for allowed start: %s", response)
	}
}

func TestRejectStartLocationOutsideRadius(t *testing.T) {
	body := strings.NewReader(`{
		"machine_latitude":45.75,
		"machine_longitude":4.85,
		"start_latitude":45.95,
		"start_longitude":4.85,
		"radius_km":10
	}`)
	request := httptest.NewRequest(
		http.MethodPost,
		"/api/v1/spatial/start",
		body,
	)
	recorder := httptest.NewRecorder()

	handleValidateStartLocation(recorder, request)

	if recorder.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", recorder.Code)
	}

	if !strings.Contains(recorder.Body.String(), "\"allowed\":false") {
		t.Fatalf("expected distant start location to be rejected: %s", recorder.Body.String())
	}
}

func TestRejectUnsupportedStartRadius(t *testing.T) {
	body := strings.NewReader(`{
		"machine_latitude":45.75,
		"machine_longitude":4.85,
		"start_latitude":45.75,
		"start_longitude":4.85,
		"radius_km":7
	}`)
	request := httptest.NewRequest(
		http.MethodPost,
		"/api/v1/spatial/start",
		body,
	)
	recorder := httptest.NewRecorder()

	handleValidateStartLocation(recorder, request)

	if recorder.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", recorder.Code)
	}
}
