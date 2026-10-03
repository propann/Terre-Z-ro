export const PYTHON_OSM_EXTRACTOR_SCRIPT = `#!/usr/bin/env python3
"""
OSM Extractor & Procedural Voxel Parser (Projet Chimères Post-Apo)
Étape 1 du Prototype : Extraction Overpass, découpage H3, tables de loot et export Godot/Unity.

Installation des dépendances :
pip install requests h3 shapely
"""

import math
import json
import requests
from dataclasses import dataclass, asdict
from typing import List, Dict, Any, Optional

OVERPASS_URL = "https://overpass-api.de/api/interpreter"

@dataclass
class LootSpot:
    id: str
    name: str
    lat: float
    lon: float
    category: str
    osm_tag: str
    items: List[Dict[str, Any]]
    danger_level: int

@dataclass
class Building3D:
    id: int
    name: Optional[str]
    polygon: List[List[float]] # [[lat, lon], ...]
    height: float
    levels: int
    building_type: str
    tags: Dict[str, str]

def fetch_osm_data(lat: float, lon: float, radius_m: int = 500) -> Dict[str, Any]:
    """Interroge l'API Overpass pour récupérer les polygones et POI dans un rayon donné."""
    print(f"[+] Téléchargement des vecteurs OSM autour de ({lat}, {lon}) - Rayon {radius_m}m...")
    
    query = f"""
    [out:json][timeout:25];
    (
      way["building"](around:{radius_m},{lat},{lon});
      way["highway"](around:{radius_m},{lat},{lon});
      node["amenity"](around:{radius_m},{lat},{lon});
      way["amenity"](around:{radius_m},{lat},{lon});
      node["shop"](around:{radius_m},{lat},{lon});
      way["shop"](around:{radius_m},{lat},{lon});
      way["leisure"="park"](around:{radius_m},{lat},{lon});
      way["landuse"="industrial"](around:{radius_m},{lat},{lon});
    );
    out body;
    >;
    out skel qt;
    """
    
    response = requests.post(OVERPASS_URL, data={"data": query})
    response.raise_for_status()
    return response.json()

def map_tag_to_loot(tags: Dict[str, str], lat: float, lon: float, elem_id: int) -> Optional[LootSpot]:
    """Mappe un tag OSM en table de loot post-apocalyptique."""
    amenity = tags.get("amenity")
    shop = tags.get("shop")
    name = tags.get("name", "Ruines Inconnues")
    
    if amenity in ["pharmacy", "hospital", "clinic"]:
        return LootSpot(
            id=f"loot-{elem_id}",
            name=name or "Pharmacie / Clinique Déchue",
            lat=lat,
            lon=lon,
            category="medical",
            osm_tag=f"amenity={amenity}",
            items=[
                {"name": "Stimpack Coagulant", "qty": 2, "weight_kg": 0.3},
                {"name": "Rad-X Antirad", "qty": 3, "weight_kg": 0.1}
            ],
            danger_level=2
        )
    elif amenity in ["restaurant", "fast_food", "cafe"] or shop in ["supermarket", "bakery", "convenience"]:
        return LootSpot(
            id=f"loot-{elem_id}",
            name=name or "Rations & Comestibles",
            lat=lat,
            lon=lon,
            category="food",
            osm_tag=f"shop={shop or amenity}",
            items=[
                {"name": "Ration MRE Militaire", "qty": 2, "weight_kg": 0.8},
                {"name": "Eau Purifiée", "qty": 1, "weight_kg": 1.0}
            ],
            danger_level=1
        )
    elif shop in ["hardware", "electronics", "computer"] or tags.get("landuse") == "industrial":
        return LootSpot(
            id=f"loot-{elem_id}",
            name=name or "Atelier Électronique & Pièces",
            lat=lat,
            lon=lon,
            category="electronics",
            osm_tag=f"shop={shop or 'industrial'}",
            items=[
                {"name": "Batterie Haute Densité", "qty": 2, "weight_kg": 0.6},
                {"name": "Pièces de Drone / Servos", "qty": 1, "weight_kg": 0.4},
                {"name": "Ferraille Voxel", "qty": 4, "weight_kg": 2.0}
            ],
            danger_level=3
        )
    elif amenity in ["police", "bank"] or tags.get("building") == "military":
        return LootSpot(
            id=f"loot-{elem_id}",
            name=name or "Cache Sécurisée",
            lat=lat,
            lon=lon,
            category="weapons",
            osm_tag=f"amenity={amenity}",
            items=[
                {"name": "Piège IEM Anti-Chimère", "qty": 1, "weight_kg": 1.5},
                {"name": "Plaque de Blindage", "qty": 1, "weight_kg": 2.5}
            ],
            danger_level=4
        )
    return None

def parse_osm_pipeline(raw_json: Dict[str, Any]) -> Dict[str, Any]:
    """Parse le JSON OSM brut en structures exploitables par le moteur 3D."""
    nodes = {}
    buildings: List[Building3D] = []
    loot_spots: List[LootSpot] = []
    
    for elem in raw_json.get("elements", []):
        if elem["type"] == "node":
            nodes[elem["id"]] = (elem["lat"], elem["lon"])
            if "tags" in elem:
                loot = map_tag_to_loot(elem["tags"], elem["lat"], elem["lon"], elem["id"])
                if loot:
                    loot_spots.append(loot)
                    
    for elem in raw_json.get("elements", []):
        if elem["type"] == "way" and "tags" in elem:
            tags = elem["tags"]
            coords = [nodes[n] for n in elem.get("nodes", []) if n in nodes]
            if not coords:
                continue
                
            if "building" in tags:
                levels = int(tags.get("building:levels", 1))
                height = float(tags.get("height", levels * 3.5))
                b_type = "industrial" if tags.get("landuse") == "industrial" else "residential"
                
                buildings.append(Building3D(
                    id=elem["id"],
                    name=tags.get("name"),
                    polygon=coords,
                    height=height,
                    levels=levels,
                    building_type=b_type,
                    tags=tags
                ))
                
                # Check for building-level loot
                avg_lat = sum(c[0] for c in coords) / len(coords)
                avg_lon = sum(c[1] for c in coords) / len(coords)
                loot = map_tag_to_loot(tags, avg_lat, avg_lon, elem["id"])
                if loot:
                    loot_spots.append(loot)
                    
    print(f"[✓] Parsing terminé : {len(buildings)} bâtiments extrudables, {len(loot_spots)} points de loot générés.")
    return {
        "buildings": [asdict(b) for b in buildings],
        "loot_spots": [asdict(l) for l in loot_spots]
    }

if __name__ == "__main__":
    # Exemple sur le quartier Tour Eiffel / Champ de Mars (Paris)
    LAT = 48.8584
    LON = 2.2945
    
    raw = fetch_osm_data(LAT, LON, radius_m=400)
    result = parse_osm_pipeline(raw)
    
    # Sauvegarde au format JSON prêt pour Godot / Unity
    output_filename = "chimeres_world_data.json"
    with open(output_filename, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)
        
    print(f"[✓] Données exportées avec succès dans '{output_filename}' !")
`;

export const NODE_OSM_EXTRACTOR_SCRIPT = `/**
 * OSM Extractor & Procedural Voxel Parser (Projet Chimères Post-Apo)
 * Node.js / TypeScript version
 * 
 * Usage:
 *   node osm_extractor.mjs
 */

import fs from 'node:fs';

const OVERPASS_URL = "https://overpass-api.de/api/interpreter";

async function fetchOsmNeighborhood(lat, lon, radiusMeters = 500) {
  console.log(\`[+] Interrogation Overpass API @ (\${lat}, \${lon}) - \${radiusMeters}m...\`);
  const query = \`
    [out:json][timeout:25];
    (
      way["building"](around:\${radiusMeters},\${lat},\${lon});
      way["highway"](around:\${radiusMeters},\${lat},\${lon});
      node["amenity"](around:\${radiusMeters},\${lat},\${lon});
      way["amenity"](around:\${radiusMeters},\${lat},\${lon});
      node["shop"](around:\${radiusMeters},\${lat},\${lon});
      way["shop"](around:\${radiusMeters},\${lat},\${lon});
    );
    out body;
    >;
    out skel qt;
  \`;

  const response = await fetch(OVERPASS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: 'data=' + encodeURIComponent(query)
  });

  if (!response.ok) throw new Error(\`Erreur HTTP Overpass: \${response.statusText}\`);
  return await response.json();
}

function processOsmData(data) {
  const nodes = new Map();
  const buildings = [];
  const lootSpots = [];

  for (const el of data.elements) {
    if (el.type === 'node') {
      nodes.set(el.id, [el.lat, el.lon]);
    }
  }

  for (const el of data.elements) {
    if (el.type === 'way' && el.tags && el.tags.building) {
      const coords = el.nodes.map(id => nodes.get(id)).filter(Boolean);
      const levels = parseInt(el.tags['building:levels'] || '1', 10);
      const height = parseFloat(el.tags.height || (levels * 3.5).toString());

      buildings.push({
        id: el.id,
        name: el.tags.name || null,
        polygon: coords,
        height,
        levels,
        tags: el.tags
      });
    }
  }

  return { buildings, totalBuildings: buildings.length };
}

// Run
(async () => {
  const parisLat = 48.8584;
  const parisLon = 2.2945;
  const raw = await fetchOsmNeighborhood(parisLat, parisLon, 450);
  const parsed = processOsmData(raw);
  fs.writeFileSync('chimeres_osm_export.json', JSON.stringify(parsed, null, 2));
  console.log(\`[✓] \${parsed.totalBuildings} bâtiments exportés dans 'chimeres_osm_export.json'\`);
})();
`;
