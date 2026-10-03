import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  PlayerState,
  BunkerState,
  LootSpot,
  Chimere,
  H3Tile,
  Item,
  ParsedBuilding,
  ParsedRoad,
  ParsedPark,
  ChimereRole
} from './types/game';
import {
  fetchOverpassData,
  parseOsmData,
  getDistanceMeters,
  OSM_PRESETS
} from './services/osmParser';
import { soundFx } from './services/soundFx';
import { Header, AppViewMode } from './components/Header';
import { MapViewer3D } from './components/MapViewer3D';
import { NomadRadarView } from './components/NomadRadarView';
import { BunkerBaseView } from './components/BunkerBaseView';
import { OsmScriptStudio } from './components/OsmScriptStudio';
import { MicroVoxelStudio } from './components/MicroVoxelStudio';
import { RoadmapPhasesView } from './components/RoadmapPhasesView';
import { ChimereCombatModal } from './components/ChimereCombatModal';
import { InventoryModal } from './components/InventoryModal';
import { QuickHelpModal } from './components/QuickHelpModal';

export const App: React.FC = () => {
  // Current view mode
  const [currentMode, setCurrentMode] = useState<AppViewMode>('nomad');
  const [isAudioOn, setIsAudioOn] = useState<boolean>(true);

  // Active Map Location (Default to Tour Eiffel Paris preset for instant rich OSM testing)
  const [centerLat, setCenterLat] = useState<number>(48.8584);
  const [centerLon, setCenterLon] = useState<number>(2.2945);
  const [queryRadius, setQueryRadius] = useState<number>(450);
  const [isLoadingOsm, setIsLoadingOsm] = useState<boolean>(true);

  // Parsed OSM Data
  const [buildings, setBuildings] = useState<ParsedBuilding[]>([]);
  const [roads, setRoads] = useState<ParsedRoad[]>([]);
  const [parks, setParks] = useState<ParsedPark[]>([]);
  const [lootSpots, setLootSpots] = useState<LootSpot[]>([]);
  const [chimeres, setChimeres] = useState<Chimere[]>([]);
  const [h3Tiles, setH3Tiles] = useState<H3Tile[]>([]);

  // Modals
  const [activeCombatChimere, setActiveCombatChimere] = useState<Chimere | null>(null);
  const [isInventoryOpen, setIsInventoryOpen] = useState<boolean>(false);
  const [isHelpOpen, setIsHelpOpen] = useState<boolean>(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Real GPS vs Virtual
  const [isRealGpsActive, setIsRealGpsActive] = useState<boolean>(false);
  const watchIdRef = useRef<number | null>(null);

  // Player State
  const [player, setPlayer] = useState<PlayerState>({
    lat: 48.8584,
    lon: 2.2945,
    heading: 45,
    speed: 1.2,
    health: 100,
    maxHealth: 100,
    radiation: 8,
    stamina: 100,
    maxStamina: 100,
    currentWeight: 4.8,
    maxWeight: 25.0,
    inventory: [
      {
        id: 'start-stim-1',
        name: 'Stimpack Coagulant',
        category: 'medical',
        weight: 0.3,
        quantity: 2,
        rarity: 'uncommon',
        icon: '💉',
        healAmount: 45,
        description: 'Soin d’urgence restaure 45 PV.'
      },
      {
        id: 'start-trap-1',
        name: 'Piège à Impulsion IEM',
        category: 'trap',
        weight: 1.0,
        quantity: 2,
        rarity: 'rare',
        icon: '🪤',
        description: 'Neutralise et capture les Chimères.'
      },
      {
        id: 'start-rad-1',
        name: 'Pilules Antirad Rad-X',
        category: 'medical',
        weight: 0.1,
        quantity: 2,
        rarity: 'rare',
        icon: '💊',
        radCleanse: 30,
        description: 'Absorbe 30 rads de contamination.'
      }
    ],
    chimeres: [],
    distanceWalkedMeters: 140,
    scavengeCount: 0,
    credits: 150,
    level: 1,
    xp: 60,
    activeTrapCount: 2
  });

  // Bunker Base Camp State
  const [bunker, setBunker] = useState<BunkerState>({
    level: 1,
    name: 'Abri Alpha (Base Sédentaire)',
    lat: 48.8584,
    lon: 2.2945,
    energy: 120,
    maxEnergy: 200,
    energyProduction: 25,
    energyConsumption: 10,
    chestStorage: [
      {
        id: 'chest-scrap-1',
        name: 'Ferraille Renforcée',
        category: 'scrap',
        weight: 2.0,
        quantity: 8,
        rarity: 'common',
        icon: '⚙️',
        description: 'Matériau pour agrandir le bunker.'
      },
      {
        id: 'chest-elec-1',
        name: 'Composants Récupérés',
        category: 'electronics',
        weight: 1.0,
        quantity: 5,
        rarity: 'common',
        icon: '🔩',
        description: 'Visserie et câbles.'
      }
    ],
    defenses: {
      turretCount: 1,
      barricadeHp: 350,
      shieldActive: true
    },
    workbenchLevel: 1
  });

  const showToast = useCallback((msg: string) => {
    setToastMessage(msg);
    setTimeout(() => {
      setToastMessage(prev => (prev === msg ? null : prev));
    }, 2800);
  }, []);

  // Load OSM Data from Overpass or Presets
  const loadOsmData = useCallback(async (lat: number, lon: number, radius: number) => {
    setIsLoadingOsm(true);
    try {
      const rawData = await fetchOverpassData(lat, lon, radius);
      const parsed = parseOsmData(rawData, lat, lon);

      setBuildings(parsed.buildings);
      setRoads(parsed.roads);
      setParks(parsed.parks);
      setLootSpots(parsed.lootSpots);
      setChimeres(parsed.chimeres);
      setH3Tiles(parsed.h3Tiles);

      setCenterLat(lat);
      setCenterLon(lon);
      setQueryRadius(radius);

      showToast(`Données OSM chargées : ${parsed.buildings.length} bâtiments, ${parsed.lootSpots.length} loots`);
    } catch {
      showToast("Génération procédurale de secours activée.");
    } finally {
      setIsLoadingOsm(false);
    }
  }, [showToast]);

  // Initial Load on Mount
  useEffect(() => {
    loadOsmData(centerLat, centerLon, queryRadius);
  }, []);

  // Sync Audio Setting with Synthesizer
  const handleToggleAudio = () => {
    const next = !isAudioOn;
    setIsAudioOn(next);
    soundFx.enabled = next;
    if (next) soundFx.playRadarPing();
  };

  // Toggle Real Smartphone GPS
  const handleToggleGps = () => {
    if (isRealGpsActive) {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current);
        watchIdRef.current = null;
      }
      setIsRealGpsActive(false);
      showToast("GPS Smartphone désactivé. Mode simulateur actif.");
    } else {
      if (!('geolocation' in navigator)) {
        showToast("Géolocalisation non supportée sur ce navigateur.");
        return;
      }

      const id = navigator.geolocation.watchPosition(
        (pos) => {
          const { latitude, longitude, heading, speed } = pos.coords;
          setPlayer(prev => {
            const dist = Math.round(getDistanceMeters(prev.lat, prev.lon, latitude, longitude));
            return {
              ...prev,
              lat: latitude,
              lon: longitude,
              heading: heading || prev.heading,
              speed: speed || prev.speed,
              distanceWalkedMeters: prev.distanceWalkedMeters + (dist < 100 ? dist : 0)
            };
          });
        },
        () => {
          showToast("Signal GPS indisponible. Mode simulateur conservé.");
          setIsRealGpsActive(false);
        },
        { enableHighAccuracy: true, maximumAge: 2000, timeout: 5000 }
      );

      watchIdRef.current = id;
      setIsRealGpsActive(true);
      showToast("GPS Smartphone activé ! Suivi de la marche en cours.");
    }
  };

  // Virtual Step Movement
  const handleVirtualStep = (dLon: number, dLat: number) => {
    setPlayer(prev => {
      const newLat = prev.lat + dLat;
      const newLon = prev.lon + dLon;
      const angle = Math.atan2(dLon, dLat) * (180 / Math.PI);
      const dist = Math.round(getDistanceMeters(prev.lat, prev.lon, newLat, newLon));

      soundFx.playRadarPing(660);

      return {
        ...prev,
        lat: newLat,
        lon: newLon,
        heading: angle >= 0 ? angle : 360 + angle,
        distanceWalkedMeters: prev.distanceWalkedMeters + dist
      };
    });
  };

  // Move player directly (e.g. from 3D Map Ground Click)
  const handlePlayerMove = (newLat: number, newLon: number) => {
    setPlayer(prev => {
      const dist = Math.round(getDistanceMeters(prev.lat, prev.lon, newLat, newLon));
      return {
        ...prev,
        lat: newLat,
        lon: newLon,
        distanceWalkedMeters: prev.distanceWalkedMeters + dist
      };
    });
    soundFx.playRadarPing(750);
  };

  // Loot Pickup (1-Tap Fast Nomad Scavenging)
  const handleLootPickup = (loot: LootSpot) => {
    setLootSpots(prev =>
      prev.map(l => (l.id === loot.id ? { ...l, looted: true } : l))
    );

    let addedWeight = 0;
    loot.items.forEach(i => (addedWeight += i.weight * i.quantity));

    setPlayer(prev => {
      const newInv = [...prev.inventory];
      loot.items.forEach(newItem => {
        const existing = newInv.find(i => i.name === newItem.name);
        if (existing) {
          existing.quantity += newItem.quantity;
        } else {
          newInv.push({ ...newItem });
        }
      });

      const newWeight = prev.currentWeight + addedWeight;
      const newXp = prev.xp + 25;
      const newLevel = Math.floor(newXp / 100) + 1;

      return {
        ...prev,
        inventory: newInv,
        currentWeight: newWeight,
        scavengeCount: prev.scavengeCount + 1,
        xp: newXp,
        level: newLevel
      };
    });

    showToast(`📦 Récolté : ${loot.items.map(i => `${i.name} x${i.quantity}`).join(', ')} (+25 XP)`);
  };

  // Engage Chimère Encounter
  const handleEngageChimere = (chimere: Chimere) => {
    setActiveCombatChimere(chimere);
  };

  // Capture Success
  const handleCaptureSuccess = (chimere: Chimere, xpGained: number) => {
    setChimeres(prev =>
      prev.map(c => (c.id === chimere.id ? { ...c, captured: true } : c))
    );

    const capturedChimere: Chimere = {
      ...chimere,
      captured: true,
      role: 'combat'
    };

    setPlayer(prev => {
      const newXp = prev.xp + xpGained;
      const newLevel = Math.floor(newXp / 100) + 1;
      return {
        ...prev,
        chimeres: [...prev.chimeres, capturedChimere],
        xp: newXp,
        level: newLevel
      };
    });

    setActiveCombatChimere(null);
    showToast(`🎉 Chimère ${chimere.name} capturée et ajoutée à votre Sanctum ! (+${xpGained} XP)`);
  };

  // Defeat Chimère Success
  const handleDefeatSuccess = (chimere: Chimere, drop: Item[], xpGained: number) => {
    setChimeres(prev =>
      prev.map(c => (c.id === chimere.id ? { ...c, captured: true } : c))
    );

    setPlayer(prev => {
      const newInv = [...prev.inventory, ...drop];
      const newXp = prev.xp + xpGained;
      const newLevel = Math.floor(newXp / 100) + 1;
      return {
        ...prev,
        inventory: newInv,
        xp: newXp,
        level: newLevel
      };
    });

    setActiveCombatChimere(null);
    showToast(`💀 ${chimere.name} vaincu ! Matériaux rares récupérés (+${xpGained} XP)`);
  };

  // Player Takes Damage in Combat
  const handlePlayerTakeDamage = (damage: number) => {
    setPlayer(prev => ({
      ...prev,
      health: Math.max(0, prev.health - damage)
    }));
  };

  // Use Item from Backpack
  const handleUseItem = (item: Item) => {
    setPlayer(prev => {
      let newHealth = prev.health;
      let newRad = prev.radiation;

      if (item.healAmount) {
        newHealth = Math.min(prev.maxHealth, prev.health + item.healAmount);
        showToast(`💉 Soin appliqué : +${item.healAmount} PV`);
      }
      if (item.radCleanse) {
        newRad = Math.max(0, prev.radiation - item.radCleanse);
        showToast(`💊 Antirad consommé : -${item.radCleanse} RAD`);
      }

      const newInv = prev.inventory
        .map(i => (i.id === item.id ? { ...i, quantity: i.quantity - 1 } : i))
        .filter(i => i.quantity > 0);

      return {
        ...prev,
        health: newHealth,
        radiation: newRad,
        inventory: newInv,
        currentWeight: Math.max(0, prev.currentWeight - item.weight)
      };
    });
  };

  // Drop Item from Backpack
  const handleDropItem = (item: Item) => {
    setPlayer(prev => ({
      ...prev,
      inventory: prev.inventory.filter(i => i.id !== item.id),
      currentWeight: Math.max(0, prev.currentWeight - item.weight * item.quantity)
    }));
    showToast(`🗑️ ${item.name} jeté au sol.`);
  };

  // Deposit Item to Bunker Chest
  const handleDepositToChest = (item: Item) => {
    setPlayer(prev => ({
      ...prev,
      inventory: prev.inventory.filter(i => i.id !== item.id),
      currentWeight: Math.max(0, prev.currentWeight - item.weight * item.quantity)
    }));

    setBunker(prev => {
      const chest = [...prev.chestStorage];
      const exist = chest.find(c => c.name === item.name);
      if (exist) {
        exist.quantity += item.quantity;
      } else {
        chest.push({ ...item });
      }
      return { ...prev, chestStorage: chest };
    });

    soundFx.playLootPickup();
    showToast(`📥 Déposé au coffre : ${item.name} x${item.quantity}`);
  };

  // Withdraw Item from Bunker Chest
  const handleWithdrawFromChest = (item: Item) => {
    setBunker(prev => ({
      ...prev,
      chestStorage: prev.chestStorage.filter(i => i.id !== item.id)
    }));

    setPlayer(prev => {
      const inv = [...prev.inventory];
      const exist = inv.find(i => i.name === item.name);
      if (exist) {
        exist.quantity += item.quantity;
      } else {
        inv.push({ ...item });
      }
      return {
        ...prev,
        inventory: inv,
        currentWeight: prev.currentWeight + item.weight * item.quantity
      };
    });

    soundFx.playLootPickup();
    showToast(`📤 Retiré du coffre : ${item.name} x${item.quantity}`);
  };

  // Craft Item at Workbench
  const handleCraftItem = (recipeId: string, resultItem: Item, costs: { itemId: string; count: number }[]) => {
    // Add crafted item to player backpack
    setPlayer(prev => {
      const inv = [...prev.inventory, { ...resultItem }];
      return {
        ...prev,
        inventory: inv,
        currentWeight: prev.currentWeight + resultItem.weight,
        xp: prev.xp + 30
      };
    });
    showToast(`🔨 Fabrication réussie : ${resultItem.name} (+30 XP)`);
  };

  // Assign Chimère Role in Bunker Sanctum
  const handleAssignChimereRole = (chimereId: string, role: ChimereRole) => {
    setPlayer(prev => ({
      ...prev,
      chimeres: prev.chimeres.map(c => (c.id === chimereId ? { ...c, role } : c))
    }));
    soundFx.playRadarPing(800);
    showToast(`✨ Rôle mis à jour pour la Chimère.`);
  };

  // Upgrade Bunker
  const handleUpgradeBunkerFacility = (facility: 'core' | 'energy' | 'defenses' | 'workbench') => {
    setBunker(prev => {
      if (facility === 'core') return { ...prev, level: prev.level + 1, maxEnergy: prev.maxEnergy + 50 };
      if (facility === 'defenses') return { ...prev, defenses: { ...prev.defenses, barricadeHp: prev.defenses.barricadeHp + 100, turretCount: prev.defenses.turretCount + 1 } };
      return prev;
    });
    soundFx.playCaptureSuccess();
    showToast(`🏛️ Bunker amélioré avec succès !`);
  };

  return (
    <div className="flex flex-col h-screen w-screen bg-slate-950 text-slate-100 overflow-hidden select-none">
      {/* Top Main Navigation Header */}
      <Header
        currentMode={currentMode}
        onSelectMode={setCurrentMode}
        onOpenInventory={() => setIsInventoryOpen(true)}
        onOpenHelp={() => setIsHelpOpen(true)}
        inventoryCount={player.inventory.reduce((acc, i) => acc + i.quantity, 0)}
        currentWeight={player.currentWeight}
        maxWeight={player.maxWeight}
        isAudioOn={isAudioOn}
        onToggleAudio={handleToggleAudio}
        level={player.level}
        xp={player.xp}
      />

      {/* Main Content Area */}
      <main className="flex-1 relative overflow-hidden">
        {/* MODE 1: NOMAD RADAR & OUTDOOR WALKING */}
        {currentMode === 'nomad' && (
          <NomadRadarView
            player={player}
            lootSpots={lootSpots}
            chimeres={chimeres}
            h3Tiles={h3Tiles}
            onLootPickup={handleLootPickup}
            onEngageChimere={handleEngageChimere}
            onVirtualStep={handleVirtualStep}
            isRealGpsActive={isRealGpsActive}
            onToggleGps={handleToggleGps}
          />
        )}

        {/* MODE 2: 3D VOXEL TACTICAL MAP */}
        {currentMode === 'map3d' && (
          <MapViewer3D
            buildings={buildings}
            roads={roads}
            parks={parks}
            lootSpots={lootSpots}
            chimeres={chimeres}
            h3Tiles={h3Tiles}
            player={player}
            centerLat={centerLat}
            centerLon={centerLon}
            onLootClick={handleLootPickup}
            onChimereClick={handleEngageChimere}
            onPlayerMove={handlePlayerMove}
          />
        )}

        {/* MODE 3: MICRO-VOXEL ENGINE & GREEDY MESHING (GODOT 4) */}
        {currentMode === 'microvoxel' && (
          <MicroVoxelStudio
            onCollectLoot={(matName, count) => {
              showToast(`⛏️ Forage réussi : ${matName} x${count} récupéré !`);
              setPlayer(prev => {
                const newInv = [...prev.inventory];
                const existing = newInv.find(i => i.name.includes(matName));
                if (existing) {
                  existing.quantity += count;
                } else {
                  newInv.push({
                    id: `loot-${Date.now()}`,
                    name: matName,
                    category: 'scrap',
                    weight: 0.4,
                    quantity: count,
                    rarity: 'rare',
                    icon: '🧱',
                    description: 'Ressource extraite par forage micro-voxel.'
                  });
                }
                return { ...prev, inventory: newInv, xp: prev.xp + 15 };
              });
            }}
          />
        )}

        {/* MODE 4: BUNKER BASE SEDENTAIRE & SANCTUM */}
        {currentMode === 'bunker' && (
          <BunkerBaseView
            bunker={bunker}
            player={player}
            onDepositToChest={handleDepositToChest}
            onWithdrawFromChest={handleWithdrawFromChest}
            onCraftItem={handleCraftItem}
            onAssignChimereRole={handleAssignChimereRole}
            onUpgradeBunkerFacility={handleUpgradeBunkerFacility}
          />
        )}

        {/* MODE 4: OSM SCRIPT STUDIO & EXPORTER */}
        {currentMode === 'studio' && (
          <OsmScriptStudio
            currentLat={centerLat}
            currentLon={centerLon}
            currentRadius={queryRadius}
            buildings={buildings}
            lootSpots={lootSpots}
            chimeres={chimeres}
            onLoadLocation={loadOsmData}
            isLoading={isLoadingOsm}
          />
        )}

        {/* MODE 5: COMPLETE 6-PHASE ROADMAP */}
        {currentMode === 'roadmap' && <RoadmapPhasesView />}
      </main>

      {/* Interactive Combat & Capture Modal */}
      {activeCombatChimere && (
        <ChimereCombatModal
          chimere={activeCombatChimere}
          player={player}
          onClose={() => setActiveCombatChimere(null)}
          onCaptureSuccess={handleCaptureSuccess}
          onDefeatSuccess={handleDefeatSuccess}
          onPlayerTakeDamage={handlePlayerTakeDamage}
        />
      )}

      {/* Inventory Backpack Modal */}
      {isInventoryOpen && (
        <InventoryModal
          player={player}
          onClose={() => setIsInventoryOpen(false)}
          onUseItem={handleUseItem}
          onDropItem={handleDropItem}
        />
      )}

      {/* Quick Help & Architecture Modal */}
      {isHelpOpen && <QuickHelpModal onClose={() => setIsHelpOpen(false)} />}

      {/* Floating Toast Notification */}
      {toastMessage && (
        <div className="absolute bottom-6 left-1/2 transform -translate-x-1/2 z-50 bg-slate-900/95 border-2 border-amber-500 text-amber-300 px-4 py-2.5 rounded-2xl shadow-[0_0_25px_rgba(245,158,11,0.3)] text-xs font-mono flex items-center gap-2 animate-bounce">
          <span>☢️</span>
          <span>{toastMessage}</span>
        </div>
      )}
    </div>
  );
};
export default App;
