import React from 'react';
import {
  Compass,
  Box,
  Home,
  Sparkles,
  Code2,
  Package,
  Volume2,
  VolumeX,
  BookOpen,
  MapPin,
  ListOrdered,
  Truck
} from 'lucide-react';
import { soundFx } from '../services/soundFx';

export type AppViewMode = 'nomad' | 'map3d' | 'microvoxel' | 'bunker' | 'chimeres' | 'studio' | 'roadmap';

interface HeaderProps {
  currentMode: AppViewMode;
  onSelectMode: (mode: AppViewMode) => void;
  onOpenInventory: () => void;
  onOpenHelp: () => void;
  inventoryCount: number;
  currentWeight: number;
  maxWeight: number;
  isAudioOn: boolean;
  onToggleAudio: () => void;
  level: number;
  xp: number;
}

export const Header: React.FC<HeaderProps> = ({
  currentMode,
  onSelectMode,
  onOpenInventory,
  onOpenHelp,
  inventoryCount,
  currentWeight,
  maxWeight,
  isAudioOn,
  onToggleAudio,
  level,
  xp
}) => {
  return (
    <header className="bg-slate-900/95 backdrop-blur-md border-b border-slate-800 px-3 py-2 flex items-center justify-between z-30 select-none shadow-lg">
      {/* Brand Title */}
      <div className="flex items-center gap-3">
        <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-amber-500 to-red-600 flex items-center justify-center text-lg shadow-[0_0_12px_rgba(245,158,11,0.3)]">
          ☢️
        </div>
        <div>
          <div className="flex items-center gap-1.5">
            <h1 className="font-tech text-base font-bold text-white tracking-wider flex items-center gap-1.5">
              <span>CHIMÈRES</span>
              <span className="text-xs bg-red-950 text-red-400 border border-red-800 px-1.5 py-0.2 rounded font-mono font-normal">
                POST-APO GPS
              </span>
            </h1>
          </div>
          <div className="text-[10px] text-slate-400 font-mono flex items-center gap-2">
            <span>Survie Géolocalisée</span>
            <span className="text-amber-400">Niv. {level} ({xp} XP)</span>
          </div>
        </div>
      </div>

      {/* Main Navigation Tabs */}
      <nav className="flex items-center gap-1 bg-slate-950/80 p-1 rounded-2xl border border-slate-800/90 overflow-x-auto">
        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('nomad');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'nomad'
              ? 'bg-amber-500 text-slate-950 shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <Compass className="w-3.5 h-3.5" />
          <span>Nomade (Dehors)</span>
        </button>

        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('map3d');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'map3d'
              ? 'bg-cyan-500 text-slate-950 shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <Box className="w-3.5 h-3.5" />
          <span>Carte 3D Voxel</span>
        </button>

        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('microvoxel');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'microvoxel'
              ? 'bg-orange-500 text-slate-950 shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <Sparkles className="w-3.5 h-3.5 text-orange-400" />
          <span>Micro-Voxel (Godot 4)</span>
        </button>

        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('bunker');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'bunker'
              ? 'bg-amber-600 text-white shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <Home className="w-3.5 h-3.5" />
          <span>Bunker (Maison)</span>
        </button>

        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('studio');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'studio'
              ? 'bg-purple-600 text-white shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <Code2 className="w-3.5 h-3.5" />
          <span>Studio OSM / Scripts</span>
        </button>

        <button
          onClick={() => {
            soundFx.playRadarPing();
            onSelectMode('roadmap');
          }}
          className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            currentMode === 'roadmap'
              ? 'bg-emerald-600 text-white shadow-md scale-102'
              : 'text-slate-400 hover:text-white hover:bg-slate-900'
          }`}
        >
          <ListOrdered className="w-3.5 h-3.5" />
          <span>Feuille de Route (Phases 1-6)</span>
        </button>
      </nav>

      {/* Action Buttons */}
      <div className="flex items-center gap-2">
        {/* Inventory Trigger */}
        <button
          onClick={onOpenInventory}
          className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 rounded-xl text-xs font-tech font-bold flex items-center gap-1.5 shadow transition-all relative"
        >
          <Package className="w-4 h-4 text-cyan-400" />
          <span className="hidden sm:inline">Sac :</span>
          <span className={`font-mono ${currentWeight >= maxWeight ? 'text-red-400 font-bold' : 'text-cyan-300'}`}>
            {currentWeight.toFixed(1)}/{maxWeight}kg
          </span>
          {inventoryCount > 0 && (
            <span className="absolute -top-1 -right-1 bg-cyan-500 text-slate-950 text-[10px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
              {inventoryCount}
            </span>
          )}
        </button>

        {/* Audio Toggle */}
        <button
          onClick={onToggleAudio}
          className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition-colors"
          title={isAudioOn ? 'Couper les effets sonores' : 'Activer les effets sonores'}
        >
          {isAudioOn ? <Volume2 className="w-4 h-4 text-emerald-400" /> : <VolumeX className="w-4 h-4 text-slate-500" />}
        </button>

        {/* Quick Help */}
        <button
          onClick={onOpenHelp}
          className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition-colors"
          title="Architecture & Guide"
        >
          <BookOpen className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
};
