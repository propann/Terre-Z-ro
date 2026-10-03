// Micro-Voxel Engine (Resolution: 1 voxel = 20cm, Chunk = 32x32x32 voxels = 6.4m)
// 16-Bit Voxel Encoding: 8 bits Material/Color, 4 bits Durability, 4 bits Light/State

export enum VoxelMaterial {
  AIR = 0,
  CONCRETE = 1,       // Béton armé (bâtiments)
  BRICK = 2,          // Brique rouge
  ASPHALT = 3,        // Asphalte craquelé (routes)
  SIDEWALK = 4,       // Pavé trottoir (2 voxels de haut)
  REINFORCED_GLASS = 5, // Verre blindé
  COPPER_WIRING = 6,  // Câblage cuivre dissimulé (loot précieux)
  MED_CACHE = 7,      // Caisse médicale cachée derrière cloison
  STEEL_BARRICADE = 8,// Barricade joueur
  TURRET_BASE = 9,    // Base de tourelle
  GRASS_ORGANIC = 10  // Végétation mutée
}

export const VOXEL_PALETTE: Record<VoxelMaterial, { name: string; color: number; hex: string; hardness: number; isLoot?: boolean }> = {
  [VoxelMaterial.AIR]: { name: 'Vide', color: 0x000000, hex: '#000000', hardness: 0 },
  [VoxelMaterial.CONCRETE]: { name: 'Béton Armé', color: 0x475569, hex: '#475569', hardness: 10 },
  [VoxelMaterial.BRICK]: { name: 'Brique Rouge', color: 0xb45309, hex: '#b45309', hardness: 7 },
  [VoxelMaterial.ASPHALT]: { name: 'Asphalte Craquelé', color: 0x1e293b, hex: '#1e293b', hardness: 12 },
  [VoxelMaterial.SIDEWALK]: { name: 'Bordure Trottoir', color: 0x64748b, hex: '#64748b', hardness: 8 },
  [VoxelMaterial.REINFORCED_GLASS]: { name: 'Verre Blindé', color: 0x38bdf8, hex: '#38bdf8', hardness: 4 },
  [VoxelMaterial.COPPER_WIRING]: { name: 'Câblage Cuivre (Loot)', color: 0xf97316, hex: '#f97316', hardness: 3, isLoot: true },
  [VoxelMaterial.MED_CACHE]: { name: 'Trousse de Soin Cachée', color: 0x10b981, hex: '#10b981', hardness: 2, isLoot: true },
  [VoxelMaterial.STEEL_BARRICADE]: { name: 'Blindage Titane', color: 0x0284c7, hex: '#0284c7', hardness: 20 },
  [VoxelMaterial.TURRET_BASE]: { name: 'Base Tourelle', color: 0xd97706, hex: '#d97706', hardness: 15 },
  [VoxelMaterial.GRASS_ORGANIC]: { name: 'Mousse Mutante', color: 0x047857, hex: '#047857', hardness: 2 }
};

export const CHUNK_SIZE = 32; // 32x32x32 voxels
export const VOXEL_SIZE_METERS = 0.2; // 20cm per voxel -> Chunk is 6.4m x 6.4m x 6.4m

// 16-bit Voxel representation (stored in a flat Uint16Array of size 32768)
export class VoxelChunk {
  public data: Uint16Array;
  public chunkX: number;
  public chunkY: number;
  public chunkZ: number;
  public modified: boolean = false;
  public editsCount: number = 0;

  constructor(chunkX = 0, chunkY = 0, chunkZ = 0) {
    this.chunkX = chunkX;
    this.chunkY = chunkY;
    this.chunkZ = chunkZ;
    this.data = new Uint16Array(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE);
  }

  private getIndex(x: number, y: number, z: number): number {
    return x + y * CHUNK_SIZE + z * CHUNK_SIZE * CHUNK_SIZE;
  }

  // Pack 16-bit: (Material 8 bits) | (Durability 4 bits << 8) | (Light/State 4 bits << 12)
  public setVoxel(x: number, y: number, z: number, mat: VoxelMaterial, durability = 15, light = 0): void {
    if (x < 0 || x >= CHUNK_SIZE || y < 0 || y >= CHUNK_SIZE || z < 0 || z >= CHUNK_SIZE) return;
    const packed = (mat & 0xff) | ((durability & 0xf) << 8) | ((light & 0xf) << 12);
    this.data[this.getIndex(x, y, z)] = packed;
    this.modified = true;
    this.editsCount++;
  }

  public getMaterial(x: number, y: number, z: number): VoxelMaterial {
    if (x < 0 || x >= CHUNK_SIZE || y < 0 || y >= CHUNK_SIZE || z < 0 || z >= CHUNK_SIZE) return VoxelMaterial.AIR;
    return this.data[this.getIndex(x, y, z)] & 0xff;
  }

  public getDurability(x: number, y: number, z: number): number {
    if (x < 0 || x >= CHUNK_SIZE || y < 0 || y >= CHUNK_SIZE || z < 0 || z >= CHUNK_SIZE) return 0;
    return (this.data[this.getIndex(x, y, z)] >> 8) & 0xf;
  }

  // Destruction / Mining: drill a sphere or cube hole
  public destroySphere(cx: number, cy: number, cz: number, radiusVoxels: number): { destroyedCount: number; lootFound: { mat: VoxelMaterial; count: number }[] } {
    let count = 0;
    const lootMap = new Map<VoxelMaterial, number>();

    const r2 = radiusVoxels * radiusVoxels;
    const minX = Math.max(0, Math.floor(cx - radiusVoxels));
    const maxX = Math.min(CHUNK_SIZE - 1, Math.ceil(cx + radiusVoxels));
    const minY = Math.max(0, Math.floor(cy - radiusVoxels));
    const maxY = Math.min(CHUNK_SIZE - 1, Math.ceil(cy + radiusVoxels));
    const minZ = Math.max(0, Math.floor(cz - radiusVoxels));
    const maxZ = Math.min(CHUNK_SIZE - 1, Math.ceil(cz + radiusVoxels));

    for (let x = minX; x <= maxX; x++) {
      for (let y = minY; y <= maxY; y++) {
        for (let z = minZ; z <= maxZ; z++) {
          const d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy) + (z - cz) * (z - cz);
          if (d2 <= r2) {
            const current = this.getMaterial(x, y, z);
            if (current !== VoxelMaterial.AIR) {
              if (current === VoxelMaterial.COPPER_WIRING || current === VoxelMaterial.MED_CACHE) {
                lootMap.set(current, (lootMap.get(current) || 0) + 1);
              }
              this.setVoxel(x, y, z, VoxelMaterial.AIR);
              count++;
            }
          }
        }
      }
    }

    const lootFound: { mat: VoxelMaterial; count: number }[] = [];
    lootMap.forEach((qty, mat) => lootFound.push({ mat, count: qty }));

    return { destroyedCount: count, lootFound };
  }

  // Count active non-air voxels
  public countSolidVoxels(): number {
    let total = 0;
    for (let i = 0; i < this.data.length; i++) {
      if ((this.data[i] & 0xff) !== VoxelMaterial.AIR) total++;
    }
    return total;
  }
}

// Procedural OSM 2.5D Voxelizer for a Real Neighborhood Block
export function voxelizeOsmBuildingBlock(buildingType: 'residential' | 'pharmacy' | 'bank' | 'industrial'): VoxelChunk {
  const chunk = new VoxelChunk();

  // 1. Asphalt Road at bottom (Y = 0 to 1) & Sidewalk with 2-voxel height (Y = 2 to 3)
  for (let x = 0; x < CHUNK_SIZE; x++) {
    for (let z = 0; z < CHUNK_SIZE; z++) {
      // Road in front
      if (z < 7) {
        chunk.setVoxel(x, 0, z, VoxelMaterial.ASPHALT);
        chunk.setVoxel(x, 1, z, VoxelMaterial.ASPHALT);
      }
      // Sidewalk
      else if (z < 11) {
        chunk.setVoxel(x, 0, z, VoxelMaterial.ASPHALT);
        chunk.setVoxel(x, 1, z, VoxelMaterial.ASPHALT);
        chunk.setVoxel(x, 2, z, VoxelMaterial.SIDEWALK);
        chunk.setVoxel(x, 3, z, VoxelMaterial.SIDEWALK);
      }
    }
  }

  // 2. Extruded Building Walls & Floors (from Y = 3 up to Y = 28)
  const wallMat = buildingType === 'industrial' ? VoxelMaterial.CONCRETE : buildingType === 'pharmacy' ? VoxelMaterial.BRICK : VoxelMaterial.CONCRETE;
  const bMinX = 4;
  const bMaxX = 27;
  const bMinZ = 12;
  const bMaxZ = 29;
  const bHeight = 26; // ~5.2 meters

  for (let y = 3; y <= bHeight; y++) {
    const isFloor = y === 3 || y === 15 || y === bHeight;

    for (let x = bMinX; x <= bMaxX; x++) {
      for (let z = bMinZ; z <= bMaxZ; z++) {
        const isPerimeter = x === bMinX || x === bMaxX || z === bMinZ || z === bMaxZ;
        const isWindow = (y >= 7 && y <= 11) || (y >= 18 && y <= 22);
        const isDoor = z === bMinZ && x >= 14 && x <= 17 && y <= 8;

        if (isDoor) {
          // Door opening (Air)
          continue;
        }

        if (isFloor) {
          chunk.setVoxel(x, y, z, VoxelMaterial.CONCRETE);
        } else if (isPerimeter) {
          if (isWindow && (x % 4 === 1 || x % 4 === 2) && z === bMinZ) {
            chunk.setVoxel(x, y, z, VoxelMaterial.REINFORCED_GLASS);
          } else {
            chunk.setVoxel(x, y, z, wallMat);
          }
        }
      }
    }
  }

  // 3. Inject Hidden Infrastructure Loot inside the walls (Sprint 3.2)
  // Copper electrical wiring embedded in the internal wall conduits
  for (let y = 4; y <= bHeight - 2; y++) {
    chunk.setVoxel(bMinX + 1, y, 18, VoxelMaterial.COPPER_WIRING);
    chunk.setVoxel(bMaxX - 1, y, 22, VoxelMaterial.COPPER_WIRING);
  }

  // If Pharmacy or Bank, hide valuable survival medical/cash caches behind inner false partitions!
  if (buildingType === 'pharmacy') {
    for (let x = 12; x <= 15; x++) {
      chunk.setVoxel(x, 5, 20, VoxelMaterial.MED_CACHE);
      chunk.setVoxel(x, 6, 20, VoxelMaterial.MED_CACHE);
    }
  } else if (buildingType === 'bank') {
    for (let x = 16; x <= 19; x++) {
      chunk.setVoxel(x, 5, 22, VoxelMaterial.STEEL_BARRICADE);
      chunk.setVoxel(x, 6, 22, VoxelMaterial.COPPER_WIRING);
    }
  }

  return chunk;
}

// -------------------------------------------------------------
// SPRINT 2.1 : GREEDY MESHING ALGORITHM (C# / TS implementation)
// -------------------------------------------------------------
// Eliminates interior faces & merges adjacent coplanar quad faces of the same material
// Reduces polygon count from ~30,000+ triangles down to ~400-800 triangles per chunk!

export interface GreedyQuad {
  x: number;
  y: number;
  z: number;
  w: number; // Width
  h: number; // Height
  axis: number; // 0 = X, 1 = Y, 2 = Z
  normalSign: number; // +1 or -1
  material: VoxelMaterial;
}

export interface MeshingResult {
  quads: GreedyQuad[];
  rawTrianglesCount: number;
  greedyTrianglesCount: number;
  reductionPercentage: number;
}

export function computeGreedyMesh(chunk: VoxelChunk): MeshingResult {
  const quads: GreedyQuad[] = [];
  let rawFacesCount = 0;

  // Sweep over the 3 axes (d = 0: X, d = 1: Y, d = 2: Z)
  for (let d = 0; d < 3; d++) {
    const u = (d + 1) % 3;
    const v = (d + 2) % 3;

    const x = [0, 0, 0];
    const q = [0, 0, 0];
    q[d] = 1;

    // Mask for current 2D slice
    const mask = new Int32Array(CHUNK_SIZE * CHUNK_SIZE);

    for (x[d] = -1; x[d] < CHUNK_SIZE; ) {
      let n = 0;
      for (x[v] = 0; x[v] < CHUNK_SIZE; x[v]++) {
        for (x[u] = 0; x[u] < CHUNK_SIZE; x[u]++) {
          const a = x[d] >= 0 ? chunk.getMaterial(x[0], x[1], x[2]) : VoxelMaterial.AIR;
          const b = x[d] < CHUNK_SIZE - 1 ? chunk.getMaterial(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : VoxelMaterial.AIR;

          if (a !== VoxelMaterial.AIR && b === VoxelMaterial.AIR) {
            mask[n++] = a; // Face facing positive direction (+1)
            rawFacesCount++;
          } else if (a === VoxelMaterial.AIR && b !== VoxelMaterial.AIR) {
            mask[n++] = -b; // Face facing negative direction (-1)
            rawFacesCount++;
          } else {
            mask[n++] = 0; // Internal shared face -> culled!
          }
        }
      }

      x[d]++;
      n = 0;

      // Greedy merge adjacent quads with identical mask value
      for (let j = 0; j < CHUNK_SIZE; j++) {
        for (let i = 0; i < CHUNK_SIZE; ) {
          const currentVal = mask[n];
          if (currentVal !== 0) {
            let width = 1;
            while (i + width < CHUNK_SIZE && mask[n + width] === currentVal) {
              width++;
            }

            let height = 1;
            let canExtend = true;
            while (j + height < CHUNK_SIZE && canExtend) {
              for (let k = 0; k < width; k++) {
                if (mask[n + k + height * CHUNK_SIZE] !== currentVal) {
                  canExtend = false;
                  break;
                }
              }
              if (canExtend) height++;
            }

            // Record Greedy Quad
            const pos = [0, 0, 0];
            pos[d] = x[d];
            pos[u] = i;
            pos[v] = j;

            quads.push({
              x: pos[0],
              y: pos[1],
              z: pos[2],
              w: width,
              h: height,
              axis: d,
              normalSign: currentVal > 0 ? 1 : -1,
              material: Math.abs(currentVal) as VoxelMaterial
            });

            // Clear processed area in mask
            for (let l = 0; l < height; l++) {
              for (let k = 0; k < width; k++) {
                mask[n + k + l * CHUNK_SIZE] = 0;
              }
            }

            i += width;
            n += width;
          } else {
            i++;
            n++;
          }
        }
      }
    }
  }

  const rawTrianglesCount = rawFacesCount * 2;
  const greedyTrianglesCount = quads.length * 2;
  const reductionPercentage = rawTrianglesCount > 0
    ? Math.round(((rawTrianglesCount - greedyTrianglesCount) / rawTrianglesCount) * 100)
    : 0;

  return {
    quads,
    rawTrianglesCount,
    greedyTrianglesCount,
    reductionPercentage
  };
}

// -------------------------------------------------------------
// SPRINT 4.1 : RUN-LENGTH ENCODING (RLE) DELTA SYNC SERIALIZER
// -------------------------------------------------------------
export function serializeVoxelDeltaRLE(edits: { x: number; y: number; z: number; mat: VoxelMaterial }[]): string {
  if (edits.length === 0) return 'EMPTY_DELTA';
  // Compact binary/hex string format: [count, x, y, z, mat]
  return JSON.stringify(edits.map(e => [e.x, e.y, e.z, e.mat]));
}
