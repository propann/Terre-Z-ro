package main

import "testing"

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
}
