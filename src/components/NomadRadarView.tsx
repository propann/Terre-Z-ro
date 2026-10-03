import React, { useState } from 'react';
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

  // Calculate distances to all loot spots
  const nearbyLoot = lootSpots
    .map(l => ({ ...l, distance: Math.round(getDistanceMeters(player.lat, player.lon, l.lat, l.lon)) }))
    .filter(l => !l.looted && (l.distance || 0) <= 65)
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

  // Closest item in 20m reach for 1-Tap fast interaction
  const immediateLoot = nearbyLoot.find(l => (l.distance || 0) <= 20);
  const immediateChimere = nearbyChimeres.find(c => (c.distance || 0) <= 20);

  // Current H3 Tile hazard
  const currentTile = h3Tiles.find(t => getDistanceMeters(player.lat, player.lon, t.center[0], t.center[1]) < 70);
  const currentRad = currentTile ? currentTile.radiationLevel : 12;

  const handleLootTap = (loot: LootSpot) => {
    if ('vibrate' in navigator) navigator.vibrate([40, 30, 40]);
    soundFx.playLootPickup();
    onLootPickup(loot);
  };

  const handleChimereTap = (chim: Chimere) => {
    if ('vibrate' in navigator) navigator.vibrate(80);
    soundFx.playChimereEncounter(chim.type === 'mechanical');
    onEngageChimere(chim);
  };

  return (
    <div className="flex flex-col h-full bg-slate-950/90 backdrop-blur-md text-slate-100 p-4 select-none overflow-y-auto">
      {/* Top Vital Bar */}
      <div className="grid grid-cols-3 gap-2 mb-3">
        {/* Health */}
        <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-2.5 flex items-center gap-2">
          <Activity className="w-4 h-4 text-emerald-400" />
          <div className="flex-1">
            <div className="flex justify-between text-[11px] text-slate-400 mb-1">
              <span>Santé</span>
              <span className="text-emerald-400 font-mono font-bold">{player.health}/{player.maxHealth}</span>
            </div>
            <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
              <div
                className="bg-emerald-500 h-full rounded-full transition-all"
                style={{ width: `${(player.health / player.maxHealth) * 100}%` }}
              />
            </div>
          </div>
        </div>

        {/* Radiation Geiger */}
        <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-2.5 flex items-center gap-2">
          <Radio className="w-4 h-4 text-amber-400 animate-pulse" />
          <div className="flex-1">
            <div className="flex justify-between text-[11px] text-slate-400 mb-1">
              <span>Geiger</span>
              <span className="text-amber-400 font-mono font-bold">{currentRad} RAD/h</span>
            </div>
            <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
              <div
                className={`h-full rounded-full transition-all ${
                  currentRad > 50 ? 'bg-red-500' : currentRad > 25 ? 'bg-amber-500' : 'bg-cyan-500'
                }`}
                style={{ width: `${Math.min(100, currentRad * 1.5)}%` }}
              />
            </div>
          </div>
        </div>

        {/* Backpack Weight */}
        <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-2.5 flex items-center gap-2">
          <Package className="w-4 h-4 text-cyan-400" />
          <div className="flex-1">
            <div className="flex justify-between text-[11px] text-slate-400 mb-1">
              <span>Sac à dos</span>
              <span className={`font-mono font-bold ${player.currentWeight >= player.maxWeight ? 'text-red-400' : 'text-cyan-400'}`}>
                {player.currentWeight.toFixed(1)}/{player.maxWeight}kg
              </span>
            </div>
            <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
              <div
                className={`h-full rounded-full transition-all ${
                  player.currentWeight >= player.maxWeight ? 'bg-red-500' : 'bg-cyan-500'
                }`}
                style={{ width: `${Math.min(100, (player.currentWeight / player.maxWeight) * 100)}%` }}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Main Radar Screen */}
      <div className="relative flex flex-col items-center justify-center bg-slate-900/60 border border-cyan-500/30 rounded-2xl p-4 my-2 shadow-2xl overflow-hidden">
        {/* Radar Circular Grid */}
        <div className="relative w-64 h-64 rounded-full border border-cyan-500/40 flex items-center justify-center shadow-inner">
          {/* Concentric distance rings: 10m, 20m, 50m */}
          <div className="absolute w-48 h-48 rounded-full border border-cyan-500/25 border-dashed" />
          <div className="absolute w-32 h-32 rounded-full border border-emerald-500/30" />
          <div className="absolute w-16 h-16 rounded-full border border-cyan-500/30" />

          {/* Cross lines */}
          <div className="absolute w-full h-[1px] bg-cyan-500/20" />
          <div className="absolute h-full w-[1px] bg-cyan-500/20" />

          {/* Rotating Radar Sweep Cone */}
          <div className="absolute inset-0 rounded-full radar-sweep pointer-events-none opacity-40">
            <div className="w-1/2 h-1/2 bg-gradient-to-br from-cyan-400/30 to-transparent rounded-tl-full origin-bottom-right" />
          </div>

          {/* Center Player Dot */}
          <div className="relative z-10 w-4 h-4 bg-cyan-400 rounded-full shadow-[0_0_12px_#22d3ee] flex items-center justify-center">
            <div className="w-1.5 h-1.5 bg-white rounded-full" />
          </div>

          {/* Nearby Loot Blips on Radar */}
          {nearbyLoot.map(loot => {
            const dist = loot.distance || 30;
            const angle = Math.atan2(loot.lon - player.lon, loot.lat - player.lat);
            const rPx = Math.min(115, (dist / 60) * 115);
            const x = Math.sin(angle) * rPx;
            const y = -Math.cos(angle) * rPx;

            return (
              <button
                key={loot.id}
                onClick={() => dist <= 20 && handleLootTap(loot)}
                className={`absolute z-20 transform -translate-x-1/2 -translate-y-1/2 w-5 h-5 rounded-full flex items-center justify-center text-[10px] transition-transform hover:scale-125 ${
                  dist <= 20
                    ? 'bg-amber-500 text-slate-950 font-bold animate-bounce shadow-[0_0_10px_#f59e0b]'
                    : 'bg-amber-600/80 text-amber-200'
                }`}
                style={{ left: `calc(50% + ${x}px)`, top: `calc(50% + ${y}px)` }}
                title={`${loot.name} (${dist}m)`}
              >
                📦
              </button>
            );
          })}

          {/* Nearby Chimère Blips on Radar */}
          {nearbyChimeres.map(chim => {
            const dist = chim.distance || 30;
            const angle = Math.atan2(chim.lon! - player.lon, chim.lat! - player.lat);
            const rPx = Math.min(115, (dist / 60) * 115);
            const x = Math.sin(angle) * rPx;
            const y = -Math.cos(angle) * rPx;

            return (
              <button
                key={chim.id}
                onClick={() => dist <= 20 && handleChimereTap(chim)}
                className={`absolute z-20 transform -translate-x-1/2 -translate-y-1/2 w-6 h-6 rounded-full flex items-center justify-center text-xs transition-transform hover:scale-125 ${
                  dist <= 20
                    ? chim.type === 'mechanical'
                      ? 'bg-red-500 text-white font-bold animate-pulse shadow-[0_0_12px_#ef4444]'
                      : 'bg-purple-500 text-white font-bold animate-pulse shadow-[0_0_12px_#a855f7]'
                    : chim.type === 'mechanical'
                    ? 'bg-red-900/80 text-red-200'
                    : 'bg-purple-900/80 text-purple-200'
                }`}
                style={{ left: `calc(50% + ${x}px)`, top: `calc(50% + ${y}px)` }}
                title={`${chim.name} (${dist}m)`}
              >
                {chim.avatarIcon}
              </button>
            );
          })}
        </div>

        {/* Radar HUD Stats */}
        <div className="flex items-center justify-between w-full mt-3 text-xs font-mono text-slate-400 px-2">
          <div className="flex items-center gap-1.5">
            <Footprints className="w-3.5 h-3.5 text-cyan-400" />
            <span>{player.distanceWalkedMeters} m parcourus</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Compass className="w-3.5 h-3.5 text-orange-400" />
            <span>Cap {Math.round(player.heading)}°</span>
          </div>
        </div>
      </div>

      {/* 1-TAP FAST NOMAD ACTION BANNER (When within 20m of a POI) */}
      {immediateLoot && (
        <div className="bg-gradient-to-r from-amber-950/80 to-slate-900 border-2 border-amber-500 rounded-2xl p-3 mb-3 flex items-center justify-between shadow-[0_0_20px_rgba(245,158,11,0.25)] animate-pulse">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500 flex items-center justify-center text-xl">
              📦
            </div>
            <div>
              <div className="text-xs font-mono text-amber-400 uppercase font-semibold">
                Loot à portée ({immediateLoot.distance}m)
              </div>
              <div className="text-sm font-bold text-white">{immediateLoot.name}</div>
              <div className="text-[11px] text-slate-300">
                {immediateLoot.items.map(i => `${i.name} x${i.quantity}`).join(', ')}
              </div>
            </div>
          </div>
          <button
            onClick={() => handleLootTap(immediateLoot)}
            className="px-4 py-2.5 bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold rounded-xl text-sm shadow-lg transition-transform active:scale-95 flex items-center gap-1.5"
          >
            <span>Fouiller</span>
            <Zap className="w-4 h-4 fill-current" />
          </button>
        </div>
      )}

      {immediateChimere && !immediateLoot && (
        <div className={`bg-gradient-to-r ${
          immediateChimere.type === 'mechanical' ? 'from-red-950/80 border-red-500' : 'from-purple-950/80 border-purple-500'
        } to-slate-900 border-2 rounded-2xl p-3 mb-3 flex items-center justify-between shadow-lg animate-pulse`}>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-slate-800 border border-slate-700 flex items-center justify-center text-2xl">
              {immediateChimere.avatarIcon}
            </div>
            <div>
              <div className="text-xs font-mono text-red-400 uppercase font-semibold">
                Mutant Repéré ({immediateChimere.distance}m)
              </div>
              <div className="text-sm font-bold text-white">{immediateChimere.name}</div>
              <div className="text-[11px] text-slate-300">Niv. {immediateChimere.level} · {immediateChimere.type === 'mechanical' ? 'Mécanique' : 'Bio-mutée'}</div>
            </div>
          </div>
          <button
            onClick={() => handleChimereTap(immediateChimere)}
            className={`px-4 py-2.5 font-bold rounded-xl text-sm shadow-lg transition-transform active:scale-95 flex items-center gap-1.5 ${
              immediateChimere.type === 'mechanical'
                ? 'bg-red-500 hover:bg-red-400 text-white'
                : 'bg-purple-500 hover:bg-purple-400 text-white'
            }`}
          >
            <span>Engager</span>
            <ChevronRight className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Proximity Entities List */}
      <div className="flex-1 bg-slate-900/70 border border-slate-800 rounded-2xl p-3 overflow-hidden flex flex-col">
        <div className="flex items-center justify-between mb-2">
          <span className="text-xs font-mono font-semibold uppercase tracking-wider text-slate-400">
            Signaux à Proximité (&lt; 75m)
          </span>
          <span className="text-[11px] bg-slate-800 text-cyan-300 px-2 py-0.5 rounded font-mono">
            {nearbyLoot.length} Caisses · {nearbyChimeres.length} Chimères
          </span>
        </div>

        <div className="flex-1 overflow-y-auto space-y-2 pr-1">
          {nearbyLoot.length === 0 && nearbyChimeres.length === 0 && (
            <div className="text-center py-6 text-slate-500 text-xs font-mono">
              Aucun signal dans un rayon de 75m. Déplacez-vous vers une zone avec des bâtiments.
            </div>
          )}

          {/* Loot items */}
          {nearbyLoot.map(loot => (
            <div
              key={loot.id}
              className="bg-slate-950/70 border border-slate-800/80 hover:border-amber-500/50 rounded-xl p-2.5 flex items-center justify-between transition-all"
            >
              <div className="flex items-center gap-2.5">
                <span className="text-lg">📦</span>
                <div>
                  <div className="text-xs font-bold text-slate-200">{loot.name}</div>
                  <div className="text-[10px] text-slate-400 font-mono">
                    {loot.osmTag} · {loot.category}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono text-cyan-400 font-semibold">{loot.distance}m</span>
                <button
                  disabled={(loot.distance || 0) > 20}
                  onClick={() => handleLootTap(loot)}
                  className={`px-2.5 py-1 text-xs font-medium rounded-lg transition-all ${
                    (loot.distance || 0) <= 20
                      ? 'bg-amber-500 text-slate-950 font-bold hover:bg-amber-400'
                      : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  }`}
                >
                  {(loot.distance || 0) <= 20 ? 'Prendre' : 'Trop loin'}
                </button>
              </div>
            </div>
          ))}

          {/* Chimères */}
          {nearbyChimeres.map(chim => (
            <div
              key={chim.id}
              className="bg-slate-950/70 border border-slate-800/80 hover:border-red-500/50 rounded-xl p-2.5 flex items-center justify-between transition-all"
            >
              <div className="flex items-center gap-2.5">
                <span className="text-xl">{chim.avatarIcon}</span>
                <div>
                  <div className="text-xs font-bold text-slate-200">{chim.name}</div>
                  <div className="text-[10px] text-slate-400 font-mono">
                    {chim.type === 'mechanical' ? '⚙️ Mécanique' : '🧬 Bio-mutée'} · ATK {chim.attack}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono text-cyan-400 font-semibold">{chim.distance}m</span>
                <button
                  disabled={(chim.distance || 0) > 20}
                  onClick={() => handleChimereTap(chim)}
                  className={`px-2.5 py-1 text-xs font-medium rounded-lg transition-all ${
                    (chim.distance || 0) <= 20
                      ? 'bg-red-500 text-white font-bold hover:bg-red-400'
                      : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  }`}
                >
                  {(chim.distance || 0) <= 20 ? 'Traquer' : 'Trop loin'}
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Virtual D-Pad & GPS Toggle for Testing */}
      <div className="mt-3 bg-slate-900/80 border border-slate-800 rounded-xl p-2.5 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <button
            onClick={onToggleGps}
            className={`px-2.5 py-1 rounded-lg text-xs font-mono border transition-all ${
              isRealGpsActive
                ? 'bg-emerald-950 border-emerald-500 text-emerald-300'
                : 'bg-slate-800 border-slate-700 text-slate-400'
            }`}
          >
            GPS Smartphone : {isRealGpsActive ? 'ACTIF 🛰️' : 'SIMULATEUR 🕹️'}
          </button>
        </div>

        {/* Virtual Walk Buttons for Instant Desktop Testing */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => onVirtualStep(-0.00015, 0)}
            className="p-1.5 bg-slate-800 hover:bg-slate-700 rounded text-slate-300"
            title="Marcher Ouest"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
          </button>
          <div className="flex flex-col gap-1">
            <button
              onClick={() => onVirtualStep(0, 0.00015)}
              className="p-1.5 bg-slate-800 hover:bg-slate-700 rounded text-slate-300"
              title="Marcher Nord"
            >
              <ArrowUp className="w-3.5 h-3.5" />
            </button>
            <button
              onClick={() => onVirtualStep(0, -0.00015)}
              className="p-1.5 bg-slate-800 hover:bg-slate-700 rounded text-slate-300"
              title="Marcher Sud"
            >
              <ArrowDown className="w-3.5 h-3.5" />
            </button>
          </div>
          <button
            onClick={() => onVirtualStep(0.00015, 0)}
            className="p-1.5 bg-slate-800 hover:bg-slate-700 rounded text-slate-300"
            title="Marcher Est"
          >
            <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
};
