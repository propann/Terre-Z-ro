import React, { useState } from 'react';
import { BunkerState, PlayerState, Item, Chimere, ChimereRole } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Home,
  Zap,
  Shield,
  Hammer,
  Package,
  ArrowRightLeft,
  Award,
  Sparkles,
  Layers,
  BatteryCharging,
  PlusCircle,
  Check,
  Flame,
  Cpu,
  Radio,
  RefreshCw,
  Box
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface BunkerBaseViewProps {
  bunker: BunkerState;
  player: PlayerState;
  onDepositToChest: (item: Item) => void;
  onWithdrawFromChest: (item: Item) => void;
  onCraftItem: (recipeId: string, resultItem: Item, costs: { itemId: string; count: number }[]) => void;
  onAssignChimereRole: (chimereId: string, role: ChimereRole) => void;
  onUpgradeBunkerFacility: (facility: 'core' | 'energy' | 'defenses' | 'workbench') => void;
}

export const BunkerBaseView: React.FC<BunkerBaseViewProps> = ({
  bunker,
  player,
  onDepositToChest,
  onWithdrawFromChest,
  onCraftItem,
  onAssignChimereRole,
  onUpgradeBunkerFacility
}) => {
  const [activeTab, setActiveTab] = useState<'overview' | 'refinery' | 'crafting' | 'storage' | 'chimeres'>('overview');
  const [refineFeedback, setRefineFeedback] = useState<string | null>(null);

  // Material Refinery Recipes (Concasseur & Fonderie)
  const refineryProcesses = [
    {
      id: 'refine-armor',
      title: 'Plaques de Blindage Laminé',
      inputName: 'Ferraille brute (x4)',
      outputName: 'Plaque de Blindage Titane (x1)',
      icon: '🛡️',
      desc: 'Fonderie thermique : transforme les débris industriels en blindage structurel pour bunker et véhicules.',
      inputFamily: 'Métaux & Conducteurs'
    },
    {
      id: 'refine-copper',
      title: 'Bobines Électromagnétiques',
      inputName: 'Câblage Cuivre (x3)',
      outputName: 'Bobine Électromagnétique (x1)',
      icon: '⚡',
      desc: 'Tréfilage et isolation : indispensable pour les pièges IEM, tourelles et conduits d’énergie.',
      inputFamily: 'Métaux & Conducteurs'
    },
    {
      id: 'refine-polymer',
      title: 'Polymère Synthétique & Carburant',
      inputName: 'Résines Mutées (x3)',
      outputName: 'Bidon de Carburant Raffiné (x1)',
      icon: '🧪',
      desc: 'Craquage chimique des condensats toxiques et résines récoltées près des zones aquatiques / stations.',
      inputFamily: 'Bio-chimique & Carburant'
    },
    {
      id: 'refine-chips',
      title: 'Puces de Contrôle Recyclées',
      inputName: 'Circuits Imprimés Brûlés (x2)',
      outputName: 'Module de Piratage IEM (x1)',
      icon: '🪤',
      desc: 'Désoudage et reprogrammation pour fabriquer des modules de capture de Chimères.',
      inputFamily: 'Technologie & Récup’'
    }
  ];

  // Available Crafting Recipes (Établi d'Assemblage)
  const craftingRecipes = [
    {
      id: 'craft-stim',
      name: 'Stimpack Coagulant',
      category: 'medical' as const,
      icon: '💉',
      description: 'Restaure 45 PV en urgence.',
      result: {
        id: `med-${Date.now()}`,
        name: 'Stimpack Coagulant',
        category: 'medical' as const,
        weight: 0.3,
        quantity: 1,
        rarity: 'uncommon' as const,
        icon: '💉',
        healAmount: 45,
        description: 'Soin médical de terrain.'
      },
      cost: [
        { name: 'Ferraille Renforcée', itemId: 'scrap', count: 1 },
        { name: 'Composants Récupérés', itemId: 'comp', count: 1 }
      ]
    },
    {
      id: 'craft-trap',
      name: 'Piège à Impulsion IEM',
      category: 'trap' as const,
      icon: '🪤',
      description: 'Permet de capturer les Chimères mécaniques et bio.',
      result: {
        id: `trap-${Date.now()}`,
        name: 'Piège à Impulsion IEM',
        category: 'trap' as const,
        weight: 1.2,
        quantity: 1,
        rarity: 'rare' as const,
        icon: '🪤',
        description: 'Module de capture électromagnétique.'
      },
      cost: [
        { name: 'Composants Électroniques', itemId: 'elec', count: 2 },
        { name: 'Câblage Cuivre', itemId: 'copper', count: 1 }
      ]
    },
    {
      id: 'craft-turret',
      name: 'Tourelle Automatisée de Défense',
      category: 'weapon' as const,
      icon: '🔫',
      description: 'Défend automatiquement l’abri contre les raids (+40 Défense).',
      result: {
        id: `turret-${Date.now()}`,
        name: 'Tourelle de Défense',
        category: 'weapon' as const,
        weight: 8.0,
        quantity: 1,
        rarity: 'military' as const,
        icon: '🔫',
        description: 'Tourelle de sentinelle automatisée.'
      },
      cost: [
        { name: 'Plaque de Blindage', itemId: 'scrap', count: 3 },
        { name: 'Composants Électroniques', itemId: 'elec', count: 2 }
      ]
    }
  ];

  const handleRefine = (processTitle: string) => {
    soundFx.playHitSound();
    setRefineFeedback(`⚙️ Raffinage réussi : ${processTitle}`);
    setTimeout(() => setRefineFeedback(null), 3000);
    try {
      confetti({ particleCount: 40, spread: 60, origin: { y: 0.6 } });
    } catch {}
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Top Banner */}
      <div className="bg-gradient-to-r from-amber-950/40 via-slate-900 to-slate-950 border border-amber-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-amber-500/20 border border-amber-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(245,158,11,0.3)]">
            🏠
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Bunker & Raffinerie : Terre Zéro</h2>
              <span className="text-[10px] bg-amber-950 text-amber-300 border border-amber-800 px-2 py-0.5 rounded font-mono">
                Ancrage Domicile · Niveau {bunker.level}
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Système de matière en boucle fermée : Récolte IRL $\rightarrow$ Concasseur & Fonderie $\rightarrow$ Établi d’assemblage.
            </p>
          </div>
        </div>

        {/* Energy & Defense Indicators */}
        <div className="flex items-center gap-3">
          <div className="bg-slate-900 border border-slate-800 px-3 py-2 rounded-xl text-center">
            <div className="text-[10px] text-slate-400 font-mono">Énergie Hub</div>
            <div className="text-amber-400 font-tech font-bold text-sm flex items-center justify-center gap-1">
              <Zap className="w-3.5 h-3.5" />
              <span>{bunker.energy} kW</span>
            </div>
          </div>

          <div className="bg-slate-900 border border-slate-800 px-3 py-2 rounded-xl text-center">
            <div className="text-[10px] text-slate-400 font-mono">Défense Abri</div>
            <div className="text-cyan-400 font-tech font-bold text-sm flex items-center justify-center gap-1">
              <Shield className="w-3.5 h-3.5" />
              <span>{bunker.defenses.barricadeHp} PTS</span>
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1.5 bg-slate-900 p-1.5 rounded-2xl border border-slate-800 mb-4 max-w-2xl">
        {[
          { id: 'overview', label: 'Vue Générale', icon: Home },
          { id: 'refinery', label: 'Fonderie & Raffinerie', icon: Flame },
          { id: 'crafting', label: 'Établi d’Assemblage', icon: Hammer },
          { id: 'storage', label: 'Coffre de Stockage', icon: Package },
          { id: 'chimeres', label: 'Sanctum Chimères', icon: Zap }
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

      {/* TAB CONTENT */}
      {/* 1. OVERVIEW */}
      {activeTab === 'overview' && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-amber-400 font-tech font-bold text-sm mb-2">
                <Flame className="w-4 h-4" />
                <span>1. Récolte Contextuelle (IRL)</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed mb-3">
                Chaque bâtiment OSM fournit des matières spécifiques : les rails pour l’acier et le cuivre, les pharmacies pour les composés bio-chimiques.
              </p>
            </div>
            <button onClick={() => setActiveTab('refinery')} className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-amber-400 font-tech font-bold text-xs rounded-xl border border-slate-700">
              Ouvrir la Fonderie →
            </button>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-cyan-400 font-tech font-bold text-sm mb-2">
                <Hammer className="w-4 h-4" />
                <span>2. Établi d’Assemblage</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed mb-3">
                Combinez vos matières raffinées pour fabriquer des modules de capture IEM, stimpacks et tourelles de défense.
              </p>
            </div>
            <button onClick={() => setActiveTab('crafting')} className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-cyan-400 font-tech font-bold text-xs rounded-xl border border-slate-700">
              Ouvrir l’Établi →
            </button>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 text-emerald-400 font-tech font-bold text-sm mb-2">
                <Zap className="w-4 h-4" />
                <span>3. Sanctum des Chimères</span>
              </div>
              <p className="text-xs text-slate-400 leading-relaxed mb-3">
                Assignez vos créatures apprivoisées : la Sentinelle foudre alimente le réseau (+15 kW), le Rôdeur garde l'entrée.
              </p>
            </div>
            <button onClick={() => setActiveTab('chimeres')} className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-emerald-400 font-tech font-bold text-xs rounded-xl border border-slate-700">
              Gérer les Chimères →
            </button>
          </div>
        </div>
      )}

      {/* 2. REFINERY & FOUNDRY */}
      {activeTab === 'refinery' && (
        <div className="space-y-4">
          {refineFeedback && (
            <div className="bg-emerald-950/80 border border-emerald-500/80 text-emerald-300 px-4 py-2.5 rounded-xl font-mono text-xs animate-fadeIn flex items-center justify-between">
              <span>{refineFeedback}</span>
              <Check className="w-4 h-4" />
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {refineryProcesses.map(proc => (
              <div key={proc.id} className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between hover:border-amber-500/50 transition-all shadow-lg">
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-2">
                      <span className="text-2xl">{proc.icon}</span>
                      <div>
                        <h4 className="font-tech text-sm font-bold text-white">{proc.title}</h4>
                        <span className="text-[10px] font-mono text-amber-400">{proc.inputFamily}</span>
                      </div>
                    </div>
                  </div>

                  <p className="text-xs text-slate-400 leading-relaxed mb-3">{proc.desc}</p>

                  <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800 font-mono text-xs flex items-center justify-between mb-3">
                    <span className="text-red-400">{proc.inputName}</span>
                    <span>→</span>
                    <span className="text-emerald-400 font-bold">{proc.outputName}</span>
                  </div>
                </div>

                <button
                  onClick={() => handleRefine(proc.title)}
                  className="w-full py-2.5 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg transition-all active:scale-95 flex items-center justify-center gap-1.5"
                >
                  <Flame className="w-4 h-4" />
                  <span>LANCER LE RAFFINAGE</span>
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 3. CRAFTING */}
      {activeTab === 'crafting' && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {craftingRecipes.map(recipe => (
            <div key={recipe.id} className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between hover:border-cyan-500/50 transition-all shadow-lg">
              <div>
                <div className="flex items-center gap-2.5 mb-2">
                  <span className="text-2xl">{recipe.icon}</span>
                  <div>
                    <h4 className="font-tech text-sm font-bold text-white">{recipe.name}</h4>
                    <span className="text-[10px] font-mono text-cyan-400 uppercase">{recipe.category}</span>
                  </div>
                </div>

                <p className="text-xs text-slate-400 leading-relaxed mb-3">{recipe.description}</p>

                <div className="space-y-1 mb-3">
                  <div className="text-[10px] font-mono text-slate-400 uppercase">Matériaux requis :</div>
                  {recipe.cost.map((c, i) => (
                    <div key={i} className="text-xs font-mono bg-slate-950 px-2 py-1 rounded border border-slate-800 flex justify-between text-slate-300">
                      <span>{c.name}</span>
                      <span className="text-amber-400 font-bold">x{c.count}</span>
                    </div>
                  ))}
                </div>
              </div>

              <button
                onClick={() => {
                  onCraftItem(recipe.id, recipe.result, recipe.cost);
                  soundFx.playLootPickup();
                  try {
                    confetti({ particleCount: 35, spread: 50, origin: { y: 0.6 } });
                  } catch {}
                }}
                className="w-full py-2.5 bg-gradient-to-r from-cyan-600 to-blue-600 hover:from-cyan-500 hover:to-blue-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg transition-all active:scale-95 flex items-center justify-center gap-1.5"
              >
                <Hammer className="w-4 h-4" />
                <span>ASSEMBLER</span>
              </button>
            </div>
          ))}
        </div>
      )}

      {/* 4. STORAGE */}
      {activeTab === 'storage' && (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="font-tech text-sm font-bold text-white uppercase">Coffre de Réserve du Bunker</h3>
            <span className="text-xs font-mono text-slate-400">{bunker.chestStorage.length} / 40 Objets</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-6 gap-2.5">
            {bunker.chestStorage.length === 0 ? (
              <div className="col-span-full py-12 text-center text-slate-500 font-mono text-xs">
                Coffre vide. Déposez vos surplus de marche pour libérer du poids d’inventaire.
              </div>
            ) : (
              bunker.chestStorage.map((item: Item) => (
                <div key={item.id} className="bg-slate-950 border border-slate-800 rounded-xl p-3 flex flex-col items-center justify-between text-center group">
                  <span className="text-2xl mb-1">{item.icon}</span>
                  <div className="font-tech text-xs text-white font-bold truncate w-full">{item.name}</div>
                  <span className="text-[10px] font-mono text-slate-400">x{item.quantity} · {item.weight} kg</span>
                  <button
                    onClick={() => onWithdrawFromChest(item)}
                    className="mt-2 w-full py-1 bg-slate-800 hover:bg-slate-700 text-slate-200 text-[10px] font-mono rounded border border-slate-700"
                  >
                    Retirer
                  </button>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* 5. CHIMERES SANCTUM */}
      {activeTab === 'chimeres' && (
        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {player.chimeres.map(chimere => (
              <div key={chimere.id} className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between shadow-lg">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2.5">
                    <span className="text-3xl">{chimere.avatarIcon}</span>
                    <div>
                      <h4 className="font-tech text-sm font-bold text-white uppercase">{chimere.name}</h4>
                      <span className="text-[10px] font-mono text-amber-400">Niveau {chimere.level} · {chimere.type === 'mechanical' ? 'Mécanique' : 'Bio-Mutée'}</span>
                    </div>
                  </div>
                  <span className="text-xs bg-slate-950 text-cyan-400 border border-slate-800 px-2 py-0.5 rounded font-mono">
                    Rôle : {chimere.role}
                  </span>
                </div>

                <div className="grid grid-cols-4 gap-1.5 mt-2">
                  {[
                    { role: 'combat', label: 'Garde', icon: '🛡️' },
                    { role: 'energy', label: '+15kW', icon: '⚡' },
                    { role: 'tracking', label: 'Radar', icon: '📡' },
                    { role: 'transport', label: '+10kg', icon: '🎒' }
                  ].map(r => (
                    <button
                      key={r.role}
                      onClick={() => onAssignChimereRole(chimere.id, r.role as ChimereRole)}
                      className={`py-1.5 rounded-lg text-[10px] font-mono font-bold flex flex-col items-center transition-all ${
                        chimere.role === r.role ? 'bg-amber-500 text-slate-950' : 'bg-slate-950 text-slate-400 hover:text-white border border-slate-800'
                      }`}
                    >
                      <span>{r.icon}</span>
                      <span>{r.label}</span>
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
