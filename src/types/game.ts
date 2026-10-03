export type ChimereType = 'bio' | 'mechanical';
export type ChimereArchetype = 'techno_heavy' | 'techno_network' | 'bio_terrestrial' | 'bio_aquatic';
export type ChimereRole = 'combat' | 'energy' | 'tracking' | 'transport' | 'mining' | 'idle';
export type ChimereRarity = 'common' | 'rare' | 'apex';

export interface ChimereAnatomyPart {
  id: 'legs' | 'tank' | 'armor' | 'core';
  name: string;
  hp: number;
  maxHp: number;
  broken: boolean;
  effectDesc: string;
}

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
  archetype?: ChimereArchetype;
  rarity: ChimereRarity;
  level: number;
  hp: number;
  maxHp: number;
  attack: number;
  defense: number;
  speed: number;
  catchRate: number; // 0-100%
  abilities: ChimereAbility[];
  anatomy?: ChimereAnatomyPart[];
  isOverkilled?: boolean;
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

export type VehicleChassisType = 'moto' | 'buggy' | 'truck_6x6';
export type VehicleDriveMode = 'manual_joystick' | 'autopilot_convoy';

export interface VehicleModule {
  slot: 'prow' | 'roof' | 'flanks' | 'flatbed';
  name: string;
  icon: string;
  effect: string;
  durability: number;
}

export interface VehicleState {
  id: string;
  name: string;
  chassis: VehicleChassisType;
  fuel: number; // 0-100%
  maxFuel: number;
  health: number;
  maxHealth: number;
  cargoCapacityKg: number;
  cargo: Item[];
  driveMode: VehicleDriveMode;
  modules: {
    prow?: VehicleModule;
    roof?: VehicleModule;
    flanks?: VehicleModule;
    flatbed?: VehicleModule;
  };
}

export type BiomutantMorphoStage = 'feral' | 'symbiote' | 'titan';

export interface TechnoideHardware {
  opticsLevel: 'basic' | 'military_infrared_50m';
  cpuOverclock: boolean;
  titaniumPlatingHp: number;
}

export interface PhysicalMilestone {
  km: number;
  traitName: string;
  effect: string;
  unlocked: boolean;
  icon: string;
}

export interface SkillPerk {
  id: string;
  branch: 'engineer' | 'biotracker' | 'combat';
  tier: number;
  name: string;
  desc: string;
  unlocked: boolean;
  cost: number;
  isUltimate?: boolean;
}

export interface CyberneticImplants {
  ocularLevel: number; // 1: Thermique, 2: Failles structurelles, 3: Spectromètre
  spinalInstalled: boolean; // Exosquelette dorsal (+charge, pas de malus forage lourd)
  cerebralInstalled: boolean; // -50% temps d'injection
  dermalArmorLevel: number; // Grille sous-cutanée (réduit acide/entailles)
}

export interface DeathCrate {
  id: string;
  lat: number;
  lon: number;
  items: Item[];
  droppedAt: number;
  expiresAt: number;
  recovered: boolean;
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
  activeVehicle?: VehicleState;
  distanceWalkedMeters: number;
  masteryPoints: number;
  unlockedPerks: string[];
  implants: CyberneticImplants;
  deathCrate?: DeathCrate;
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
