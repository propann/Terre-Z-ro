import React from 'react';
import { X, BookOpen, Map, Zap, Layers, Compass, Shield, Box, Sparkles } from 'lucide-react';

interface QuickHelpModalProps {
  onClose: () => void;
}

export const QuickHelpModal: React.FC<QuickHelpModalProps> = ({ onClose }) => {
  return (
    <div className="fixed inset-0 z-50 bg-slate-950/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="relative w-full max-w-2xl bg-slate-900 border-2 border-cyan-500/40 rounded-3xl p-6 shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
          <div className="flex items-center gap-2">
            <BookOpen className="w-5 h-5 text-cyan-400" />
            <h3 className="font-tech text-base uppercase font-bold text-white tracking-wider">
              Architecture & Manuel du Prototype Chimères
            </h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Body */}
        <div className="flex-1 overflow-y-auto space-y-4 pr-1 text-xs text-slate-300 leading-relaxed font-sans">
          {/* Section 1 */}
          <div className="bg-slate-950 p-3.5 rounded-2xl border border-slate-800">
            <h4 className="font-tech font-bold text-amber-400 text-sm mb-1.5 flex items-center gap-2">
              <Map className="w-4 h-4" /> 1. La Stack Technique & Données OSM
            </h4>
            <p className="text-slate-400">
              Le monde est généré côté client à partir des polygones réels d'OpenStreetMap (Overpass API). Les routes deviennent des voies dégagées ou crevassées, les bâtiments sont extrudés selon leur hauteur (<code className="text-cyan-300">building:levels</code>) et les tags (<code className="text-cyan-300">amenity</code>, <code className="text-cyan-300">shop</code>) déterminent les tables de loot et l'apparition des Chimères.
            </p>
          </div>

          {/* Section 2 */}
          <div className="bg-slate-950 p-3.5 rounded-2xl border border-slate-800">
            <h4 className="font-tech font-bold text-cyan-400 text-sm mb-1.5 flex items-center gap-2">
              <Layers className="w-4 h-4" /> 2. Découpage Spatial Uber H3
            </h4>
            <p className="text-slate-400">
              La terre est découpée en cellules hexagonales hiérarchisées (Résolution 9 ~100m). Chaque tuile gère le brouillard de guerre, les niveaux de radiation (Geiger) et la densité locale d'ennemis.
            </p>
          </div>

          {/* Section 3 */}
          <div className="bg-slate-950 p-3.5 rounded-2xl border border-slate-800">
            <h4 className="font-tech font-bold text-emerald-400 text-sm mb-1.5 flex items-center gap-2">
              <Compass className="w-4 h-4" /> 3. La Boucle de Gameplay (Core Loop)
            </h4>
            <ul className="list-disc list-inside space-y-1 text-slate-400">
              <li><strong className="text-white">Phase Nomade (Dehors) :</strong> Déplacement GPS réel, radar de proximité 50m, notifications haptiques, extraction de loot en 1-tap à moins de 20m, inventaire limité en poids.</li>
              <li><strong className="text-white">Phase Sédentaire (Bunker) :</strong> Déchargement dans les coffres, craft à l'établi, gestion et entraînement des Chimères capturées.</li>
            </ul>
          </div>

          {/* Section 4 */}
          <div className="bg-slate-950 p-3.5 rounded-2xl border border-slate-800">
            <h4 className="font-tech font-bold text-purple-400 text-sm mb-1.5 flex items-center gap-2">
              <Sparkles className="w-4 h-4" /> 4. Les Chimères Post-Apocalyptiques
            </h4>
            <p className="text-slate-400">
              <strong>Chimères Mécaniques :</strong> Drones industriels réactivés sauvagement (zones commerciales, voies ferrées, usines).<br />
              <strong>Chimères Bio-mutées :</strong> Faune locale altérée (parcs, forêts, plans d'eau).<br />
              Rôles au bunker : Défense contre les raids, Génération d'énergie, Pistage radar +25m, Mule de transport +10kg.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
