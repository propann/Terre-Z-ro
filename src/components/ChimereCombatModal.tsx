import React, { useState, useEffect } from 'react';
import { Chimere, PlayerState, Item, ChimereAnatomyPart } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Shield,
  Zap,
  Heart,
  Sparkles,
  AlertOctagon,
  Award,
  X,
  Radio,
  Sword,
  Crosshair,
  Cpu,
  Target,
  Flame,
  AlertTriangle
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface ChimereCombatModalProps {
  chimere: Chimere;
  player: PlayerState;
  onClose: () => void;
  onCaptureSuccess: (chimere: Chimere, xpGained: number) => void;
  onDefeatSuccess: (chimere: Chimere, loot: Item[], xpGained: number) => void;
  onPlayerTakeDamage: (damage: number) => void;
}

export const ChimereCombatModal: React.FC<ChimereCombatModalProps> = ({
  chimere,
  player,
  onClose,
  onCaptureSuccess,
  onDefeatSuccess,
  onPlayerTakeDamage
}) => {
  const [enemyHp, setEnemyHp] = useState<number>(chimere.hp);
  const [playerHp, setPlayerHp] = useState<number>(player.health);
  const [selectedTargetPart, setSelectedTargetPart] = useState<'legs' | 'tank' | 'armor' | 'core'>('armor');

  // Voxel Anatomy State
  const [anatomy, setAnatomy] = useState<ChimereAnatomyPart[]>([
    { id: 'armor', name: 'Plaque de Blindage Frontal', hp: 30, maxHp: 30, broken: false, effectDesc: 'Protège le noyau vital' },
    { id: 'legs', name: 'Vérins & Membres Locomoteurs', hp: 25, maxHp: 25, broken: false, effectDesc: 'Réduit l’esquive et l’attaque' },
    { id: 'tank', name: chimere.type === 'mechanical' ? 'Générateur / Condensateur' : 'Poche de Bile Acide', hp: 20, maxHp: 20, broken: false, effectDesc: 'Provoque une fuite interne continue' },
    { id: 'core', name: 'Noyau Critique', hp: 40, maxHp: 40, broken: false, effectDesc: 'Attention : le pulvériser détruit la créature !' }
  ]);

  const [isOverkilled, setIsOverkilled] = useState<boolean>(false);
  const [combatLog, setCombatLog] = useState<string[]>([
    `⚠️ ${chimere.name} (${chimere.type === 'mechanical' ? 'Technoïde' : 'Biomutant'}) repéré ! Visez chirurgicalement ses composants en voxel.`
  ]);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const [battleState, setBattleState] = useState<'active' | 'hacking' | 'captured' | 'defeated' | 'overkilled'>('active');

  // Hacking / Injection Minigame State
  const [pulsePos, setPulsePos] = useState<number>(50);
  const [targetZone, setTargetZone] = useState<{ min: number; max: number }>({ min: 38, max: 62 });
  const [pulseDirection, setPulseDirection] = useState<number>(1);

  const isMechanical = chimere.type === 'mechanical';
  const captureItemName = isMechanical ? "Puce d'Override IEM" : "Injecteur Neurotoxique";

  // Calculate current capture chance based on GDD formula:
  // [Qualite du module] * [Integrite restante du noyau] / [Niveau de la Chimere]
  const hpRatio = enemyHp / chimere.maxHp;
  const corePart = anatomy.find(p => p.id === 'core');
  const coreIntegrity = corePart ? corePart.hp / corePart.maxHp : 0.5;
  const currentCaptureRate = Math.min(95, Math.max(15, Math.round((40 + (1 - hpRatio) * 45) * (0.5 + coreIntegrity * 0.5))));

  // Oscillator for the capture pulse
  useEffect(() => {
    if (battleState !== 'hacking') return;
    const interval = setInterval(() => {
      setPulsePos(prev => {
        let next = prev + pulseDirection * 4;
        if (next >= 95) {
          setPulseDirection(-1);
          next = 95;
        } else if (next <= 5) {
          setPulseDirection(1);
          next = 5;
        }
        return next;
      });
    }, 28);
    return () => clearInterval(interval);
  }, [battleState, pulseDirection]);

  // Targeted Surgical Attack
  const handleTargetedAttack = () => {
    if (isProcessing || battleState !== 'active') return;
    setIsProcessing(true);
    soundFx.playHitSound();

    const baseDamage = Math.round(18 + Math.random() * 12 + player.level * 2.5);
    const log: string[] = [];

    // Apply damage to targeted anatomical voxel part
    setAnatomy(prev =>
      prev.map(part => {
        if (part.id === selectedTargetPart) {
          const newPartHp = Math.max(0, part.hp - baseDamage);
          const wasBroken = part.broken;
          const isNowBroken = newPartHp === 0;

          if (!wasBroken && isNowBroken) {
            log.push(`🎯 COMPOSANT BRISÉ : ${part.name} est détruit ! (${part.effectDesc})`);
          } else {
            log.push(`💥 Frappe chirurgicale sur ${part.name} (-${baseDamage} PV)`);
          }
          return { ...part, hp: newPartHp, broken: isNowBroken };
        }
        return part;
      })
    );

    const newEnemyHp = Math.max(0, enemyHp - baseDamage);
    setEnemyHp(newEnemyHp);

    // Overkill Check: if core is destroyed or damage is excessive
    if (selectedTargetPart === 'core' && newEnemyHp <= 0) {
      log.push(`⚠️ ACHERNEMENT CRITIQUE : Le noyau a été pulvérisé ! Les composants sont calcinés, capture impossible.`);
      setCombatLog(prev => [...log, ...prev]);
      setIsOverkilled(true);
      setBattleState('overkilled');
      setIsProcessing(false);
      soundFx.playHitSound();
      return;
    }

    if (newEnemyHp <= 0) {
      log.push(`🏆 ${chimere.name} est neutralisée !`);
      setCombatLog(prev => [...log, ...prev]);
      setBattleState('defeated');
      setIsProcessing(false);
      soundFx.playCaptureSuccess();
      return;
    }

    // Chimere Counter-Attack (weakened if legs/tank broken)
    setTimeout(() => {
      const legsBroken = anatomy.find(p => p.id === 'legs')?.broken;
      const attackMultiplier = legsBroken ? 0.6 : 1.0;
      const enemyDmg = Math.max(4, Math.round((chimere.attack - player.level * 1.5) * attackMultiplier));
      const newPlayerHp = Math.max(0, playerHp - enemyDmg);
      setPlayerHp(newPlayerHp);
      onPlayerTakeDamage(enemyDmg);
      log.push(`⚡ ${chimere.name} riposte et vous inflige ${enemyDmg} dégâts !`);
      setCombatLog(prev => [...log, ...prev]);
      setIsProcessing(false);
    }, 450);
  };

  // Launch Capture Mode
  const handleStartCapture = () => {
    if (isProcessing || battleState !== 'active' || isOverkilled) return;
    setBattleState('hacking');
    setTargetZone({
      min: Math.floor(32 + Math.random() * 20),
      max: Math.floor(58 + Math.random() * 20)
    });
    soundFx.playRadarPing(850);
  };

  // Lock Frequency / Inject Neurotoxin
  const handleLockInjection = () => {
    const isHit = pulsePos >= targetZone.min && pulsePos <= targetZone.max;
    const bonus = isHit ? 35 : -25;
    const totalChance = Math.min(95, Math.max(10, currentCaptureRate + bonus));
    const roll = Math.random() * 100;

    if (roll < totalChance) {
      setBattleState('captured');
      setCombatLog(prev => [
        `🎉 ${isHit ? 'Injection parfaite !' : 'Asservissement réussi in-extremis !'} ${chimere.name} est liée à votre abri.`,
        ...prev
      ]);
      soundFx.playCaptureSuccess();
      try {
        confetti({ particleCount: 85, spread: 75, origin: { y: 0.6 } });
      } catch {}
    } else {
      setBattleState('active');
      setCombatLog(prev => [
        `❌ Rejet du module ! ${chimere.name} est entrée en état de surcharge et tente de fuir !`,
        ...prev
      ]);
      soundFx.playHitSound();
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-950/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-slate-900 border-2 border-orange-500/80 rounded-3xl w-full max-w-xl overflow-hidden shadow-[0_0_40px_rgba(249,115,22,0.3)] flex flex-col animate-scaleUp">
        {/* Header */}
        <div className="bg-gradient-to-r from-orange-950/70 via-slate-900 to-slate-950 p-4 border-b border-orange-500/30 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="text-3xl">{chimere.avatarIcon}</span>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="font-tech text-base font-bold text-white uppercase tracking-wider">{chimere.name}</h2>
                <span className="text-[10px] bg-orange-950 text-orange-400 border border-orange-800 px-2 py-0.5 rounded font-mono">
                  {isMechanical ? 'Technoïde Lourd' : 'Biomutant Sylvestre'}
                </span>
              </div>
              <span className="text-[11px] font-mono text-slate-400">Niv. {chimere.level} · Rayon GPS 25m</span>
            </div>
          </div>
          <button onClick={onClose} className="p-1.5 rounded-full bg-slate-800 text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Global HP Bars */}
        <div className="p-4 grid grid-cols-2 gap-3 bg-slate-950/70 border-b border-slate-800">
          <div>
            <div className="flex justify-between text-xs font-mono mb-1">
              <span className="text-slate-400">{chimere.name}</span>
              <span className="text-red-400 font-bold">{enemyHp} / {chimere.maxHp} PV</span>
            </div>
            <div className="w-full bg-slate-800 h-2.5 rounded-full overflow-hidden">
              <div
                className="bg-gradient-to-r from-red-600 to-amber-500 h-full transition-all duration-300"
                style={{ width: `${(enemyHp / chimere.maxHp) * 100}%` }}
              />
            </div>
          </div>

          <div>
            <div className="flex justify-between text-xs font-mono mb-1">
              <span className="text-slate-400">Votre Survivant</span>
              <span className="text-emerald-400 font-bold">{playerHp} / {player.maxHealth} PV</span>
            </div>
            <div className="w-full bg-slate-800 h-2.5 rounded-full overflow-hidden">
              <div
                className="bg-gradient-to-r from-emerald-500 to-cyan-500 h-full transition-all duration-300"
                style={{ width: `${(playerHp / player.maxHealth) * 100}%` }}
              />
            </div>
          </div>
        </div>

        {/* ANATOMIE VOXEL & CIBLAGE CHIRURGICAL */}
        <div className="p-4 border-b border-slate-800 bg-slate-900/50">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-1.5 text-xs font-tech font-bold text-amber-400 uppercase">
              <Target className="w-4 h-4" />
              <span>Ciblage Anatomique Voxel</span>
            </div>
            <span className="text-[10px] font-mono text-slate-400">Sélectionnez la zone à forer</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
            {anatomy.map(part => {
              const isSelected = selectedTargetPart === part.id;
              return (
                <button
                  key={part.id}
                  onClick={() => {
                    setSelectedTargetPart(part.id);
                    soundFx.playRadarPing(750);
                  }}
                  className={`p-2 rounded-xl text-left border transition-all flex flex-col justify-between ${
                    part.broken
                      ? 'bg-red-950/40 border-red-800 opacity-60'
                      : isSelected
                      ? 'bg-amber-500/20 border-amber-500 shadow-md scale-102'
                      : 'bg-slate-950 border-slate-800 hover:border-slate-700'
                  }`}
                >
                  <div className="flex items-center justify-between w-full mb-1">
                    <span className="text-[11px] font-tech font-bold text-white truncate">{part.name}</span>
                    {part.broken && <span className="text-[9px] bg-red-950 text-red-400 px-1 rounded">BRISÉ</span>}
                  </div>
                  <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden mb-1">
                    <div
                      className={`h-full ${part.id === 'core' ? 'bg-purple-500' : 'bg-cyan-400'}`}
                      style={{ width: `${(part.hp / part.maxHp) * 100}%` }}
                    />
                  </div>
                  <span className="text-[9px] font-mono text-slate-400 truncate">{part.effectDesc}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Combat Area / Hacking Area */}
        <div className="p-4 flex-1 flex flex-col justify-between min-h-[190px]">
          {battleState === 'hacking' ? (
            <div className="bg-slate-950 border border-orange-500/50 rounded-2xl p-4 flex flex-col items-center justify-center gap-3">
              <div className="flex items-center gap-2 text-xs font-tech text-orange-400 font-bold uppercase">
                <Cpu className="w-4 h-4 animate-spin" />
                <span>{isMechanical ? "Piratage de la Puce d'Override" : "Injection du Collier Neurotoxique"}</span>
              </div>

              {/* Frequency / Pulse Bar */}
              <div className="w-full h-8 bg-slate-900 border border-slate-700 rounded-xl relative overflow-hidden">
                <div
                  className="absolute top-0 bottom-0 bg-emerald-500/40 border-x-2 border-emerald-400"
                  style={{
                    left: `${targetZone.min}%`,
                    width: `${targetZone.max - targetZone.min}%`
                  }}
                />
                <div
                  className="absolute top-0 bottom-0 w-2.5 bg-orange-400 shadow-[0_0_12px_#f97316] transition-all"
                  style={{ left: `${pulsePos}%` }}
                />
              </div>

              <button
                onClick={handleLockInjection}
                className="w-full py-3 bg-gradient-to-r from-orange-600 to-amber-600 hover:from-orange-500 hover:to-amber-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg active:scale-95 flex items-center justify-center gap-2"
              >
                <Target className="w-4 h-4" />
                <span>VERROUILLER L’INJECTION ({currentCaptureRate}% DE CHANCE)</span>
              </button>
            </div>
          ) : (
            <div className="bg-slate-950/80 border border-slate-800 rounded-xl p-3 h-28 overflow-y-auto font-mono text-xs text-slate-300 space-y-1">
              {combatLog.map((log, idx) => (
                <div key={idx}>{log}</div>
              ))}
            </div>
          )}

          {/* Action Buttons */}
          {battleState === 'active' && (
            <div className="grid grid-cols-2 gap-2 mt-3">
              <button
                onClick={handleTargetedAttack}
                disabled={isProcessing}
                className="py-3 bg-red-600 hover:bg-red-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg transition-all flex items-center justify-center gap-1.5 active:scale-95"
              >
                <Sword className="w-4 h-4" />
                <span>FORER : {anatomy.find(p => p.id === selectedTargetPart)?.name}</span>
              </button>

              <button
                onClick={handleStartCapture}
                disabled={isProcessing}
                className="py-3 bg-gradient-to-r from-orange-600 to-amber-600 hover:from-orange-500 hover:to-amber-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg transition-all flex items-center justify-center gap-1.5 active:scale-95"
              >
                <Radio className="w-4 h-4" />
                <span>{captureItemName} ({currentCaptureRate}%)</span>
              </button>
            </div>
          )}

          {battleState === 'overkilled' && (
            <div className="mt-3 p-3 bg-red-950/80 border border-red-500 rounded-xl text-center">
              <div className="text-red-300 font-tech font-bold text-xs mb-1">⚠️ CRÉATURE PULVÉRISÉE (INCAPTURABLE)</div>
              <p className="text-[10px] text-slate-400 mb-2">Un excès de dégâts a broyé les puces internes et les organes vitaux.</p>
              <button
                onClick={() => {
                  onDefeatSuccess(chimere, [], 15);
                  onClose();
                }}
                className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-white font-tech font-bold text-xs rounded-lg"
              >
                RÉCUPÉRER QUELQUES GRAVATS & FERRAILLE (+15 XP)
              </button>
            </div>
          )}

          {battleState === 'captured' && (
            <button
              onClick={() => {
                onCaptureSuccess(chimere, 60);
                onClose();
              }}
              className="mt-3 w-full py-3 bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-tech font-bold text-sm rounded-xl shadow-lg active:scale-95"
            >
              LIER LA CHIMÈRE AU BUNKER (+60 XP)
            </button>
          )}

          {battleState === 'defeated' && (
            <button
              onClick={() => {
                onDefeatSuccess(chimere, [], 35);
                onClose();
              }}
              className="mt-3 w-full py-3 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-sm rounded-xl shadow-lg active:scale-95"
            >
              RÉCOLTER COMPOSANTS & ENZYMES (+35 XP)
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
