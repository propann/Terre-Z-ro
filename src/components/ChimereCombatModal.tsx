import React, { useState } from 'react';
import { Chimere, PlayerState, Item } from '../types/game';
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
  Crosshair
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
  const [combatLog, setCombatLog] = useState<string[]>([
    `⚠️ ${chimere.name} (${chimere.type === 'mechanical' ? 'Chimère Mécanique' : 'Chimère Bio-mutée'}) vous attaque !`
  ]);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const [battleState, setBattleState] = useState<'active' | 'captured' | 'defeated' | 'fled'>('active');

  // Traps in inventory
  const empTraps = player.inventory.find(i => i.id.startsWith('wpn-') || i.category === 'trap')?.quantity || 1;

  // Calculate current capture chance (higher when HP is low)
  const hpRatio = enemyHp / chimere.maxHp;
  const currentCaptureRate = Math.min(95, Math.max(15, Math.round(chimere.catchRate + (1 - hpRatio) * 45)));

  // Player Attack
  const handleAttack = () => {
    if (isProcessing || battleState !== 'active') return;
    setIsProcessing(true);
    soundFx.playHitSound();

    const damage = Math.round(20 + Math.random() * 15 + player.level * 3);
    const newEnemyHp = Math.max(0, enemyHp - damage);
    setEnemyHp(newEnemyHp);

    const log = [`💥 Vous infligez ${damage} dégâts à ${chimere.name}.`];

    if (newEnemyHp <= 0) {
      log.push(`🏆 ${chimere.name} est vaincu !`);
      setCombatLog(prev => [...log, ...prev]);
      setBattleState('defeated');
      setIsProcessing(false);

      const lootDrop: Item[] = [
        {
          id: `drop-${Date.now()}`,
          name: chimere.type === 'mechanical' ? 'Noyau Énergétique Surchargé' : 'Glande de Toxine Mutante',
          category: chimere.type === 'mechanical' ? 'electronics' : 'medical',
          weight: 0.8,
          quantity: 1,
          rarity: 'rare',
          icon: chimere.type === 'mechanical' ? '⚡' : '🧪',
          description: `Matériau rare extrait de ${chimere.name}.`
        }
      ];

      setTimeout(() => {
        onDefeatSuccess(chimere, lootDrop, chimere.level * 40);
      }, 1200);
      return;
    }

    // Enemy counter-attack
    setTimeout(() => {
      const enemyDmg = Math.max(5, Math.round(chimere.attack * 0.4 + Math.random() * 8));
      const newPlayerHp = Math.max(0, playerHp - enemyDmg);
      setPlayerHp(newPlayerHp);
      onPlayerTakeDamage(enemyDmg);
      soundFx.playHitSound();
      log.push(`🔴 ${chimere.name} riposte et vous inflige ${enemyDmg} dégâts !`);

      setCombatLog(prev => [...log, ...prev]);
      setIsProcessing(false);
    }, 600);
  };

  // Capture Attempt
  const handleAttemptCapture = () => {
    if (isProcessing || battleState !== 'active') return;
    setIsProcessing(true);

    const roll = Math.random() * 100;
    const log = [`🪤 Lancement d'un piège électro-magnétique sur ${chimere.name}... (${currentCaptureRate}% de chance)`];

    setTimeout(() => {
      if (roll <= currentCaptureRate) {
        soundFx.playCaptureSuccess();
        try {
          confetti({ particleCount: 60, spread: 70, origin: { y: 0.6 } });
        } catch {}

        log.push(`✨ SUCCÈS ! ${chimere.name} est neutralisé et capturé !`);
        setBattleState('captured');
        setCombatLog(prev => [...log, ...prev]);
        setIsProcessing(false);

        setTimeout(() => {
          onCaptureSuccess(chimere, chimere.level * 60);
        }, 1200);
      } else {
        soundFx.playHitSound();
        log.push(`❌ La capture a échoué ! ${chimere.name} s'est libéré avec fureur !`);
        const enemyDmg = Math.max(8, Math.round(chimere.attack * 0.5));
        const newPlayerHp = Math.max(0, playerHp - enemyDmg);
        setPlayerHp(newPlayerHp);
        onPlayerTakeDamage(enemyDmg);
        log.push(`🔴 ${chimere.name} vous inflige ${enemyDmg} dégâts en représailles !`);

        setCombatLog(prev => [...log, ...prev]);
        setIsProcessing(false);
      }
    }, 800);
  };

  // Flee
  const handleFlee = () => {
    if (isProcessing) return;
    setBattleState('fled');
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-950/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="relative w-full max-w-lg bg-slate-900 border-2 border-red-500/50 rounded-3xl p-5 shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
          <div className="flex items-center gap-2">
            <AlertOctagon className="w-5 h-5 text-red-400 animate-pulse" />
            <span className="font-tech text-base uppercase font-bold text-red-400 tracking-wider">
              Engagement Tactique
            </span>
          </div>
          <button
            onClick={handleFlee}
            className="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Chimère Display Card */}
        <div className="relative bg-gradient-to-b from-slate-950 to-slate-900 border border-slate-800 rounded-2xl p-4 mb-4 flex flex-col items-center text-center">
          <div className="relative w-24 h-24 rounded-2xl bg-slate-800/80 border-2 border-red-500/40 flex items-center justify-center text-5xl mb-3 shadow-[0_0_20px_rgba(239,68,68,0.2)]">
            {chimere.avatarIcon}
            <span className="absolute -top-2 -right-2 bg-red-600 text-white font-mono text-[10px] font-bold px-2 py-0.5 rounded-full">
              Niv. {chimere.level}
            </span>
          </div>

          <h3 className="text-lg font-bold text-white font-tech">{chimere.name}</h3>
          <p className="text-xs text-slate-400 font-mono mb-2">
            {chimere.type === 'mechanical' ? '⚙️ Chimère Mécanique Industrielle' : '🧬 Chimère Bio-Mutée des Parcs'}
          </p>

          {/* Enemy HP Bar */}
          <div className="w-full bg-slate-950 p-2 rounded-xl border border-slate-800">
            <div className="flex justify-between text-xs font-mono text-slate-300 mb-1">
              <span>Points de Structure / Vitalité</span>
              <span className="text-red-400 font-bold">{enemyHp} / {chimere.maxHp} PV</span>
            </div>
            <div className="w-full bg-slate-800 h-2.5 rounded-full overflow-hidden">
              <div
                className="bg-red-500 h-full rounded-full transition-all duration-300 shadow-[0_0_8px_#ef4444]"
                style={{ width: `${(enemyHp / chimere.maxHp) * 100}%` }}
              />
            </div>
          </div>

          <p className="text-[11px] text-slate-400 italic mt-2 px-2">{chimere.lore}</p>
        </div>

        {/* Player Status In Combat */}
        <div className="bg-slate-950/80 border border-slate-800 rounded-xl p-3 mb-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Heart className="w-4 h-4 text-emerald-400" />
            <span className="text-xs font-mono text-slate-300">Votre Santé :</span>
            <span className="text-xs font-mono font-bold text-emerald-400">{playerHp}/{player.maxHealth} PV</span>
          </div>
          <div className="flex items-center gap-1.5 text-xs font-mono text-amber-400">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Chance de Capture : <strong>{currentCaptureRate}%</strong></span>
          </div>
        </div>

        {/* Combat Actions */}
        <div className="grid grid-cols-3 gap-2 mb-4">
          <button
            disabled={isProcessing || battleState !== 'active'}
            onClick={handleAttack}
            className="py-3 px-3 bg-red-600 hover:bg-red-500 disabled:opacity-50 text-white font-tech font-bold text-sm rounded-xl shadow-lg flex flex-col items-center justify-center gap-1 transition-all active:scale-95"
          >
            <Sword className="w-4 h-4" />
            <span>Attaquer</span>
          </button>

          <button
            disabled={isProcessing || battleState !== 'active'}
            onClick={handleAttemptCapture}
            className="py-3 px-3 bg-amber-500 hover:bg-amber-400 disabled:opacity-50 text-slate-950 font-tech font-bold text-sm rounded-xl shadow-lg flex flex-col items-center justify-center gap-1 transition-all active:scale-95"
          >
            <Crosshair className="w-4 h-4" />
            <span>Capturer ({currentCaptureRate}%)</span>
          </button>

          <button
            disabled={isProcessing || battleState !== 'active'}
            onClick={handleFlee}
            className="py-3 px-3 bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-slate-300 font-tech font-medium text-sm rounded-xl flex flex-col items-center justify-center gap-1 transition-all"
          >
            <Shield className="w-4 h-4" />
            <span>Fuir</span>
          </button>
        </div>

        {/* Combat Log */}
        <div className="flex-1 bg-slate-950 border border-slate-800 rounded-xl p-3 overflow-y-auto max-h-32 text-xs font-mono space-y-1.5">
          {combatLog.map((entry, idx) => (
            <div key={idx} className="text-slate-300 leading-relaxed">
              {entry}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
