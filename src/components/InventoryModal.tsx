import React from 'react';
import { PlayerState, Item } from '../types/game';
import { soundFx } from '../services/soundFx';
import {
  Package,
  X,
  Heart,
  Radio,
  Trash2,
  Sparkles,
  Zap,
  AlertTriangle
} from 'lucide-react';

interface InventoryModalProps {
  player: PlayerState;
  onClose: () => void;
  onUseItem: (item: Item) => void;
  onDropItem: (item: Item) => void;
}

export const InventoryModal: React.FC<InventoryModalProps> = ({
  player,
  onClose,
  onUseItem,
  onDropItem
}) => {
  const isOverweight = player.currentWeight > player.maxWeight;

  return (
    <div className="fixed inset-0 z-50 bg-slate-950/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="relative w-full max-w-lg bg-slate-900 border-2 border-cyan-500/40 rounded-3xl p-5 shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-3">
          <div className="flex items-center gap-2">
            <Package className="w-5 h-5 text-cyan-400" />
            <h3 className="font-tech text-base uppercase font-bold text-white tracking-wider">
              Inventaire Sac à Dos
            </h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Weight Status */}
        <div className={`p-3 rounded-2xl border mb-3 flex items-center justify-between ${
          isOverweight ? 'bg-red-950/60 border-red-500/50' : 'bg-slate-950 border-slate-800'
        }`}>
          <div>
            <div className="text-xs font-mono text-slate-400">Poids Total Transporté :</div>
            <div className={`text-sm font-bold font-mono ${isOverweight ? 'text-red-400' : 'text-cyan-400'}`}>
              {player.currentWeight.toFixed(1)} / {player.maxWeight} kg
            </div>
          </div>
          {isOverweight && (
            <div className="flex items-center gap-1.5 text-xs text-red-400 font-mono">
              <AlertTriangle className="w-4 h-4 animate-bounce" />
              <span>Surcharge ! Vitesse réduite</span>
            </div>
          )}
        </div>

        {/* Item List */}
        <div className="flex-1 overflow-y-auto space-y-2 pr-1">
          {player.inventory.length === 0 && (
            <div className="text-center py-12 text-slate-500 font-mono text-xs">
              Votre sac à dos est vide. Explorez les bâtiments en mode Nomade pour récolter du loot.
            </div>
          )}

          {player.inventory.map(item => (
            <div
              key={item.id}
              className="bg-slate-950/90 border border-slate-800 hover:border-cyan-500/40 rounded-xl p-3 flex items-center justify-between transition-all"
            >
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-slate-900 border border-slate-700 flex items-center justify-center text-xl">
                  {item.icon}
                </div>
                <div>
                  <div className="text-xs font-bold text-white flex items-center gap-2">
                    <span>{item.name}</span>
                    <span className="text-[10px] bg-slate-800 text-cyan-400 px-1.5 py-0.2 rounded font-mono">
                      x{item.quantity}
                    </span>
                  </div>
                  <div className="text-[11px] text-slate-400">{item.description}</div>
                  <div className="text-[10px] text-slate-500 font-mono">
                    {(item.weight * item.quantity).toFixed(1)} kg ({item.weight} kg/u) · {item.category}
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-1.5">
                {(item.healAmount || item.radCleanse) && (
                  <button
                    onClick={() => {
                      soundFx.playLootPickup();
                      onUseItem(item);
                    }}
                    className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white font-tech font-bold text-xs rounded-lg shadow transition-all"
                  >
                    Utiliser
                  </button>
                )}
                <button
                  onClick={() => onDropItem(item)}
                  className="p-1.5 bg-slate-800 hover:bg-red-950 text-slate-400 hover:text-red-400 rounded-lg border border-slate-700 transition-colors"
                  title="Jeter l'objet"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
