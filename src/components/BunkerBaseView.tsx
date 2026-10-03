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
  Flame
} from 'lucide-react';

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
  const [activeTab, setActiveTab] = useState<'overview' | 'storage' | 'crafting' | 'chimeres'>('overview');

  // Available Crafting Recipes
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
        weight: 1.0,
        quantity: 1,
        rarity: 'rare' as const,
        icon: '🪤',
        description: 'Piège de capture électromagnétique.'
      },
      cost: [
        { name: 'Piles Haute Densité', itemId: 'elec', count: 1 },
        { name: 'Ferraille Renforcée', itemId: 'scrap', count: 2 }
      ]
    },
    {
      id: 'craft-bag',
      name: 'Extension Sac à Dos Tactique (+5kg)',
      category: 'module' as const,
      icon: '🎒',
      description: 'Augmente la capacité d’emport de votre sac.',
      result: {
        id: `bag-${Date.now()}`,
        name: 'Extension Sac à Dos Tactique',
        category: 'module' as const,
        weight: 0.5,
        quantity: 1,
        rarity: 'military' as const,
        icon: '🎒',
        description: 'Poches molletonnées haute résistance.'
      },
      cost: [
        { name: 'Ferraille Renforcée', itemId: 'scrap', count: 3 },
        { name: 'Plaques de Blindage', itemId: 'wpn', count: 1 }
      ]
    }
  ];

  const handleCraft = (recipe: typeof craftingRecipes[0]) => {
    soundFx.playLootPickup();
    onCraftItem(recipe.id, recipe.result, recipe.cost);
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Header Bunker Banner */}
      <div className="bg-gradient-to-r from-amber-950/40 via-slate-900 to-slate-950 border border-amber-500/30 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-amber-500/20 border border-amber-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(245,158,11,0.2)]">
            🏛️
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">{bunker.name}</h2>
              <span className="text-[10px] bg-amber-950 text-amber-300 border border-amber-800 px-2 py-0.5 rounded font-mono">
                Niveau {bunker.level}
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Base Sédentaire · Coordonnées sécurisées · Générateur {bunker.energy}/{bunker.maxEnergy} kW/h
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <div className="text-right hidden sm:block">
            <div className="text-xs font-mono text-emerald-400 font-bold">Zone Sécurisée</div>
            <div className="text-[11px] text-slate-400">0% Radiations</div>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-800 pb-3 mb-4 overflow-x-auto">
        <button
          onClick={() => setActiveTab('overview')}
          className={`px-4 py-2 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            activeTab === 'overview'
              ? 'bg-amber-500 text-slate-950 shadow-md'
              : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          <Home className="w-4 h-4" />
          Vue d'Ensemble
        </button>

        <button
          onClick={() => setActiveTab('storage')}
          className={`px-4 py-2 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            activeTab === 'storage'
              ? 'bg-amber-500 text-slate-950 shadow-md'
              : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          <Package className="w-4 h-4" />
          Coffre & Déchargement ({bunker.chestStorage.length})
        </button>

        <button
          onClick={() => setActiveTab('crafting')}
          className={`px-4 py-2 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            activeTab === 'crafting'
              ? 'bg-amber-500 text-slate-950 shadow-md'
              : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          <Hammer className="w-4 h-4" />
          Atelier d'Établi
        </button>

        <button
          onClick={() => setActiveTab('chimeres')}
          className={`px-4 py-2 rounded-xl text-xs font-tech font-bold uppercase tracking-wider flex items-center gap-1.5 transition-all ${
            activeTab === 'chimeres'
              ? 'bg-amber-500 text-slate-950 shadow-md'
              : 'bg-slate-900 text-slate-400 hover:text-white'
          }`}
        >
          <Sparkles className="w-4 h-4" />
          Sanctum des Chimères ({player.chimeres.length})
        </button>
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-y-auto">
        {/* OVERVIEW TAB */}
        {activeTab === 'overview' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Energy Core */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <BatteryCharging className="w-5 h-5 text-amber-400" />
                  <h3 className="text-sm font-bold font-tech text-white uppercase">Réseau Énergétique</h3>
                </div>
                <span className="text-xs font-mono text-amber-400 font-bold">
                  +{player.chimeres.filter(c => c.role === 'energy').length * 15} kW (Chimères)
                </span>
              </div>
              <div className="w-full bg-slate-950 h-3 rounded-full overflow-hidden mb-2">
                <div
                  className="bg-amber-500 h-full rounded-full transition-all"
                  style={{ width: `${(bunker.energy / bunker.maxEnergy) * 100}%` }}
                />
              </div>
              <p className="text-xs text-slate-400">
                Alimente le filtre à air, l'établi et le bouclier défensif. Affectez des Chimères mécaniques pour surcharger la production.
              </p>
            </div>

            {/* Defenses */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <Shield className="w-5 h-5 text-cyan-400" />
                  <h3 className="text-sm font-bold font-tech text-white uppercase">Système Défensif</h3>
                </div>
                <span className="text-xs font-mono text-cyan-400 font-bold">
                  {bunker.defenses.turretCount} Tourelles actives
                </span>
              </div>
              <p className="text-xs text-slate-400 mb-3">
                Barricades en titane : <strong>{bunker.defenses.barricadeHp} / 500 PV</strong>. Protège vos réserves contre les attaques de mutants sauvages.
              </p>
              <button
                onClick={() => onUpgradeBunkerFacility('defenses')}
                className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-cyan-300 font-tech font-bold text-xs rounded-xl border border-slate-700 transition-colors"
              >
                + Renforcer les Barricades & Tourelles
              </button>
            </div>

            {/* Voxel Base Expansion */}
            <div className="md:col-span-2 bg-gradient-to-r from-slate-900 to-slate-950 border border-slate-800 rounded-2xl p-4 flex items-center justify-between">
              <div>
                <h4 className="text-sm font-bold text-white font-tech">Extension Modulaire Voxel du Bunker</h4>
                <p className="text-xs text-slate-400 mt-1">
                  Améliorez l'établi et débloquez de nouvelles pièces (Laboratoire Bio, Baie de Réparation Robotique).
                </p>
              </div>
              <button
                onClick={() => onUpgradeBunkerFacility('core')}
                className="px-4 py-2.5 bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs rounded-xl font-tech shadow-lg"
              >
                Améliorer (Niv. {bunker.level + 1})
              </button>
            </div>
          </div>
        )}

        {/* STORAGE TAB */}
        {activeTab === 'storage' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Player Backpack */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4 flex flex-col">
              <div className="flex items-center justify-between mb-3 border-b border-slate-800 pb-2">
                <span className="text-xs font-tech font-bold uppercase text-cyan-400">
                  🎒 Sac à dos ({player.currentWeight.toFixed(1)} / {player.maxWeight} kg)
                </span>
                <span className="text-[11px] text-slate-400">{player.inventory.length} objets</span>
              </div>

              <div className="flex-1 overflow-y-auto space-y-2 max-h-72">
                {player.inventory.length === 0 && (
                  <div className="text-center py-8 text-xs text-slate-500 font-mono">Sac vide.</div>
                )}
                {player.inventory.map(item => (
                  <div
                    key={item.id}
                    className="bg-slate-950 border border-slate-800 rounded-xl p-2.5 flex items-center justify-between"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{item.icon}</span>
                      <div>
                        <div className="text-xs font-bold text-white">{item.name} x{item.quantity}</div>
                        <div className="text-[10px] text-slate-400">{(item.weight * item.quantity).toFixed(1)} kg</div>
                      </div>
                    </div>
                    <button
                      onClick={() => onDepositToChest(item)}
                      className="px-2.5 py-1 bg-amber-500/20 hover:bg-amber-500 text-amber-300 hover:text-slate-950 text-xs font-tech font-bold rounded-lg border border-amber-500/40 transition-all"
                    >
                      Déposer ⬇️
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {/* Bunker Safe Chest */}
            <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4 flex flex-col">
              <div className="flex items-center justify-between mb-3 border-b border-slate-800 pb-2">
                <span className="text-xs font-tech font-bold uppercase text-amber-400">
                  🏛️ Coffre Sécurisé du Bunker ({bunker.chestStorage.length} objets)
                </span>
              </div>

              <div className="flex-1 overflow-y-auto space-y-2 max-h-72">
                {bunker.chestStorage.length === 0 && (
                  <div className="text-center py-8 text-xs text-slate-500 font-mono">Coffre vide. Déposez vos ressources pour alléger votre sac.</div>
                )}
                {bunker.chestStorage.map(item => (
                  <div
                    key={item.id}
                    className="bg-slate-950 border border-slate-800 rounded-xl p-2.5 flex items-center justify-between"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{item.icon}</span>
                      <div>
                        <div className="text-xs font-bold text-white">{item.name} x{item.quantity}</div>
                        <div className="text-[10px] text-slate-400">{item.description}</div>
                      </div>
                    </div>
                    <button
                      onClick={() => onWithdrawFromChest(item)}
                      className="px-2.5 py-1 bg-cyan-500/20 hover:bg-cyan-500 text-cyan-300 hover:text-slate-950 text-xs font-tech font-bold rounded-lg border border-cyan-500/40 transition-all"
                    >
                      Prendre ⬆️
                    </button>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* CRAFTING TAB */}
        {activeTab === 'crafting' && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            {craftingRecipes.map(recipe => (
              <div
                key={recipe.id}
                className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center gap-2.5 mb-2">
                    <span className="text-2xl">{recipe.icon}</span>
                    <div>
                      <h4 className="text-xs font-bold text-white font-tech">{recipe.name}</h4>
                      <span className="text-[10px] text-amber-400 font-mono">Atelier Niv. {bunker.workbenchLevel}</span>
                    </div>
                  </div>
                  <p className="text-xs text-slate-400 mb-3">{recipe.description}</p>
                </div>

                <div className="border-t border-slate-800 pt-3">
                  <div className="text-[11px] font-mono text-slate-400 mb-2">
                    Coût : {recipe.cost.map(c => `${c.name} x${c.count}`).join(', ')}
                  </div>
                  <button
                    onClick={() => handleCraft(recipe)}
                    className="w-full py-2 bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold font-tech text-xs rounded-xl shadow transition-all flex items-center justify-center gap-1.5"
                  >
                    <Hammer className="w-3.5 h-3.5" />
                    <span>Fabriquer</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* CHIMERES SANCTUM TAB */}
        {activeTab === 'chimeres' && (
          <div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
              {player.chimeres.length === 0 && (
                <div className="col-span-full text-center py-12 text-xs text-slate-500 font-mono">
                  Aucune Chimère capturée pour l'instant. Sortez en mode Nomade avec des pièges pour en capturer !
                </div>
              )}

              {player.chimeres.map(chim => (
                <div
                  key={chim.id}
                  className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between"
                >
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <span className="text-2xl">{chim.avatarIcon}</span>
                        <div>
                          <h4 className="text-xs font-bold text-white">{chim.name}</h4>
                          <span className="text-[10px] text-slate-400 font-mono">
                            {chim.type === 'mechanical' ? '⚙️ Mécanique' : '🧬 Bio-mutée'} · ATK {chim.attack}
                          </span>
                        </div>
                      </div>
                      <span className="text-xs font-mono font-bold text-amber-400">Niv. {chim.level}</span>
                    </div>
                    <p className="text-[11px] text-slate-400 italic mb-3">{chim.lore}</p>
                  </div>

                  <div className="border-t border-slate-800 pt-2">
                    <label className="text-[10px] font-mono text-slate-400 block mb-1">Rôle Assigné au Bunker :</label>
                    <select
                      value={chim.role}
                      onChange={(e) => onAssignChimereRole(chim.id, e.target.value as ChimereRole)}
                      className="w-full bg-slate-950 border border-slate-700 text-xs text-slate-200 rounded-lg p-2 font-mono"
                    >
                      <option value="combat">🛡️ Défense du Bunker (Combat)</option>
                      <option value="energy">⚡ Génération d'Énergie (+15 kW)</option>
                      <option value="tracking">📡 Pistage de Ressources (+25m)</option>
                      <option value="transport">🎒 Mule Cargo (+10 kg au sac)</option>
                      <option value="idle">💤 Repos</option>
                    </select>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
