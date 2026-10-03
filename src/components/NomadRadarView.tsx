import React, { useState, useEffect } from 'react';
import {
  PlayerState,
  LootSpot,
  Chimere,
  H3Tile
} from '../types/game';
import { getDistanceMeters } from '../services/osmParser';
import { soundFx } from '../services/soundFx';
import {
  Compass,
  Zap,
  Activity,
  Package,
  Crosshair,
  Footprints,
  Shield,
  Radio,
  AlertTriangle,
  ChevronRight,
  Sun,
  Flame,
  Target,
  Vibrate,
  ArrowUp,
  ArrowDown,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';

interface NomadRadarViewProps {
  player: PlayerState;
  lootSpots: LootSpot[];
  chimeres: Chimere[];
  h3Tiles: H3Tile[];
  onLootPickup: (loot: LootSpot) => void;
  onEngageChimere: (chimere: Chimere) => void;
  onVirtualStep: (dx: number, dy: number) => void;
  isRealGpsActive: boolean;
  onToggleGps: () => void;
}

export const NomadRadarView: React.FC<NomadRadarViewProps> = ({
  player,
  lootSpots,
  chimeres,
  h3Tiles,
  onLootPickup,
  onEngageChimere,
  onVirtualStep,
  isRealGpsActive,
  onToggleGps
}) => {
  const [activeTab, setActiveTab] = useState<'radar' | 'nearby'>('radar');
  const [highContrastMode, setHighContrastMode] = useState<boolean>(false);

  // Calculate distances to all loot spots
  const nearbyLoot = lootSpots
    .map(l => ({ ...l, distance: Math.round(getDistanceMeters(player.lat, player.lon, l.lat, l.lon)) }))
    .filter(l => !l.looted && (l.distance || 0) <= 75)
    .sort((a, b) => (a.distance || 0) - (b.distance || 0));

  // Calculate distances to all chimères
  const nearbyChimeres = chimeres
    .filter(c => !c.captured && c.lat && c.lon)
    .map(c => ({
      ...c,
      distance: Math.round(getDistanceMeters(player.lat, player.lon, c.lat!, c.lon!))
    }))
    .filter(c => (c.distance || 0) <= 75)
    .sort((a, b) => (a.distance || 0) - (b.distance || 0));

  // Closest item in 25m reach for 1-Tap fast interaction (Bounding Bubble GDD)
  const immediateLoot = nearbyLoot.find(l => (l.distance || 0) <= 25);
  const immediateChimere = nearbyChimeres.find(c => (c.distance || 0) <= 25);

  // Trigger distinctive haptic feedbacks according to GDD spec
  useEffect(() => {
    if ('vibrate' in navigator) {
      if (immediateChimere) {
        // Double impulsion saccadée : Chimère en approche
        navigator.vibrate([100, 50, 100]);
      } else if (immediateLoot) {
        // Vibration courte 50ms : Ressource noble à portée 25m
        navigator.vibrate(50);
      } else if (player.currentWeight >= player.maxWeight) {
        // Longue vibration continue : Sac plein
        navigator.vibrate(500);
      }
    }
  }, [immediateLoot?.id, immediateChimere?.id, player.currentWeight, player.maxWeight]);

  // Current H3 Tile hazard
  const currentTile = h3Tiles.find(t => getDistanceMeters(player.lat, player.lon, t.center[0], t.center[1]) < 70);
  const currentRad = currentTile ? currentTile.radiationLevel : 12;

  const handleLootTap = (loot: LootSpot) => {
    if ('vibrate' in navigator) navigator.vibrate(50);
    soundFx.playLootPickup();
    onLootPickup(loot);
  };

  const handleChimereTap = (chimere: Chimere) => {
    if ('vibrate' in navigator) navigator.vibrate([100, 50, 100]);
    soundFx.playRadarPing(900);
    onEngageChimere(chimere);
  };

  return (
    <div className={`flex flex-col h-full select-none overflow-y-auto ${
      highContrastMode ? 'bg-black text-amber-300 font-bold' : 'bg-slate-950 text-slate-100'
    } p-3 sm:p-4`}>
      {/* Top Banner with High-Contrast Sun Mode Toggle */}
      <div className={`border rounded-2xl p-3 mb-3 flex items-center justify-between shadow-xl ${
        highContrastMode ? 'bg-zinc-900 border-amber-400' : 'bg-slate-900/90 border-slate-800'
      }`}>
        <div className="flex items-center gap-2.5">
          <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500 flex items-center justify-center text-xl">
            📡
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <h2 className="text-sm font-bold font-tech">Mode Nomade (IRL)</h2>
              <span className={`text-[10px] px-1.5 py-0.2 rounded font-mono ${
                highContrastMode ? 'bg-amber-400 text-black' : 'bg-amber-950 text-amber-400 border border-amber-800'
              }`}>
                Rayon 25m · Cécité Minimale
              </span>
            </div>
            <div className="text-[11px] font-mono text-slate-400">
              Filtre Kalman EKF · {isRealGpsActive ? '🟢 GPS Réel Connecté' : '🟡 Simulation Flèche'}
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {/* Outdoor High Contrast Button */}
          <button
            onClick={() => setHighContrastMode(!highContrastMode)}
            className={`p-2 rounded-xl text-xs font-mono font-bold border transition-all flex items-center gap-1 ${
              highContrastMode ? 'bg-amber-400 text-black border-white' : 'bg-slate-800 text-slate-300 border-slate-700'
            }`}
            title="Mode Plein Soleil (Contraste Élevé)"
          >
            <Sun className="w-4 h-4" />
            <span className="hidden sm:inline">Plein Soleil</span>
          </button>

          <button
            onClick={onToggleGps}
            className={`px-3 py-2 rounded-xl text-xs font-tech font-bold border transition-all ${
              isRealGpsActive
                ? 'bg-emerald-600 border-emerald-400 text-white'
                : 'bg-slate-800 border-slate-700 text-slate-300'
            }`}
          >
            {isRealGpsActive ? 'GPS Actif' : 'Activer GPS'}
          </button>
        </div>
      </div>

      {/* Main Sonar Radar Scope Display */}
      <div className="flex-1 min-h-[300px] relative flex flex-col items-center justify-center">
        {/* Radar Ring Canvas */}
        <div className="relative w-64 h-64 sm:w-72 sm:h-72 rounded-full border-2 border-cyan-500/30 bg-slate-950/80 flex items-center justify-center overflow-hidden shadow-[0_0_50px_rgba(6,182,212,0.15)]">
          {/* Concentric distance rings: 10m, 25m (Bounding Bubble), 50m */}
          <div className="absolute w-20 h-20 rounded-full border border-cyan-500/20 pointer-events-none" />
          <div className="absolute w-44 h-44 rounded-full border-2 border-dashed border-amber-500/50 pointer-events-none animate-pulse">
            <span className="absolute -top-3 left-1/2 -translate-x-1/2 text-[9px] font-mono text-amber-400 bg-slate-950 px-1 rounded">25m</span>
          </div>
          <div className="absolute w-60 h-60 rounded-full border border-cyan-500/20 pointer-events-none" />

          {/* Sweeping Sonar Beam */}
          <div className="absolute inset-0 bg-gradient-to-tr from-transparent via-cyan-500/10 to-transparent rounded-full animate-spin [animation-duration:4s] pointer-events-none" />

          {/* Player Center Blip */}
          <div className="w-4 h-4 rounded-full bg-cyan-400 border-2 border-white shadow-[0_0_12px_#38bdf8] z-10 flex items-center justify-center">
            <div className="w-1.5 h-1.5 bg-slate-950 rounded-full" />
          </div>

          {/* Render Nearby Loot Blips on Radar */}
          {nearbyLoot.map(loot => {
            const dist = loot.distance || 30;
            const angle = ((loot.lat - player.lat) * 1000) % 360;
            const r = Math.min(120, (dist / 75) * 120);
            const x = Math.cos(angle) * r;
            const y = Math.sin(angle) * r;
            const isInsideBubble = dist <= 25;

            const lootIcon = loot.category === 'medical' ? '💉' : loot.category === 'electronics' ? '⚡' : loot.category === 'fuel' ? '⛽' : loot.category === 'weapons' ? '🔫' : '🧱';

            return (
              <button
                key={loot.id}
                onClick={() => handleLootTap(loot)}
                style={{ transform: `translate(${x}px, ${y}px)` }}
                className={`absolute w-7 h-7 rounded-full flex items-center justify-center text-xs shadow-lg transition-all ${
                  isInsideBubble
                    ? 'bg-amber-500 text-slate-950 scale-115 animate-bounce ring-2 ring-white'
                    : 'bg-slate-800 text-slate-300 border border-slate-700'
                }`}
                title={`${loot.name} (${dist}m)`}
              >
                <span>{lootIcon}</span>
              </button>
            );
          })}

          {/* Render Nearby Chimere Blips on Radar */}
          {nearbyChimeres.map(chim => {
            const dist = chim.distance || 35;
            const angle = ((chim.lat! - player.lat) * 1500) % 360;
            const r = Math.min(120, (dist / 75) * 120);
            const x = Math.cos(angle) * r;
            const y = Math.sin(angle) * r;
            const isInsideBubble = dist <= 25;

            return (
              <button
                key={chim.id}
                onClick={() => handleChimereTap(chim)}
                style={{ transform: `translate(${x}px, ${y}px)` }}
                className={`absolute w-8 h-8 rounded-full flex items-center justify-center text-sm shadow-lg transition-all ${
                  isInsideBubble
                    ? 'bg-red-500 text-white scale-120 animate-pulse ring-2 ring-red-300'
                    : 'bg-red-950 text-red-300 border border-red-800'
                }`}
                title={`${chim.name} (${dist}m)`}
              >
                <span>{chim.avatarIcon}</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* ERGONOMIE MOBILE : ZONE ACTIVE DU TIERS INFÉRIEUR (Accessible au Pouce à une main) */}
      <div className={`mt-2 p-3 rounded-2xl border ${
        highContrastMode ? 'bg-zinc-900 border-amber-400' : 'bg-slate-900 border-slate-800'
      } shadow-2xl space-y-2`}>
        {/* Immediate 1-Tap Interaction Alert */}
        {immediateLoot && (
          <button
            onClick={() => handleLootTap(immediateLoot)}
            className="w-full py-3 bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-tech font-bold text-sm rounded-xl shadow-lg flex items-center justify-center gap-2 active:scale-95 animate-pulse"
          >
            <Package className="w-5 h-5" />
            <span>RÉCOLTER : {immediateLoot.name} ({immediateLoot.distance}m) [1-TAP]</span>
          </button>
        )}

        {immediateChimere && (
          <button
            onClick={() => handleChimereTap(immediateChimere)}
            className="w-full py-3 bg-gradient-to-r from-red-600 to-amber-600 hover:from-red-500 hover:to-amber-500 text-white font-tech font-bold text-sm rounded-xl shadow-lg flex items-center justify-center gap-2 active:scale-95 animate-pulse"
          >
            <Radio className="w-5 h-5" />
            <span>INTERCEPTER CHIMÈRE : {immediateChimere.name} ({immediateChimere.distance}m)</span>
          </button>
        )}

        {/* Directional Pad for Manual Virtual Steps (Testing without physical movement) */}
        {!isRealGpsActive && (
          <div className="flex items-center justify-between pt-1">
            <span className="text-[11px] font-mono text-slate-400">Simulation pas virtuels (IRL) :</span>
            <div className="flex items-center gap-1.5">
              <button
                onClick={() => onVirtualStep(0, 0.00015)}
                className="w-10 h-10 rounded-xl bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center active:scale-90"
              >
                <ArrowUp className="w-5 h-5" />
              </button>
              <button
                onClick={() => onVirtualStep(-0.00015, 0)}
                className="w-10 h-10 rounded-xl bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center active:scale-90"
              >
                <ArrowLeft className="w-5 h-5" />
              </button>
              <button
                onClick={() => onVirtualStep(0, -0.00015)}
                className="w-10 h-10 rounded-xl bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center active:scale-90"
              >
                <ArrowDown className="w-5 h-5" />
              </button>
              <button
                onClick={() => onVirtualStep(0.00015, 0)}
                className="w-10 h-10 rounded-xl bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center active:scale-90"
              >
                <ArrowRight className="w-5 h-5" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
