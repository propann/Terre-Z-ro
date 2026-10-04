package main

import (
	"strings"
	"testing"
)

func TestH3CellBounds(t *testing.T) {
	bounds, err := h3CellBounds("891fb466257ffff")
	if err != nil {
		t.Fatalf("h3 bounds: %v", err)
	}

	if bounds.South >= bounds.North {
		t.Fatalf("invalid latitude bounds: %+v", bounds)
	}
	if bounds.West >= bounds.East {
		t.Fatalf("invalid longitude bounds: %+v", bounds)
	}
}

func TestBuildOverpassQuery(t *testing.T) {
	query := buildOverpassQuery(geoBounds{
		South: 45.70,
		West:  4.80,
		North: 45.80,
		East:  4.90,
	})

	for _, expected := range []string{
		`way["building"]`,
		`way["highway"]`,
		"45.7000000,4.8000000,45.8000000,4.9000000",
		"out tags geom;",
	} {
		if !strings.Contains(query, expected) {
			t.Fatalf("query missing %q: %s", expected, query)
		}
	}
}

func TestParseOverpass(t *testing.T) {
	raw := []byte(`{
		"elements": [
			{
				"type": "way",
				"id": 101,
				"tags": {
					"building": "retail",
					"name": "Test Shop",
					"shop": "bakery",
					"building:levels": "3",
					"height": "11.5 m"
				},
				"geometry": [
					{"lat":45.0,"lon":5.0},
					{"lat":45.0,"lon":5.001},
					{"lat":45.001,"lon":5.001},
					{"lat":45.001,"lon":5.0}
				]
			},
			{
				"type": "way",
				"id": 202,
				"tags": {
					"highway": "residential",
					"surface": "paving_stones",
					"lanes": "2"
				},
				"geometry": [
					{"lat":45.0,"lon":5.0},
					{"lat":45.002,"lon":5.002}
				]
			}
		]
	}`)

	cell, err := parseOverpass(raw)
	if err != nil {
		t.Fatalf("parse: %v", err)
	}

	if len(cell.Buildings) != 1 {
		t.Fatalf("expected 1 building, got %d", len(cell.Buildings))
	}
	if len(cell.Roads) != 1 {
		t.Fatalf("expected 1 road, got %d", len(cell.Roads))
	}

	building := cell.Buildings[0]
	if building.BuildingType != "retail" ||
		building.Shop != "bakery" ||
		building.Levels != 3 {
		t.Fatalf("unexpected building: %+v", building)
	}
	if building.HeightMeters != 11.5 {
		t.Fatalf("unexpected height: %f", building.HeightMeters)
	}
	if !strings.Contains(building.GeoJSON, `"type":"Polygon"`) {
		t.Fatalf("building geometry is not polygon: %s", building.GeoJSON)
	}

	road := cell.Roads[0]
	if road.HighwayType != "residential" ||
		road.Surface != "paving_stones" ||
		road.Lanes != 2 {
		t.Fatalf("unexpected road: %+v", road)
	}
	if !strings.Contains(road.GeoJSON, `"type":"LineString"`) {
		t.Fatalf("road geometry is not linestring: %s", road.GeoJSON)
	}
}

func TestParseOSMNumericTags(t *testing.T) {
	if got := parsePositiveInt("2;3", 1); got != 2 {
		t.Fatalf("lanes/levels parsing: got %d", got)
	}
	if got := parsePositiveInt("bad", 4); got != 4 {
		t.Fatalf("fallback parsing: got %d", got)
	}
	if got := parseMeters("12.4 m"); got != 12.4 {
		t.Fatalf("height parsing: got %f", got)
	}
}