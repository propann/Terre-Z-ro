import React, { useState } from 'react';
import { PlayerState, SkillPerk, PhysicalMilestone } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Footprints,
  Award,
  Zap,
  Shield,
  Cpu,
  Eye,
  Hammer,
  Radio,
  Flame,
  Check,
  Lock,
  Sparkles,
  AlertTriangle,
  Skull,
  Crosshair,
  Compass
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface CharacterProgressionViewProps {
  player: PlayerState;
  onUnlockPerk: (perkId: string, cost: number) => void;
  onUpgradeImplant: (implantType: 'ocular' | 'spinal' | 'cerebral' | 'dermal') => void;
  onRecoverDeathCrate?: () => void;
}

export const CharacterProgressionView: React.FC<CharacterProgressionViewProps> = ({
  player,
  onUnlockPerk,
  onUpgradeImplant,
  onRecoverDeathCrate
}) => {
  const [activeTab, setActiveTab] = useState<'physical' | 'trees' | 'implants' | 'deathcrate'>('physical');
  const [selectedBranch, setSelectedBranch] = useState<'engineer' | 'biotracker' | 'combat'>('engineer');

  const totalKm = (player.distanceWalkedMeters / 1000).toFixed(1);

  // 1. Physical Milestones
  const milestones: PhysicalMilestone[] = [
    { km: 10, traitName: 'Foulée Économique', effect: 'Réduit de 15% la fatigue lors des sprints d’évasion.', unlocked: player.distanceWalkedMeters >= 10000, icon: '👟' },
    { km: 50, traitName: 'Dos d’Acier', effect: 'Débloque +15 kg de charge maximale dans le sac.', unlocked: player.distanceWalkedMeters >= 50000, icon: '🎒' },
    { km: 100, traitName: 'Sens du Pisteur', effect: 'Augmente le rayon de détection passive des créatures (+10m).', unlocked: player.distanceWalkedMeters >= 100000, icon: '📡' },
    { km: 250, traitName: 'Métabolisme Durci', effect: 'Résistance passive aux zones radioactives et toxiques (+20%).', unlocked: player.distanceWalkedMeters >= 250000, icon: '☢️' },
    { km: 500, traitName: 'Vagabond Vétéran', effect: 'Vitesse de marche accrue et rations réduites de moitié.', unlocked: player.distanceWalkedMeters >= 500000, icon: '🏆' }
  ];

  // 2. Skill Perks by Branch
  const perksByBranch: Record<string, SkillPerk[]> = {
    engineer: [
      { id: 'eng_t1', branch: 'engineer', tier: 1, name: 'Découpe Chirurgicale', desc: 'Réduit de 30% la perte de matière noble lors du minage.', unlocked: player.unlockedPerks?.includes('eng_t1') || false, cost: 1 },
      { id: 'eng_t2', branch: 'engineer', tier: 2, name: 'Sonde de Résonance', desc: 'Le scanner sonar indique la composition exacte des murs à travers 3 couches.', unlocked: player.unlockedPerks?.includes('eng_t2') || false, cost: 1 },
      { id: 'eng_t3', branch: 'engineer', tier: 3, name: 'Béton Armé Rapide', desc: 'Vitesse de pose doublée et résistance structurelle +25%.', unlocked: player.unlockedPerks?.includes('eng_t3') || false, cost: 2 },
      { id: 'eng_t4', branch: 'engineer', tier: 4, name: 'Maître Recycleur', desc: 'Rendement de la fonderie +20% et désassemblage sans perte.', unlocked: player.unlockedPerks?.includes('eng_t4') || false, cost: 2 },
      { id: 'eng_ult', branch: 'engineer', tier: 5, name: 'Surcharge d’Atelier (Ultime)', desc: 'Alimente les machines du bunker avec 50% d’énergie en moins.', unlocked: player.unlockedPerks?.includes('eng_ult') || false, cost: 3, isUltimate: true }
    ],
    biotracker: [
      { id: 'bio_t1', branch: 'biotracker', tier: 1, name: 'Fréquence de Capture', desc: 'Taux de réussite des puces de piratage et injecteurs +15%.', unlocked: player.unlockedPerks?.includes('bio_t1') || false, cost: 1 },
      { id: 'bio_t2', branch: 'biotracker', tier: 2, name: 'Écholocalisation', desc: 'Révèle nids et empreintes dans un rayon de 500m sur la carte.', unlocked: player.unlockedPerks?.includes('bio_t2') || false, cost: 1 },
      { id: 'bio_t3', branch: 'biotracker', tier: 3, name: 'Empathie Mutante', desc: 'Les bêtes sauvages de bas niveau n’attaquent plus à vue.', unlocked: player.unlockedPerks?.includes('bio_t3') || false, cost: 2 },
      { id: 'bio_t4', branch: 'biotracker', tier: 4, name: 'Siphon Génétique', desc: 'Récolte le double d’enzymes lors des dissections propres.', unlocked: player.unlockedPerks?.includes('bio_t4') || false, cost: 2 },
      { id: 'bio_ult', branch: 'biotracker', tier: 5, name: 'Lien Synaptique Double (Ultime)', desc: 'Permet de déployer 2 Chimères simultanément en escorte.', unlocked: player.unlockedPerks?.includes('bio_ult') || false, cost: 3, isUltimate: true }
    ],
    combat: [
      { id: 'com_t1', branch: 'combat', tier: 1, name: 'Stabilisateur de Tir', desc: 'Réduit le tremblement du réticule lors du ciblage en marchant.', unlocked: player.unlockedPerks?.includes('com_t1') || false, cost: 1 },
      { id: 'com_t2', branch: 'combat', tier: 2, name: 'Perforateur Thermique', desc: 'Les tirs sur les blindages en acier fragilisent les blocs adjacents.', unlocked: player.unlockedPerks?.includes('com_t2') || false, cost: 1 },
      { id: 'com_t3', branch: 'combat', tier: 3, name: 'Charge d’Impact', desc: 'Charge au corps à corps pulvérisant portes et murs fragilisés.', unlocked: player.unlockedPerks?.includes('com_t3') || false, cost: 2 },
      { id: 'com_t4', branch: 'combat', tier: 4, name: 'Cuirasse de Récup’', desc: 'Réduit les dégâts reçus si à proximité d’un mur ou d’une épave.', unlocked: player.unlockedPerks?.includes('com_t4') || false, cost: 2 },
      { id: 'com_ult', branch: 'combat', tier: 5, name: 'Dernier Rempart (Ultime)', desc: 'En cas de coup fatal : 5 secondes d’invulnérabilité pour fuir ou se soigner.', unlocked: player.unlockedPerks?.includes('com_ult') || false, cost: 3, isUltimate: true }
    ]
  };

  const handleUnlock = (perk: SkillPerk) => {
    if (perk.unlocked || (player.masteryPoints || 0) < perk.cost) return;
    soundFx.playCaptureSuccess();
    onUnlockPerk(perk.id, perk.cost);
    try {
      confetti({ particleCount: 50, spread: 60, origin: { y: 0.6 } });
    } catch {}
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Header Banner */}
      <div className="bg-gradient-to-r from-amber-950/50 via-slate-900 to-slate-950 border border-amber-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-amber-500/20 border border-amber-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(245,158,11,0.3)]">
            🧬
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Progression & Survie : Le Survivant</h2>
              <span className="text-[10px] bg-amber-950 text-amber-300 border border-amber-800 px-2 py-0.5 rounded font-mono">
                Marche IRL Reconnue · Pas de montée magique
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Distance marchée : <strong className="text-amber-400">{totalKm} km</strong> · Points de Maîtrise : <strong className="text-cyan-400">{player.masteryPoints || 0} PTS</strong>
            </p>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1.5 bg-slate-900 p-1.5 rounded-2xl border border-slate-800 mb-4 max-w-2xl">
        {[
          { id: 'physical', label: 'Condition Physique (IRL)', icon: Footprints },
          { id: 'trees', label: 'Arbres de Maîtrise (3 Voies)', icon: Award },
          { id: 'implants', label: 'Implants Cybernétiques', icon: Cpu },
          { id: 'deathcrate', label: 'Caisse de Mort (24h)', icon: Skull }
        ].map(t => {
          const Icon = t.icon;
          const isActive = activeTab === t.id;
          return (
            <button
              key={t.id}
              onClick={() => {
                setActiveTab(t.id as any);
                soundFx.playRadarPing(700);
              }}
              className={`flex-1 py-2 px-3 rounded-xl text-xs font-tech font-bold transition-all flex items-center justify-center gap-1.5 ${
                isActive ? 'bg-amber-500 text-slate-950 shadow-lg scale-102' : 'text-slate-400 hover:text-white'
              }`}
            >
              <Icon className="w-4 h-4" />
              <span>{t.label}</span>
            </button>
          );
        })}
      </div>

      {/* 1. PHYSICAL MILESTONES */}
      {activeTab === 'physical' && (
        <div className="space-y-3">
          <div className="text-xs font-mono text-slate-400 mb-2">
            La condition physique se développe par le mouvement réel. Le podomètre et le filtre GPS débloquent des seuils permanents :
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {milestones.map(m => (
              <div
                key={m.km}
                className={`p-4 rounded-2xl border transition-all flex items-start gap-3 ${
                  m.unlocked
                    ? 'bg-slate-900/90 border-emerald-500/60 shadow-lg'
                    : 'bg-slate-950/60 border-slate-800 opacity-60'
                }`}
              >
                <div className="text-3xl p-2 rounded-xl bg-slate-950 border border-slate-800">{m.icon}</div>
                <div className="flex-1">
                  <div className="flex items-center justify-between">
                    <h4 className="font-tech text-sm font-bold text-white">{m.traitName}</h4>
                    <span className={`text-[10px] font-mono px-2 py-0.5 rounded ${
                      m.unlocked ? 'bg-emerald-950 text-emerald-300 border border-emerald-800' : 'bg-slate-800 text-slate-400'
                    }`}>
                      {m.km} km {m.unlocked ? '✓ DÉBLOQUÉ' : 'VERROUILLÉ'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-1">{m.effect}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 2. SKILL TREES */}
      {activeTab === 'trees' && (
        <div className="space-y-4">
          {/* Branch Switcher */}
          <div className="flex items-center gap-2">
            {[
              { id: 'engineer', label: 'Voie A : Ingénieur de Brèche', icon: Hammer, color: 'border-orange-500 text-orange-400' },
              { id: 'biotracker', label: 'Voie B : Bio-Pisteur', icon: Radio, color: 'border-emerald-500 text-emerald-400' },
              { id: 'combat', label: 'Voie C : Ferrailleur Lourd', icon: Flame, color: 'border-red-500 text-red-400' }
            ].map(b => {
              const Icon = b.icon;
              const isSel = selectedBranch === b.id;
              return (
                <button
                  key={b.id}
                  onClick={() => {
                    setSelectedBranch(b.id as any);
                    soundFx.playRadarPing(800);
                  }}
                  className={`flex-1 py-2.5 px-3 rounded-xl text-xs font-tech font-bold border transition-all flex items-center justify-center gap-1.5 ${
                    isSel ? `bg-slate-900 ${b.color} shadow-lg scale-102` : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  <span>{b.label}</span>
                </button>
              );
            })}
          </div>

          {/* Perks List */}
          <div className="space-y-2">
            {perksByBranch[selectedBranch]?.map(perk => (
              <div
                key={perk.id}
                className={`p-3.5 rounded-2xl border flex items-center justify-between transition-all ${
                  perk.unlocked
                    ? 'bg-slate-900 border-emerald-500/60'
                    : perk.isUltimate
                    ? 'bg-purple-950/30 border-purple-800/80'
                    : 'bg-slate-950 border-slate-800'
                }`}
              >
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-lg flex items-center justify-center font-mono font-bold text-xs ${
                    perk.unlocked ? 'bg-emerald-500 text-slate-950' : 'bg-slate-800 text-slate-400'
                  }`}>
                    T{perk.tier}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h4 className="font-tech text-xs font-bold text-white">{perk.name}</h4>
                      {perk.isUltimate && <span className="text-[9px] bg-purple-950 text-purple-300 px-1 rounded font-mono">ULTIME</span>}
                    </div>
                    <p className="text-[11px] text-slate-400 font-mono">{perk.desc}</p>
                  </div>
                </div>

                <button
                  onClick={() => handleUnlock(perk)}
                  disabled={perk.unlocked || (player.masteryPoints || 0) < perk.cost}
                  className={`px-3 py-1.5 rounded-xl text-xs font-tech font-bold transition-all ${
                    perk.unlocked
                      ? 'bg-emerald-950 text-emerald-400 border border-emerald-800'
                      : (player.masteryPoints || 0) >= perk.cost
                      ? 'bg-cyan-600 hover:bg-cyan-500 text-white shadow-md active:scale-95'
                      : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  }`}
                >
                  {perk.unlocked ? '✓ ACQUIS' : `DÉBLOQUER (${perk.cost} PT)`}
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 3. CYBERNETIC IMPLANTS */}
      {activeTab === 'implants' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Implant Oculaire */}
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-cyan-400 font-tech font-bold text-sm mb-1">
                <Eye className="w-5 h-5" />
                <span>1. Implant Oculaire (Scanner Spectrométrique)</span>
              </div>
              <p className="text-xs text-slate-400 mb-3">
                Niv. {player.implants?.ocularLevel || 1} / 3 : Filtre thermique $\rightarrow$ Failles structurelles $\rightarrow$ Spectromètre de métaux.
              </p>
            </div>
            <button
              onClick={() => onUpgradeImplant('ocular')}
              className="py-2 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-xs rounded-xl"
            >
              Améliorer l’Optique au Bunker
            </button>
          </div>

          {/* Implant Rachidien */}
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-amber-400 font-tech font-bold text-sm mb-1">
                <Zap className="w-5 h-5" />
                <span>2. Implant Rachidien (Exosquelette Dorsal)</span>
              </div>
              <p className="text-xs text-slate-400 mb-3">
                {player.implants?.spinalInstalled ? '✓ Exosquelette actif (+Charge utile, pas de malus forage lourd)' : 'Non greffé (Nécessite des pièces high-tech)'}
              </p>
            </div>
            <button
              onClick={() => onUpgradeImplant('spinal')}
              className="py-2 bg-amber-600 hover:bg-amber-500 text-slate-950 font-tech font-bold text-xs rounded-xl"
            >
              Greffer l’Exosquelette
            </button>
          </div>

          {/* Implant Cérébral */}
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-purple-400 font-tech font-bold text-sm mb-1">
                <Cpu className="w-5 h-5" />
                <span>3. Implant Cérébral (Interface de Piratage)</span>
              </div>
              <p className="text-xs text-slate-400 mb-3">
                {player.implants?.cerebralInstalled ? '✓ Interface active (-50% temps d’injection modules de contrôle)' : 'Non greffé'}
              </p>
            </div>
            <button
              onClick={() => onUpgradeImplant('cerebral')}
              className="py-2 bg-purple-600 hover:bg-purple-500 text-white font-tech font-bold text-xs rounded-xl"
            >
              Greffer l’Interface
            </button>
          </div>

          {/* Implant Dermique */}
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-emerald-400 font-tech font-bold text-sm mb-1">
                <Shield className="w-5 h-5" />
                <span>4. Implant Dermique (Blindage Sous-Cutané)</span>
              </div>
              <p className="text-xs text-slate-400 mb-3">
                Grille sous-cutanée réduisant les dégâts d’acide et d’entailles des bêtes sauvages.
              </p>
            </div>
            <button
              onClick={() => onUpgradeImplant('dermal')}
              className="py-2 bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-tech font-bold text-xs rounded-xl"
            >
              Renforcer le Derme
            </button>
          </div>
        </div>
      )}

      {/* 4. DEATH CRATE RADAR & 24H RECOVERY */}
      {activeTab === 'deathcrate' && (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4">
          <div className="flex items-center gap-2 text-red-400 font-tech font-bold text-sm mb-2">
            <Skull className="w-5 h-5" />
            <span>Balise de Caisse de Mort (Death Crate)</span>
          </div>

          <p className="text-xs text-slate-400 leading-relaxed mb-4">
            En cas de mort, vos ressources brutes restent au sol dans une caisse aux coordonnées exactes de votre trépas.
            Vous avez <strong>24 heures réelles</strong> pour vous y rendre physiquement en marchant IRL avant pillage ou dissolution.
          </p>

          <div className="p-4 bg-slate-950 rounded-xl border border-slate-800 flex items-center justify-between">
            <div>
              <div className="text-xs font-mono text-white">Coordonnées GPS de la caisse : 48.8584° N, 2.2945° E</div>
              <span className="text-[11px] font-mono text-emerald-400">Temps restant : 21h 45m · Distance : 18m (À portée)</span>
            </div>

            <button
              onClick={() => {
                soundFx.playLootPickup();
                try {
                  confetti({ particleCount: 40, spread: 60, origin: { y: 0.6 } });
                } catch {}
                if (onRecoverDeathCrate) onRecoverDeathCrate();
              }}
              className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-tech font-bold text-xs rounded-xl shadow-lg active:scale-95"
            >
              RÉCUPÉRER LE SAC (25M)
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
