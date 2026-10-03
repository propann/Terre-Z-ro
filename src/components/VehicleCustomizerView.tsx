import React, { useState } from 'react';
import { VehicleState, VehicleChassisType, VehicleDriveMode, VehicleModule, PlayerState, Item } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Truck,
  Shield,
  Zap,
  Flame,
  Hammer,
  RotateCcw,
  Sparkles,
  Award,
  Plus,
  Check,
  Radio,
  Sliders,
  Compass,
  ArrowRight
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface VehicleCustomizerViewProps {
  player: PlayerState;
  onUpdateVehicle: (vehicle: VehicleState) => void;
}

export const VehicleCustomizerView: React.FC<VehicleCustomizerViewProps> = ({ player, onUpdateVehicle }) => {
  const [currentVehicle, setCurrentVehicle] = useState<VehicleState>(
    player.activeVehicle || {
      id: 'veh-01',
      name: 'Buggy Nomade Voxel-Scrap',
      chassis: 'buggy',
      fuel: 85,
      maxFuel: 100,
      health: 220,
      maxHealth: 250,
      cargoCapacityKg: 150,
      cargo: [],
      driveMode: 'autopilot_convoy',
      modules: {
        prow: { slot: 'prow', name: 'Lame de Déblaiement en Acier', icon: '🪓', effect: 'Broie les barricades voxel sur la route', durability: 100 },
        roof: { slot: 'roof', name: 'Siège de Chimère Sentinelle', icon: '🔫', effect: 'Tir automatique sur les meutes hostiles', durability: 100 },
        flanks: { slot: 'flanks', name: 'Blindage Composite Titane', icon: '🛡️', effect: '+80 PV structurels', durability: 100 },
        flatbed: { slot: 'flatbed', name: 'Bennes de Fret Voxel (x10)', icon: '📦', effect: 'Multiplie la capacité de transport (+150 kg)', durability: 100 }
      }
    }
  );

  const [selectedSlot, setSelectedSlot] = useState<'prow' | 'roof' | 'flanks' | 'flatbed'>('prow');

  // Available Chassis
  const chassisList = [
    { type: 'moto' as VehicleChassisType, name: 'Moto-Scrap', icon: '🏍️', cargo: '40 kg', speed: 'Rapide (80 km/h)', fuelConso: 'Faible' },
    { type: 'buggy' as VehicleChassisType, name: 'Buggy Léger', icon: '🏎️', cargo: '150 kg', speed: 'Équilibré (65 km/h)', fuelConso: 'Moyenne' },
    { type: 'truck_6x6' as VehicleChassisType, name: 'Camion Blindé 6x6', icon: '🚛', cargo: '800 kg', speed: 'Lourd (45 km/h)', fuelConso: 'Élevée' }
  ];

  // Available Modules by Slot
  const availableModulesBySlot: Record<string, VehicleModule[]> = {
    prow: [
      { slot: 'prow', name: 'Lame de Déblaiement en Acier', icon: '🪓', effect: 'Broie les barricades voxel sans stopper le véhicule', durability: 100 },
      { slot: 'prow', name: 'Herse Électrifiée IEM', icon: '⚡', effect: 'Électrocute et paralyse les créatures sur la route', durability: 100 }
    ],
    roof: [
      { slot: 'roof', name: 'Nid de Mitrailleuse 12.7mm', icon: '🔫', effect: 'Tir automatique de barrage sur les meutes', durability: 100 },
      { slot: 'roof', name: 'Siège de Chimère Asservie', icon: '🤖', effect: 'Permet à une Chimère de foudre de recharger le véhicule', durability: 100 }
    ],
    flanks: [
      { slot: 'flanks', name: 'Blindage Composite Titane', icon: '🛡️', effect: '+80 PV structurels au véhicule', durability: 100 },
      { slot: 'flanks', name: 'Réservoirs de Carburant Auxiliaires', icon: '⛽', effect: '+50% autonomie en expédition', durability: 100 }
    ],
    flatbed: [
      { slot: 'flatbed', name: 'Bennes de Fret Voxel (x10)', icon: '📦', effect: 'Multiplie par dix la capacité d’emport de minerai', durability: 100 },
      { slot: 'flatbed', name: 'Atelier Mobile de Terrain', icon: '🔧', effect: 'Autorise les réparations d’urgence en pleine marche IRL', durability: 100 }
    ]
  };

  const handleSelectChassis = (type: VehicleChassisType, name: string) => {
    soundFx.playRadarPing(800);
    const updated = { ...currentVehicle, chassis: type, name };
    setCurrentVehicle(updated);
    onUpdateVehicle(updated);
  };

  const handleEquipModule = (mod: VehicleModule) => {
    soundFx.playHitSound();
    const updated = {
      ...currentVehicle,
      modules: {
        ...currentVehicle.modules,
        [mod.slot]: mod
      }
    };
    setCurrentVehicle(updated);
    onUpdateVehicle(updated);
    try {
      confetti({ particleCount: 30, spread: 50, origin: { y: 0.6 } });
    } catch {}
  };

  const handleToggleDriveMode = () => {
    const nextMode: VehicleDriveMode = currentVehicle.driveMode === 'autopilot_convoy' ? 'manual_joystick' : 'autopilot_convoy';
    soundFx.playRadarPing(900);
    const updated = { ...currentVehicle, driveMode: nextMode };
    setCurrentVehicle(updated);
    onUpdateVehicle(updated);
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Banner */}
      <div className="bg-gradient-to-r from-orange-950/50 via-slate-900 to-slate-950 border border-orange-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-orange-500/20 border border-orange-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(249,115,22,0.3)]">
            🏎️
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Garage & Customisation Voxel : {currentVehicle.name}</h2>
              <span className="text-[10px] bg-orange-950 text-orange-400 border border-orange-800 px-2 py-0.5 rounded font-mono">
                Conduite Hybride OSM · 3 Châssis Modulaires
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Collisions hybrides sur le ruban routier vectoriel et convoi autonome suivant vos pas IRL.
            </p>
          </div>
        </div>

        {/* Autopilot Toggle */}
        <button
          onClick={handleToggleDriveMode}
          className={`px-4 py-2.5 rounded-xl text-xs font-tech font-bold flex items-center gap-2 border transition-all shadow-lg active:scale-95 ${
            currentVehicle.driveMode === 'autopilot_convoy'
              ? 'bg-emerald-950/80 border-emerald-500 text-emerald-300'
              : 'bg-cyan-950/80 border-cyan-500 text-cyan-300'
          }`}
        >
          <Compass className="w-4 h-4 animate-spin" />
          <span>
            {currentVehicle.driveMode === 'autopilot_convoy'
              ? 'PILOTE AUTOMATIQUE (Convoi IRL)'
              : 'JOYSTICK MANUEL (À l’écran)'}
          </span>
        </button>
      </div>

      {/* Grid: 3 Chassis + Customizer */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* LEFT COL: Chassis Selection */}
        <div className="space-y-3">
          <div className="text-xs font-tech font-bold text-slate-400 uppercase tracking-wider">
            1. Choix du Châssis Voxel
          </div>
          {chassisList.map(c => {
            const isSelected = currentVehicle.chassis === c.type;
            return (
              <button
                key={c.type}
                onClick={() => handleSelectChassis(c.type, c.name)}
                className={`w-full p-4 rounded-2xl border text-left transition-all flex items-center justify-between ${
                  isSelected
                    ? 'bg-orange-500/20 border-orange-500 shadow-[0_0_20px_rgba(249,115,22,0.2)] scale-102'
                    : 'bg-slate-900 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-center gap-3">
                  <span className="text-3xl">{c.icon}</span>
                  <div>
                    <h3 className="font-tech font-bold text-sm text-white">{c.name}</h3>
                    <span className="text-[10px] font-mono text-slate-400">Emport : {c.cargo} · {c.speed}</span>
                  </div>
                </div>
                {isSelected && <Check className="w-5 h-5 text-orange-400" />}
              </button>
            );
          })}

          {/* Vehicle Stats */}
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 space-y-2 font-mono text-xs">
            <div className="flex justify-between">
              <span className="text-slate-400">Réservoir Carburant :</span>
              <span className="text-amber-400 font-bold">{currentVehicle.fuel} / {currentVehicle.maxFuel} %</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Intégrité Châssis :</span>
              <span className="text-cyan-400 font-bold">{currentVehicle.health} / {currentVehicle.maxHealth} PV</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Capacité de Fret :</span>
              <span className="text-emerald-400 font-bold">{currentVehicle.cargoCapacityKg} kg</span>
            </div>
          </div>
        </div>

        {/* CENTER & RIGHT: 4 Modular Anchor Points */}
        <div className="lg:col-span-2 bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-3">
              <div className="text-xs font-tech font-bold text-white uppercase tracking-wider">
                2. Points d’Ancrage Modulaires (Voxel Greffés)
              </div>
              <span className="text-[10px] font-mono text-orange-400">Sélectionnez un point d’ancrage</span>
            </div>

            {/* Slots Selector */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mb-4">
              {[
                { slot: 'prow', label: 'Proue / Pare-chocs', icon: '🪓' },
                { slot: 'roof', label: 'Toit / Tourelle', icon: '🔫' },
                { slot: 'flanks', label: 'Flancs / Blindage', icon: '🛡️' },
                { slot: 'flatbed', label: 'Plateau Arrière', icon: '📦' }
              ].map(s => {
                const isSelected = selectedSlot === s.slot;
                const installed = (currentVehicle.modules as any)[s.slot] as VehicleModule | undefined;

                return (
                  <button
                    key={s.slot}
                    onClick={() => {
                      setSelectedSlot(s.slot as any);
                      soundFx.playRadarPing(700);
                    }}
                    className={`p-3 rounded-xl border text-left transition-all flex flex-col justify-between ${
                      isSelected
                        ? 'bg-amber-500/20 border-amber-500 shadow-md scale-102'
                        : 'bg-slate-950 border-slate-800 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-lg">{s.icon}</span>
                      {installed && <span className="text-[9px] bg-emerald-950 text-emerald-400 px-1 rounded">ÉQUIPÉ</span>}
                    </div>
                    <div className="font-tech text-xs font-bold text-white truncate">{s.label}</div>
                    <span className="text-[9px] font-mono text-slate-400 truncate">{installed?.name || 'Emplacement vide'}</span>
                  </button>
                );
              })}
            </div>

            {/* Available Modules for Selected Slot */}
            <div className="space-y-2">
              <div className="text-[11px] font-mono text-slate-400 uppercase">
                Modules disponibles pour : <strong>{selectedSlot}</strong>
              </div>

              {availableModulesBySlot[selectedSlot]?.map(mod => {
                const isEquipped = (currentVehicle.modules as any)[selectedSlot]?.name === mod.name;

                return (
                  <div
                    key={mod.name}
                    className={`p-3 rounded-xl border flex items-center justify-between transition-all ${
                      isEquipped ? 'bg-slate-950 border-emerald-500/60' : 'bg-slate-950/70 border-slate-800'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <span className="text-2xl">{mod.icon}</span>
                      <div>
                        <div className="font-tech text-xs font-bold text-white">{mod.name}</div>
                        <p className="text-[11px] text-slate-400 font-mono">{mod.effect}</p>
                      </div>
                    </div>

                    <button
                      onClick={() => handleEquipModule(mod)}
                      disabled={isEquipped}
                      className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold transition-all ${
                        isEquipped
                          ? 'bg-emerald-950 text-emerald-400 border border-emerald-800'
                          : 'bg-amber-600 hover:bg-amber-500 text-slate-950'
                      }`}
                    >
                      {isEquipped ? 'INSTALLÉ' : 'INSTALLER'}
                    </button>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Bottom Help */}
          <div className="mt-4 p-3 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-slate-400 flex items-center justify-between">
            <span>🛡️ Les roues roulent sur le ruban OSM sans à-coups, la proue broie les barricades voxel.</span>
          </div>
        </div>
      </div>
    </div>
  );
};
