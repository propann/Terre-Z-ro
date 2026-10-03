export type ChimereType = 'bio' | 'mechanical';
export type ChimereRole = 'combat' | 'energy' | 'tracking' | 'transport' | 'idle';
export type ChimereRarity = 'common' | 'rare' | 'apex';

export interface ChimereAbility {
  id: string;
  name: string;
  damage: number;
  effect?: 'stun' | 'bleed' | 'emp' | 'heal';
  cooldown: number;
  description: string;
}

export interface Chimere {
  id: string;
  name: string;
  species: string;
  type: ChimereType;
  rarity: ChimereRarity;
  level: number;
  hp: number;
  maxHp: number;
  attack: number;
  defense: number;
  speed: number;
  catchRate: number; // 0-100%
  abilities: ChimereAbility[];
  role: ChimereRole;
  captured: boolean;
  discoveredAt: string;
  osmContext: string; // e.g. "Zone Industrielle désaffectée" or "Parc Botanique muté"
  lat?: number;
  lon?: number;
  avatarIcon: string;
  lore: string;
}

export type ItemCategory = 'medical' | 'food' | 'scrap' | 'electronics' | 'weapon' | 'module' | 'trap';

export interface Item {
  id: string;
  name: string;
  category: ItemCategory;
  description: string;
  weight: number; // kg
  quantity: number;
  rarity: 'common' | 'uncommon' | 'rare' | 'military';
  icon: string;
  healAmount?: number;
  radCleanse?: number;
  powerValue?: number;
  craftRecipe?: { itemId: string; count: number }[];
}

export interface LootSpot {
  id: string;
  name: string;
  lat: number;
  lon: number;
  distance?: number;
  category: 'medical' | 'food' | 'weapons' | 'electronics' | 'fuel' | 'scrap';
  osmTag: string;
  items: Item[];
  looted: boolean;
  respawnTime?: number;
  dangerLevel: number; // 1-5
}

export interface ParsedBuilding {
  id: number;
  name?: string;
  polygon: [number, number][]; // [lat, lon]
  height: number;
  levels: number;
  type: 'residential' | 'commercial' | 'industrial' | 'hospital' | 'ruins' | 'fortress';
  tags: Record<string, string>;
  centroid: [number, number]; // [lat, lon]
}

export interface ParsedRoad {
  id: number;
  name?: string;
  points: [number, number][]; // [lat, lon]
  highwayType: string;
  isCrevassed?: boolean;
}

export interface ParsedPark {
  id: number;
  name?: string;
  polygon: [number, number][];
}

export interface H3Tile {
  index: string;
  center: [number, number];
  polygon: [number, number][];
  explored: boolean;
  radiationLevel: number; // 0 to 100 rads
  chimereCount: number;
  lootCount: number;
  lootDensity: number;
}

export interface PlayerState {
  lat: number;
  lon: number;
  heading: number;
  speed: number;
  health: number;
  maxHealth: number;
  radiation: number; // 0-100 rads
  stamina: number;
  maxStamina: number;
  currentWeight: number;
  maxWeight: number; // kg
  inventory: Item[];
  chimeres: Chimere[];
  distanceWalkedMeters: number;
  scavengeCount: number;
  credits: number;
  level: number;
  xp: number;
  activeTrapCount: number;
}

export interface BunkerState {
  level: number;
  name: string;
  lat: number;
  lon: number;
  energy: number;
  maxEnergy: number;
  energyProduction: number; // /hr from generator + chimeres
  energyConsumption: number;
  chestStorage: Item[];
  defenses: {
    turretCount: number;
    barricadeHp: number;
    shieldActive: boolean;
  };
  workbenchLevel: number;
}

export interface OSMQueryPreset {
  name: string;
  city: string;
  lat: number;
  lon: number;
  radiusMeters: number;
  description: string;
}
