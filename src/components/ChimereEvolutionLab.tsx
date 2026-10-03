import React, { useState } from 'react';
import { Chimere, PlayerState } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Sparkles,
  Zap,
  Shield,
  Eye,
  Cpu,
  Flame,
  Award,
  PlusCircle,
  Check,
  RotateCcw,
  Layers,
  Radio
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface ChimereEvolutionLabProps {
  player: PlayerState;
  onUpdateChimere: (chimere: Chimere) => void;
}

export const ChimereEvolutionLab: React.FC<ChimereEvolutionLabProps> = ({ player, onUpdateChimere }) => {
  const [selectedChimereId, setSelectedChimereId] = useState<string>(player.chimeres[0]?.id || '');
  const [evolutionFeedback, setEvolutionFeedback] = useState<string | null>(null);

  const activeChimere = player.chimeres.find(c => c.id === selectedChimereId) || player.chimeres[0];

  const handleFeedNutrient = (nutrientName: string, statBonus: string) => {
    if (!activeChimere) return;
    soundFx.playHitSound();
    const updated: Chimere = {
      ...activeChimere,
      hp: activeChimere.hp + 15,
      maxHp: activeChimere.maxHp + 15,
      attack: activeChimere.attack + 3
    };
    onUpdateChimere(updated);
    setEvolutionFeedback(`🧪 Métabolisme activé avec ${nutrientName} (${statBonus})`);
    setTimeout(() => setEvolutionFeedback(null), 3000);
    try {
      confetti({ particleCount: 40, spread: 60, origin: { y: 0.6 } });
    } catch {}
  };

  const handleApplyMorphogenesis = (stageName: string) => {
    if (!activeChimere) return;
    soundFx.playCaptureSuccess();
    const updated: Chimere = {
      ...activeChimere,
      level: activeChimere.level + 1,
      maxHp: activeChimere.maxHp + 35,
      hp: activeChimere.maxHp + 35,
      attack: activeChimere.attack + 8,
      name: stageName === 'Titan Mutagène' ? `Titan ${activeChimere.name}` : `Symbiote ${activeChimere.name}`
    };
    onUpdateChimere(updated);
    setEvolutionFeedback(`🧬 Morphogenèse réussie : Stade ${stageName} atteint !`);
    setTimeout(() => setEvolutionFeedback(null), 3000);
    try {
      confetti({ particleCount: 70, spread: 70, origin: { y: 0.6 } });
    } catch {}
  };

  const handleTechnoideUpgrade = (upgradeName: string, statBonus: string) => {
    if (!activeChimere) return;
    soundFx.playHitSound();
    const updated: Chimere = {
      ...activeChimere,
      attack: activeChimere.attack + 5,
      defense: activeChimere.defense + 8
    };
    onUpdateChimere(updated);
    setEvolutionFeedback(`⚙️ Greffe mécanique terminée : ${upgradeName} (${statBonus})`);
    setTimeout(() => setEvolutionFeedback(null), 3000);
    try {
      confetti({ particleCount: 40, spread: 60, origin: { y: 0.6 } });
    } catch {}
  };

  if (!activeChimere) {
    return (
      <div className="p-8 text-center text-slate-400 font-mono">
        Aucune Chimère dans votre escouade. Explorez les rues pour en capturer.
      </div>
    );
  }

  const isMechanical = activeChimere.type === 'mechanical';

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Banner */}
      <div className="bg-gradient-to-r from-purple-950/50 via-slate-900 to-slate-950 border border-purple-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-purple-500/20 border border-purple-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(168,85,247,0.3)]">
            🧬
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Laboratoire d’Évolution Modulaire des Chimères</h2>
              <span className="text-[10px] bg-purple-950 text-purple-300 border border-purple-800 px-2 py-0.5 rounded font-mono">
                {isMechanical ? 'Modularité Mécanique Technoïde' : 'Morphogenèse & Métabolisme Biomutant'}
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Évolution chirurgicale non-linéaire : catalyseurs biochimiques ou puces électroniques overclockées.
            </p>
          </div>
        </div>
      </div>

      {evolutionFeedback && (
        <div className="bg-emerald-950/80 border border-emerald-500/80 text-emerald-300 px-4 py-2.5 rounded-xl font-mono text-xs mb-4 animate-fadeIn flex items-center justify-between">
          <span>{evolutionFeedback}</span>
          <Check className="w-4 h-4" />
        </div>
      )}

      {/* Main Grid: Selector + Modding Lab */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Chimere Selector */}
        <div className="space-y-2">
          <div className="text-xs font-tech font-bold text-slate-400 uppercase tracking-wider">
            1. Chimères de l’Escouade
          </div>
          {player.chimeres.map(c => {
            const isSelected = c.id === activeChimere.id;
            return (
              <button
                key={c.id}
                onClick={() => {
                  setSelectedChimereId(c.id);
                  soundFx.playRadarPing(700);
                }}
                className={`w-full p-3 rounded-xl border text-left transition-all flex items-center justify-between ${
                  isSelected
                    ? 'bg-purple-500/20 border-purple-500 shadow-md scale-102'
                    : 'bg-slate-900 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-center gap-2.5">
                  <span className="text-2xl">{c.avatarIcon}</span>
                  <div>
                    <h3 className="font-tech font-bold text-xs text-white">{c.name}</h3>
                    <span className="text-[10px] font-mono text-purple-400">Niveau {c.level} · {c.type === 'mechanical' ? 'Technoïde' : 'Biomutant'}</span>
                  </div>
                </div>
                {isSelected && <Check className="w-4 h-4 text-purple-400" />}
              </button>
            );
          })}
        </div>

        {/* Evolution Lab */}
        <div className="lg:col-span-2 bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4 border-b border-slate-800 pb-3">
              <div className="flex items-center gap-3">
                <span className="text-4xl">{activeChimere.avatarIcon}</span>
                <div>
                  <h3 className="font-tech text-base font-bold text-white uppercase">{activeChimere.name}</h3>
                  <div className="flex items-center gap-2 text-xs font-mono text-slate-400">
                    <span>PV : <strong className="text-emerald-400">{activeChimere.hp} / {activeChimere.maxHp}</strong></span>
                    <span>· Attaque : <strong className="text-red-400">{activeChimere.attack}</strong></span>
                    <span>· Défense : <strong className="text-cyan-400">{activeChimere.defense}</strong></span>
                  </div>
                </div>
              </div>
            </div>

            {/* IF BIOMUTANT: Nutrients & Morphogenesis */}
            {!isMechanical ? (
              <div className="space-y-4">
                <div>
                  <div className="text-xs font-tech font-bold text-emerald-400 uppercase mb-2">
                    A. Nutrition Métabolique (Catalyseurs Organiques)
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex flex-col justify-between">
                      <div>
                        <div className="font-tech text-xs font-bold text-white">Résines Soufrées d'Usine</div>
                        <p className="text-[10px] text-slate-400 font-mono">Épaissit la cuirasse (+15 PV max).</p>
                      </div>
                      <button
                        onClick={() => handleFeedNutrient('Résines Soufrées', '+15 PV')}
                        className="mt-2 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-tech font-bold text-xs rounded-lg"
                      >
                        Nourrir
                      </button>
                    </div>

                    <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex flex-col justify-between">
                      <div>
                        <div className="font-tech text-xs font-bold text-white">Distillat d’Algues Fluviales</div>
                        <p className="text-[10px] text-slate-400 font-mono">Développe la puissance d'attaque (+3 Attaque).</p>
                      </div>
                      <button
                        onClick={() => handleFeedNutrient('Distillat d’Algues', '+3 ATK')}
                        className="mt-2 py-1.5 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-xs rounded-lg"
                      >
                        Nourrir
                      </button>
                    </div>
                  </div>
                </div>

                <div>
                  <div className="text-xs font-tech font-bold text-purple-400 uppercase mb-2">
                    B. Stades de Morphogenèse
                  </div>
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      onClick={() => handleApplyMorphogenesis('Symbiote Domestiqué')}
                      className="p-3 bg-slate-950 border border-slate-800 hover:border-purple-500 rounded-xl text-left transition-all"
                    >
                      <div className="font-tech text-xs font-bold text-white">1. Symbiote Domestiqué</div>
                      <p className="text-[10px] text-slate-400 font-mono">Harnais neuro-inhibiteur : assistance au forage.</p>
                    </button>

                    <button
                      onClick={() => handleApplyMorphogenesis('Titan Mutagène')}
                      className="p-3 bg-slate-950 border border-slate-800 hover:border-purple-500 rounded-xl text-left transition-all"
                    >
                      <div className="font-tech text-xs font-bold text-white">2. Titan Mutagène</div>
                      <p className="text-[10px] text-slate-400 font-mono">Second membre d'attaque et glandes d'acide.</p>
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              /* IF TECHNOIDE: Mechanical Modular Upgrades */
              <div className="space-y-4">
                <div className="text-xs font-tech font-bold text-cyan-400 uppercase mb-2">
                  Modularité Matérielle & Overclocking
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                  <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex flex-col justify-between">
                    <div>
                      <div className="font-tech text-xs font-bold text-white">Optiques Infrarouges 50m</div>
                      <p className="text-[10px] text-slate-400 font-mono">Capteur militaire repérant les filons à travers les murs.</p>
                    </div>
                    <button
                      onClick={() => handleTechnoideUpgrade('Optiques Infrarouges 50m', '+5 ATK')}
                      className="mt-2 py-1.5 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-xs rounded-lg"
                    >
                      Installer
                    </button>
                  </div>

                  <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex flex-col justify-between">
                    <div>
                      <div className="font-tech text-xs font-bold text-white">Overclocking CPU</div>
                      <p className="text-[10px] text-slate-400 font-mono">Puces bancaires : vitesse de calcul accrue.</p>
                    </div>
                    <button
                      onClick={() => handleTechnoideUpgrade('Overclocking CPU', '+8 DEF')}
                      className="mt-2 py-1.5 bg-amber-600 hover:bg-amber-500 text-slate-950 font-tech font-bold text-xs rounded-lg"
                    >
                      Overclocker
                    </button>
                  </div>

                  <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex flex-col justify-between">
                    <div>
                      <div className="font-tech text-xs font-bold text-white">Blindage Titane Laminé</div>
                      <p className="text-[10px] text-slate-400 font-mono">Plaques soudées : rempart mobile pour les raids.</p>
                    </div>
                    <button
                      onClick={() => handleTechnoideUpgrade('Blindage Titane Laminé', '+20 PV')}
                      className="mt-2 py-1.5 bg-purple-600 hover:bg-purple-500 text-white font-tech font-bold text-xs rounded-lg"
                    >
                      Souder
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="mt-4 p-3 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-slate-400">
            🧬 Les Chimères n'évoluent pas passivement : chaque gain est le résultat d'un assemblage ou d'un métabolisme ciblé à l'abri.
          </div>
        </div>
      </div>
    </div>
  );
};
