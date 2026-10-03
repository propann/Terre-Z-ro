import {
  ParsedBuilding,
  ParsedRoad,
  ParsedPark,
  LootSpot,
  Chimere,
  H3Tile,
  Item,
  OSMQueryPreset
} from '../types/game';

// Preset locations for rapid testing without requiring live GPS or if indoor
export const OSM_PRESETS: OSMQueryPreset[] = [
  {
    name: 'Tour Eiffel & Champ de Mars',
    city: 'Paris, France',
    lat: 48.8584,
    lon: 2.2945,
    radiusMeters: 450,
    description: 'Zone urbaine dense avec parcs, structures métalliques et monuments'
  },
  {
    name: 'Times Square & Midtown',
    city: 'New York, USA',
    lat: 40.7580,
    lon: -73.9855,
    radiusMeters: 350,
    description: 'Gratte-ciels géants, forte densité commerciale et souterrains'
  },
  {
    name: 'Shibuya Crossing & Gare',
    city: 'Tokyo, Japon',
    lat: 35.6595,
    lon: 139.7005,
    radiusMeters: 400,
    description: 'Ruines technologiques, néons brisés et densité extrême'
  },
  {
    name: 'Centre Historique & Quais',
    city: 'Lyon, France',
    lat: 45.7640,
    lon: 4.8357,
    radiusMeters: 400,
    description: 'Bâtiments anciens, ruelles étroites et fleuve contaminé'
  }
];

// Distance in meters between two lat/lon points (Haversine formula)
export function getDistanceMeters(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371e3; // Earth radius in meters
  const phi1 = (lat1 * Math.PI) / 180;
  const phi2 = (lat2 * Math.PI) / 180;
  const deltaPhi = ((lat2 - lat1) * Math.PI) / 180;
  const deltaLambda = ((lon2 - lon1) * Math.PI) / 180;

  const a =
    Math.sin(deltaPhi / 2) * Math.sin(deltaPhi / 2) +
    Math.cos(phi1) * Math.cos(phi2) * Math.sin(deltaLambda / 2) * Math.sin(deltaLambda / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

  return R * c;
}

// Convert Lat/Lon to Local X, Z in meters relative to an anchor [originLat, originLon]
export function latLonToLocalMeters(
  lat: number,
  lon: number,
  originLat: number,
  originLon: number
): { x: number; z: number } {
  const latMeters = (lat - originLat) * 111320;
  const lonMeters = (lon - originLon) * (40075000 * Math.cos((originLat * Math.PI) / 180) / 360);
  return { x: lonMeters, z: -latMeters };
}

// Build Overpass QL Query for a bounding circle
export function buildOverpassQuery(lat: number, lon: number, radiusMeters: number): string {
  return `[out:json][timeout:25];
(
  // Buildings with height/levels tags
  way["building"](around:${radiusMeters},${lat},${lon});
  relation["building"](around:${radiusMeters},${lat},${lon});
  
  // Roads and paths
  way["highway"](around:${radiusMeters},${lat},${lon});
  
  // Amenities & Shops (Loot spots & Points of Interest)
  node["amenity"](around:${radiusMeters},${lat},${lon});
  way["amenity"](around:${radiusMeters},${lat},${lon});
  node["shop"](around:${radiusMeters},${lat},${lon});
  way["shop"](around:${radiusMeters},${lat},${lon});
  
  // Leisure & Parks (Bio Chimere breeding grounds)
  way["leisure"="park"](around:${radiusMeters},${lat},${lon});
  way["landuse"="forest"](around:${radiusMeters},${lat},${lon});
  way["landuse"="industrial"](around:${radiusMeters},${lat},${lon});
);
out body;
>;
out skel qt;`;
}

// Generate Realistic Mock OSM Data when API is unreachable or rate-limited
export function generateProceduralOsmData(centerLat: number, centerLon: number, radiusMeters: number) {
  const elements: any[] = [];
  const nodesMap = new Map<number, { lat: number; lon: number }>();
  let idCounter = 100000;

  // Grid layout around center
  const step = 0.0008; // ~80m
  const count = Math.ceil(radiusMeters / 80);

  // Roads
  for (let i = -count; i <= count; i++) {
    // Horizontal road
    const rId = idCounter++;
    const nodeIds: number[] = [];
    for (let j = -count; j <= count; j++) {
      const nId = idCounter++;
      const lat = centerLat + i * step;
      const lon = centerLon + j * step;
      nodesMap.set(nId, { lat, lon });
      elements.push({ type: 'node', id: nId, lat, lon });
      nodeIds.push(nId);
    }
    elements.push({
      type: 'way',
      id: rId,
      nodes: nodeIds,
      tags: { highway: i === 0 ? 'primary' : 'residential', name: `Rue Déchue ${Math.abs(i)}` }
    });
  }

  // Buildings in blocks
  for (let i = -count; i < count; i++) {
    for (let j = -count; j < count; j++) {
      if (Math.abs(i) === 0 && Math.abs(j) === 0) continue; // Keep center clear for player base
      const bLat = centerLat + (i + 0.5) * step;
      const bLon = centerLon + (j + 0.5) * step;
      const size = step * 0.35;

      const n1 = idCounter++;
      const n2 = idCounter++;
      const n3 = idCounter++;
      const n4 = idCounter++;

      elements.push({ type: 'node', id: n1, lat: bLat - size, lon: bLon - size });
      elements.push({ type: 'node', id: n2, lat: bLat - size, lon: bLon + size });
      elements.push({ type: 'node', id: n3, lat: bLat + size, lon: bLon + size });
      elements.push({ type: 'node', id: n4, lat: bLat + size, lon: bLon - size });

      const levels = Math.floor(Math.random() * 6) + 1;
      const isShop = Math.random() > 0.5;
      const shopTypes = ['supermarket', 'bakery', 'chemist', 'hardware', 'electronics'];
      const amenityTypes = ['pharmacy', 'hospital', 'restaurant', 'bank', 'police'];
      
      const tags: Record<string, string> = {
        building: 'yes',
        'building:levels': levels.toString(),
        height: (levels * 3.5).toString()
      };

      if (isShop) {
        if (Math.random() > 0.5) {
          tags.amenity = amenityTypes[Math.floor(Math.random() * amenityTypes.length)];
          tags.name = `Ruines de ${tags.amenity.toUpperCase()}`;
        } else {
          tags.shop = shopTypes[Math.floor(Math.random() * shopTypes.length)];
          tags.name = `Boutique ${tags.shop}`;
        }
      }

      elements.push({
        type: 'way',
        id: idCounter++,
        nodes: [n1, n2, n3, n4, n1],
        tags
      });
    }
  }

  return { elements };
}

// Fetch from Overpass API with CORS proxy or fallback
export async function fetchOverpassData(lat: number, lon: number, radiusMeters: number): Promise<any> {
  const query = buildOverpassQuery(lat, lon, radiusMeters);
  const endpoints = [
    'https://overpass-api.de/api/interpreter',
    'https://overpass.kumi.systems/api/interpreter',
    'https://maps.mail.ru/osm/tools/overpass/api/interpreter'
  ];

  for (const endpoint of endpoints) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 8000);

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
        },
        body: 'data=' + encodeURIComponent(query),
        signal: controller.signal
      });
      clearTimeout(timeoutId);

      if (response.ok) {
        const data = await response.json();
        if (data && data.elements && data.elements.length > 0) {
          return data;
        }
      }
    } catch {
      // Try next endpoint or fallback
    }
  }

  // Fallback to procedural OSM generator
  return generateProceduralOsmData(lat, lon, radiusMeters);
}

// Parser that takes raw Overpass JSON and produces game assets
export function parseOsmData(
  osmJson: any,
  centerLat: number,
  centerLon: number
): {
  buildings: ParsedBuilding[];
  roads: ParsedRoad[];
  parks: ParsedPark[];
  lootSpots: LootSpot[];
  chimeres: Chimere[];
  h3Tiles: H3Tile[];
  stats: {
    totalNodes: number;
    totalWays: number;
    buildingCount: number;
    roadCount: number;
    lootCount: number;
    chimereCount: number;
  };
} {
  const nodesMap = new Map<number, { lat: number; lon: number }>();
  const buildings: ParsedBuilding[] = [];
  const roads: ParsedRoad[] = [];
  const parks: ParsedPark[] = [];
  const lootSpots: LootSpot[] = [];
  const chimeres: Chimere[] = [];

  let totalNodes = 0;
  let totalWays = 0;

  // 1. Index all nodes
  if (osmJson?.elements) {
    for (const elem of osmJson.elements) {
      if (elem.type === 'node') {
        totalNodes++;
        nodesMap.set(elem.id, { lat: elem.lat, lon: elem.lon });

        // Node level amenity / shop loot point
        if (elem.tags && (elem.tags.amenity || elem.tags.shop)) {
          const spot = createLootSpotFromTags(elem.id, elem.lat, elem.lon, elem.tags, centerLat, centerLon);
          if (spot) lootSpots.push(spot);
        }
      }
    }

    // 2. Parse ways (Buildings, Roads, Parks, Landuse)
    for (const elem of osmJson.elements) {
      if (elem.type === 'way' && elem.nodes && elem.nodes.length > 1) {
        totalWays++;
        const coords: [number, number][] = [];
        for (const nodeId of elem.nodes) {
          const n = nodesMap.get(nodeId);
          if (n) {
            coords.push([n.lat, n.lon]);
          }
        }

        if (coords.length < 2) continue;

        const tags = elem.tags || {};

        // A. Is Building
        if (tags.building) {
          let levels = 1;
          if (tags['building:levels']) {
            levels = Math.max(1, parseInt(tags['building:levels'], 10) || 1);
          }
          let height = levels * 3.5;
          if (tags.height) {
            const parsedH = parseFloat(tags.height);
            if (!isNaN(parsedH) && parsedH > 0) height = parsedH;
          }

          // Calculate Centroid
          let sumLat = 0;
          let sumLon = 0;
          for (const [lat, lon] of coords) {
            sumLat += lat;
            sumLon += lon;
          }
          const centroid: [number, number] = [sumLat / coords.length, sumLon / coords.length];

          // Determine building type
          let bType: ParsedBuilding['type'] = 'residential';
          if (tags.amenity === 'hospital' || tags.amenity === 'clinic') bType = 'hospital';
          else if (tags.landuse === 'industrial' || tags.building === 'industrial') bType = 'industrial';
          else if (tags.shop || tags.amenity === 'restaurant' || tags.amenity === 'bank') bType = 'commercial';
          else if (levels > 6) bType = 'ruins';

          buildings.push({
            id: elem.id,
            name: tags.name,
            polygon: coords,
            height,
            levels,
            type: bType,
            tags,
            centroid
          });

          // Check if building has loot tags
          if (tags.amenity || tags.shop) {
            const spot = createLootSpotFromTags(elem.id, centroid[0], centroid[1], tags, centerLat, centerLon);
            if (spot) lootSpots.push(spot);
          }

          // Chance to spawn a Chimère inside or near the building
          if (Math.random() < 0.22) {
            const chimere = generateChimereFromContext(
              elem.id,
              centroid[0],
              centroid[1],
              bType === 'industrial' ? 'mechanical' : Math.random() > 0.4 ? 'mechanical' : 'bio',
              tags.name || `Ruine #${elem.id % 1000}`,
              centerLat,
              centerLon
            );
            chimeres.push(chimere);
          }
        }
        // B. Is Road / Highway
        else if (tags.highway) {
          roads.push({
            id: elem.id,
            name: tags.name,
            points: coords,
            highwayType: tags.highway,
            isCrevassed: Math.random() < 0.3
          });
        }
        // C. Is Park / Forest
        else if (tags.leisure === 'park' || tags.landuse === 'forest' || tags.landuse === 'grass') {
          parks.push({
            id: elem.id,
            name: tags.name,
            polygon: coords
          });

          // Parks are prime spawning zones for Bio Chimères!
          if (coords.length > 2) {
            const pCenter: [number, number] = [coords[0][0], coords[0][1]];
            const bioChimere = generateChimereFromContext(
              elem.id + 999,
              pCenter[0],
              pCenter[1],
              'bio',
              tags.name || 'Zone Boisée Contaminée',
              centerLat,
              centerLon
            );
            chimeres.push(bioChimere);
          }
        }
      }
    }
  }

  // 3. Generate H3 Hexagonal Grid Cells around center
  const h3Tiles = generateH3HexGrid(centerLat, centerLon, 450, lootSpots, chimeres);

  return {
    buildings,
    roads,
    parks,
    lootSpots,
    chimeres,
    h3Tiles,
    stats: {
      totalNodes,
      totalWays,
      buildingCount: buildings.length,
      roadCount: roads.length,
      lootCount: lootSpots.length,
      chimereCount: chimeres.length
    }
  };
}

// Convert OSM Tags to Scavengeable Loot
function createLootSpotFromTags(
  id: number,
  lat: number,
  lon: number,
  tags: Record<string, string>,
  centerLat: number,
  centerLon: number
): LootSpot | null {
  const distance = Math.round(getDistanceMeters(centerLat, centerLon, lat, lon));
  let category: LootSpot['category'] = 'scrap';
  const items: Item[] = [];
  let name = tags.name || 'Bâtiment Abandonné';

  const tagKey = tags.amenity ? `amenity=${tags.amenity}` : tags.shop ? `shop=${tags.shop}` : 'building=yes';

  if (tags.amenity === 'pharmacy' || tags.amenity === 'hospital' || tags.amenity === 'clinic' || tags.amenity === 'doctors') {
    category = 'medical';
    name = tags.name || 'Pharmacie de Survie';
    items.push(
      { id: `med-${id}-1`, name: 'Stimpack Coagulant', category: 'medical', weight: 0.3, quantity: 2, rarity: 'uncommon', icon: '💉', healAmount: 45, description: 'Stoppe les hémorragies et restaure 45 PV.' },
      { id: `med-${id}-2`, name: 'Pilules Antirad Rad-X', category: 'medical', weight: 0.1, quantity: 3, rarity: 'rare', icon: '💊', radCleanse: 30, description: 'Absorbe 30 rads de contamination.' }
    );
  } else if (tags.amenity === 'restaurant' || tags.amenity === 'fast_food' || tags.amenity === 'cafe' || tags.shop === 'supermarket' || tags.shop === 'bakery' || tags.shop === 'convenience') {
    category = 'food';
    name = tags.name || 'Ration & Vivres';
    items.push(
      { id: `food-${id}-1`, name: 'Rations MRE Militaires', category: 'food', weight: 0.8, quantity: 2, rarity: 'common', icon: '🥫', healAmount: 25, description: 'Ration calorique compacte.' },
      { id: `food-${id}-2`, name: 'Gourde d’Eau Purifiée', category: 'food', weight: 1.0, quantity: 1, rarity: 'common', icon: '💧', healAmount: 15, description: 'Eau sans particules lourdes.' }
    );
  } else if (tags.shop === 'hardware' || tags.shop === 'electronics' || tags.shop === 'computer' || tags.landuse === 'industrial') {
    category = 'electronics';
    name = tags.name || 'Atelier & Électronique';
    items.push(
      { id: `elec-${id}-1`, name: 'Piles Haute Densité', category: 'electronics', weight: 0.5, quantity: 2, rarity: 'uncommon', icon: '🔋', powerValue: 50, description: 'Alimente les générateurs du bunker.' },
      { id: `elec-${id}-2`, name: 'Puces de Guidage de Drone', category: 'electronics', weight: 0.2, quantity: 1, rarity: 'rare', icon: '💾', description: 'Requis pour améliorer le radar et les pièges.' },
      { id: `scrap-${id}-3`, name: 'Ferraille Renforcée', category: 'scrap', weight: 2.0, quantity: 3, rarity: 'common', icon: '⚙️', description: 'Matériau de base pour le bunker.' }
    );
  } else if (tags.amenity === 'police' || tags.amenity === 'bank' || tags.building === 'military') {
    category = 'weapons';
    name = tags.name || 'Dépôt Sécurisé';
    items.push(
      { id: `wpn-${id}-1`, name: 'Piège à Impulsion IEM', category: 'trap', weight: 1.2, quantity: 1, rarity: 'rare', icon: '🪤', description: 'Neutralise et capture les Chimères mécaniques.' },
      { id: `wpn-${id}-2`, name: 'Plaques de Blindage Céramique', category: 'module', weight: 2.5, quantity: 1, rarity: 'military', icon: '🛡️', description: 'Augmente la capacité de résistance du sac.' }
    );
  } else {
    items.push(
      { id: `scrap-${id}-1`, name: 'Composants Récupérés', category: 'scrap', weight: 1.2, quantity: 2, rarity: 'common', icon: '🔩', description: 'Visserie et câblage pour craft.' }
    );
  }

  return {
    id: `loot-${id}`,
    name,
    lat,
    lon,
    distance,
    category,
    osmTag: tagKey,
    items,
    looted: false,
    dangerLevel: Math.floor(Math.random() * 3) + 1
  };
}

// Generate Chimères based on environment
function generateChimereFromContext(
  id: number,
  lat: number,
  lon: number,
  type: 'bio' | 'mechanical',
  contextName: string,
  centerLat: number,
  centerLon: number
): Chimere {
  const mechanicalSpecies = [
    { name: 'Sentinelle Foreuse Mk-IV', avatar: '🤖', lore: 'Ancien drone de forage minier réactivé par une boucle d’IA corrompue.', attack: 28, def: 35, hp: 120, abilities: ['Laser de Découpe', 'Bouclier Métallique'] },
    { name: 'Scarabée Dynamo', avatar: '🦾', lore: 'Créature mécanique alimentée par des condensateurs surchargés.', attack: 34, def: 20, hp: 95, abilities: ['Décharge Haute Tension', 'Vitesse Turbo'] },
    { name: 'Molosse Servo-Assisté', avatar: '🐕‍🦺', lore: 'Chien de garde cybernétique aux mâchoires pneumatiques.', attack: 40, def: 25, hp: 110, abilities: ['Morsure Hydraulique', 'Pistage Thermique'] }
  ];

  const bioSpecies = [
    { name: 'Rat Cerbère des Ruines', avatar: '🐀', lore: 'Rongeur géant à trois gueules muté par les eaux toxiques.', attack: 22, def: 15, hp: 80, abilities: ['Griffes Infectieuses', 'Cri Sonique'] },
    { name: 'Corbeau à Double Tête', avatar: '🦅', lore: 'Rapace aux yeux multiples capable de repérer les proies à des kilomètres.', attack: 30, def: 18, hp: 85, abilities: ['Piqué Venimeux', 'Brouillage Visuel'] },
    { name: 'Lichen Vorace Rampant', avatar: '🌿', lore: 'Symbiose végétale et fongique qui absorbe les radiations.', attack: 25, def: 40, hp: 140, abilities: ['Spores Toxiques', 'Régénération'] },
    { name: 'Cerf Chimérique Luminescent', avatar: '🦌', lore: 'Grand herbivore aux bois irradiés produisant une lueur étrange.', attack: 38, def: 30, hp: 150, abilities: ['Charge Sismique', 'Aura Radiactrice'] }
  ];

  const pool = type === 'mechanical' ? mechanicalSpecies : bioSpecies;
  const spec = pool[id % pool.length];
  const level = Math.floor(Math.random() * 5) + 1;
  const rarity = level >= 4 ? 'rare' : 'common';

  return {
    id: `chimere-${id}`,
    name: `${spec.name} Lvl.${level}`,
    species: spec.name,
    type,
    rarity,
    level,
    hp: spec.hp + level * 10,
    maxHp: spec.hp + level * 10,
    attack: spec.attack + level * 3,
    defense: spec.def + level * 2,
    speed: 20 + level * 2,
    catchRate: Math.max(25, 75 - level * 10),
    abilities: spec.abilities.map((abName, i) => ({
      id: `ab-${i}`,
      name: abName,
      damage: 18 + i * 10 + level * 2,
      cooldown: i * 2,
      description: `Capacité signature de ${spec.name}`
    })),
    role: 'combat',
    captured: false,
    discoveredAt: new Date().toLocaleTimeString(),
    osmContext: `Détecté à proximité de : ${contextName}`,
    lat,
    lon,
    avatarIcon: spec.avatar,
    lore: spec.lore
  };
}

// Generate Uber H3 Hexagonal Grid projection approximation (Resolution 9 ~100m)
export function generateH3HexGrid(
  centerLat: number,
  centerLon: number,
  radiusMeters: number,
  lootSpots: LootSpot[],
  chimeres: Chimere[]
): H3Tile[] {
  const tiles: H3Tile[] = [];
  const hexRadiusMeters = 70; // ~100m diameter
  const hexRadiusLat = hexRadiusMeters / 111320;
  const hexRadiusLon = hexRadiusMeters / (40075000 * Math.cos((centerLat * Math.PI) / 180) / 360);

  const steps = Math.ceil(radiusMeters / (hexRadiusMeters * 1.5));

  for (let q = -steps; q <= steps; q++) {
    for (let r = -steps; r <= steps; r++) {
      // Axial to Cartesian conversion for Hexagon center
      const latOffset = (Math.sqrt(3) * q + (Math.sqrt(3) / 2) * r) * hexRadiusLat;
      const lonOffset = ((3 / 2) * r) * hexRadiusLon;

      const cLat = centerLat + latOffset;
      const cLon = centerLon + lonOffset;

      const dist = getDistanceMeters(centerLat, centerLon, cLat, cLon);
      if (dist > radiusMeters) continue;

      // Hexagon 6 vertices
      const polygon: [number, number][] = [];
      for (let i = 0; i < 6; i++) {
        const angle = (Math.PI / 180) * (60 * i);
        const pLat = cLat + (hexRadiusLat * 0.95) * Math.sin(angle);
        const pLon = cLon + (hexRadiusLon * 0.95) * Math.cos(angle);
        polygon.push([pLat, pLon]);
      }

      const hexIndex = `89${Math.abs(q).toString(16).padStart(2, '0')}${Math.abs(r).toString(16).padStart(2, '0')}ffffff`;

      const lootsInHex = lootSpots.filter(l => getDistanceMeters(cLat, cLon, l.lat, l.lon) < hexRadiusMeters).length;
      const chimeresInHex = chimeres.filter(c => c.lat && c.lon && getDistanceMeters(cLat, cLon, c.lat, c.lon) < hexRadiusMeters).length;

      tiles.push({
        index: hexIndex,
        center: [cLat, cLon],
        polygon,
        explored: dist < 120, // Player reveals nearby hexes
        radiationLevel: Math.floor((Math.sin(q * 3) + Math.cos(r * 2) + 2) * 15),
        lootDensity: lootsInHex,
        chimereCount: chimeresInHex,
        lootCount: lootsInHex
      });
    }
  }

  return tiles;
}

// Convert parsed buildings into Wavefront .OBJ 3D file for Godot/Unity export
export function exportToWavefrontObj(buildings: ParsedBuilding[], centerLat: number, centerLon: number): string {
  let obj = `# OpenStreetMap Procedural Post-Apo Voxel Exporter (AI Studio Build)\n`;
  obj += `# Center: ${centerLat}, ${centerLon}\n`;
  obj += `# Total Extruded Buildings: ${buildings.length}\n\n`;

  let vertexOffset = 1;

  buildings.forEach((b, bIdx) => {
    obj += `o Building_${b.id}_${bIdx}\n`;

    const baseVertices: { x: number; y: number; z: number }[] = [];
    const topVertices: { x: number; y: number; z: number }[] = [];

    // Extrude building footprint
    b.polygon.forEach(([lat, lon]) => {
      const { x, z } = latLonToLocalMeters(lat, lon, centerLat, centerLon);
      baseVertices.push({ x, y: 0, z });
      topVertices.push({ x, y: b.height, z });
    });

    // Write Vertices
    baseVertices.forEach(v => {
      obj += `v ${v.x.toFixed(2)} ${v.y.toFixed(2)} ${v.z.toFixed(2)}\n`;
    });
    topVertices.forEach(v => {
      obj += `v ${v.x.toFixed(2)} ${v.y.toFixed(2)} ${v.z.toFixed(2)}\n`;
    });

    const count = baseVertices.length;
    // Bottom & Top Caps (triangulated simple fan)
    for (let i = 1; i < count - 1; i++) {
      obj += `f ${vertexOffset} ${vertexOffset + i + 1} ${vertexOffset + i}\n`; // Bottom
      obj += `f ${vertexOffset + count} ${vertexOffset + count + i} ${vertexOffset + count + i + 1}\n`; // Top
    }

    // Side Walls
    for (let i = 0; i < count; i++) {
      const next = (i + 1) % count;
      const b1 = vertexOffset + i;
      const b2 = vertexOffset + next;
      const t1 = vertexOffset + count + i;
      const t2 = vertexOffset + count + next;
      obj += `f ${b1} ${b2} ${t2}\n`;
      obj += `f ${b1} ${t2} ${t1}\n`;
    }

    vertexOffset += count * 2;
    obj += `\n`;
  });

  return obj;
}
