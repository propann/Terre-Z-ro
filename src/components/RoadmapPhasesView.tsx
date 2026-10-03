import React, { useState } from 'react';
import {
  Layers,
  Smartphone,
  Sword,
  Home,
  Users,
  Truck,
  CheckCircle2,
  Clock,
  ExternalLink,
  ChevronDown,
  ChevronUp,
  Terminal,
  Play,
  FileText,
  Shield,
  Radio,
  Cpu,
  Database,
  Globe,
  Flame,
  Hammer
} from 'lucide-react';

export const RoadmapPhasesView: React.FC = () => {
  const [activeSection, setActiveSection] = useState<number>(1);

  const gddSections = [
    {
      id: 1,
      title: '1. Pitch & Vision Globale',
      icon: Globe,
      badge: 'Game Design',
      content: (
        <div className="space-y-3 text-xs leading-relaxed text-slate-300">
          <p>
            <strong>Terre Zéro</strong> est un jeu mobile de survie post-apocalyptique multijoueur synchrone/asynchrone basé sur la géolocalisation réelle.
            Le monde réel est transcrit à la volée en un univers micro-voxel (1 voxel = 20 cm) destructible et constructible, généré de façon déterministe à partir d'OpenStreetMap.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-2 pt-2">
            <div className="bg-slate-900 border border-slate-800 p-3 rounded-xl">
              <div className="font-tech text-amber-400 font-bold mb-1">🧭 1. Exploration Nomade (IRL)</div>
              <p className="text-[11px] text-slate-400">Marche réelle avec rayon de 25m, radar haptique, fouille 1-tap de POIs et capture de chimères.</p>
            </div>
            <div className="bg-slate-900 border border-slate-800 p-3 rounded-xl">
              <div className="font-tech text-red-400 font-bold mb-1">⛏️ 2. Survie & Forage Voxel</div>
              <p className="text-[11px] text-slate-400">Extraction chirurgicale dans le décor (creuser un mur pour du cuivre, percer un blindage de banque).</p>
            </div>
            <div className="bg-slate-900 border border-slate-800 p-3 rounded-xl">
              <div className="font-tech text-cyan-400 font-bold mb-1">🏠 3. Sédentarisation (Bunker)</div>
              <p className="text-[11px] text-slate-400">Ancrage domicile, construction libre, raffinerie de métaux et gestion d'escouade de chimères.</p>
            </div>
          </div>
        </div>
      )
    },
    {
      id: 2,
      title: '2. Direction Artistique & Rendu (Lo-Fi Cyber-Scrap)',
      icon: Layers,
      badge: 'Graphismes & Shaders',
      content: (
        <div className="space-y-2 text-xs leading-relaxed text-slate-300">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
            <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800 font-mono">
              <div className="text-slate-400 text-[10px]">Résolution Grille</div>
              <div className="text-amber-400 font-bold text-sm">1 voxel = 20 cm</div>
            </div>
            <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800 font-mono">
              <div className="text-slate-400 text-[10px]">Épaisseur Mur</div>
              <div className="text-white font-bold text-sm">1 à 2 voxels (20-40cm)</div>
            </div>
            <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800 font-mono">
              <div className="text-slate-400 text-[10px]">Hauteur d'un Étage</div>
              <div className="text-cyan-400 font-bold text-sm">14 à 15 voxels (~3m)</div>
            </div>
            <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800 font-mono">
              <div className="text-slate-400 text-[10px]">Palette Couleur</div>
              <div className="text-emerald-400 font-bold text-sm">Atlas 256×256 (Nearest)</div>
            </div>
          </div>
          <ul className="list-disc list-inside space-y-1 text-slate-400 pt-1 text-[11px]">
            <li><strong>Vertex Ambient Occlusion (AO) :</strong> Précalculée au sommet sans coût GPU pour ombrer les recoins.</li>
            <li><strong>Post-processing :</strong> Ombres portées nettes, brouillard volumétrique au sol et bloom sur néons / chimères.</li>
          </ul>
        </div>
      )
    },
    {
      id: 3,
      title: '3. Architecture Globale : Hybride Client Déterministe + Serveur Delta',
      icon: Cpu,
      badge: 'Architecture Système',
      content: (
        <div className="space-y-3 text-xs leading-relaxed text-slate-300">
          <div className="bg-slate-900 border border-slate-800 p-3 rounded-xl font-mono text-[11px] text-slate-300 leading-relaxed overflow-x-auto">
            [ Données OpenStreetMap (.pbf / Overpass) ]<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;│<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;▼<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[ Backend Carto / PostGIS 16 ] (Indexation Uber H3 Résolution 9 ~100m)<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;│<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;▼<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[ API Client / Protobuf ou JSON ]<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;│<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;┌─────────┴─────────┐<br />
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;▼&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;▼<br />
            [ Client Mobile Godot 4 C# ]&nbsp;&nbsp;&nbsp;&nbsp;[ Serveur Réseau Nakama / Go ]<br />
            &nbsp;&nbsp;├─ Rasterisation Voxel 2.5D&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;├─ Vérification Anti-spoof GPS<br />
            &nbsp;&nbsp;├─ Greedy Meshing multithread&nbsp;&nbsp;&nbsp;&nbsp;├─ Stockage des Deltas (trous/blocs)<br />
            &nbsp;&nbsp;├─ Pont GPS + Filtre Kalman&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;└─ Broadcast temps réel (cellule H3)<br />
            &nbsp;&nbsp;└─ Shader & Collisions
          </div>
        </div>
      )
    },
    {
      id: 4,
      title: '4. Pipeline Procédural OSM vers Voxel',
      icon: Terminal,
      badge: 'Génération Voxel',
      content: (
        <div className="space-y-2 text-xs leading-relaxed text-slate-300">
          <p>Le monde brut n'est jamais stocké en voxel sur le serveur (économie de bande passante) :</p>
          <div className="space-y-1.5 font-mono text-[11px]">
            <div className="bg-slate-900 p-2 rounded-lg border border-slate-800">
              <strong className="text-amber-400">1. Routes & Trottoirs :</strong> Bitume posé à Y=0. Trottoirs extrudés à Y=+2 voxels (40 cm) le long des îlots.
            </div>
            <div className="bg-slate-900 p-2 rounded-lg border border-slate-800">
              <strong className="text-cyan-400">2. Bâtiments :</strong> Scanline polygonale + extrusion selon les tags `building:levels` ou `height`.
            </div>
            <div className="bg-slate-900 p-2 rounded-lg border border-slate-800">
              <strong className="text-emerald-400">3. Intérieurs & Caches :</strong> Fonction déterministe avec l'OSM ID comme seed générant cloisons et câbles de cuivre dans les murs.
            </div>
          </div>
        </div>
      )
    },
    {
      id: 5,
      title: '5. Moteur de Rendu : Chunks 32³ & Greedy Meshing',
      icon: Layers,
      badge: 'Performance Mobile',
      content: (
        <div className="space-y-2 text-xs leading-relaxed text-slate-300">
          <div className="grid grid-cols-2 gap-2">
            <div className="bg-slate-900 p-3 rounded-xl border border-slate-800">
              <div className="text-slate-400 text-[10px] font-mono">Format Chunk</div>
              <div className="text-white font-bold text-sm">32×32×32 voxels (6,4 m³)</div>
            </div>
            <div className="bg-slate-900 p-3 rounded-xl border border-slate-800">
              <div className="text-slate-400 text-[10px] font-mono">Encodage Voxel</div>
              <div className="text-amber-400 font-bold text-sm">16 bits (ushort) compact</div>
            </div>
          </div>
          <p className="text-[11px] text-slate-400">
            <strong>Greedy Meshing multithread :</strong> Culling des faces internes + fusion des quads coplanaires. Réduction de 85% à 98% des polygones GPU. Recalcul sub-16ms lors du minage.
          </p>
        </div>
      )
    },
    {
      id: 6,
      title: '6. Gameplay : Exploration Nomade, Sonar X-Ray & Bunker',
      icon: Radio,
      badge: 'Game Loop',
      content: (
        <div className="space-y-2 text-xs leading-relaxed text-slate-300">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            <div className="bg-slate-900 p-3 rounded-xl border border-slate-800">
              <div className="font-tech text-amber-400 font-bold mb-1">📡 Boucle Nomade & GPS 25m</div>
              <ul className="text-[11px] space-y-1 text-slate-400">
                <li>• Filtre de Kalman EKF (lissage des sauts 5-15m).</li>
                <li>• Bounding bubble 25m : interaction sécurisée sans intrusion.</li>
                <li>• Scanner Sonar X-Ray : repère le cuivre et caisses médicales.</li>
                <li>• Chasse & puces de piratage pour capturer les chimères.</li>
              </ul>
            </div>
            <div className="bg-slate-900 p-3 rounded-xl border border-slate-800">
              <div className="font-tech text-cyan-400 font-bold mb-1">🏠 Boucle Sédentaire (Bunker)</div>
              <ul className="text-[11px] space-y-1 text-slate-400">
                <li>• Ancrage GPS de la résidence réelle du joueur.</li>
                <li>• Construction libre : sas blindés, tourelles, couveuse.</li>
                <li>• Raffinerie : débris bruts transformés en lingots & circuits.</li>
                <li>• Rôles chimères : Batterie +15kW, Mule +10kg, Radar +25m.</li>
              </ul>
            </div>
          </div>
        </div>
      )
    },
    {
      id: 7,
      title: '7. Multijoueur & Format Delta RLE',
      icon: Users,
      badge: 'Réseau & Anti-Cheat',
      content: (
        <div className="space-y-2 text-xs leading-relaxed text-slate-300">
          <div className="bg-slate-900 border border-slate-800 p-3 rounded-xl font-mono text-[11px]">
            <div className="text-slate-400 mb-1">// Événement Delta Diffusé :</div>
            <div className="text-emerald-400">
              &#123;<br />
              &nbsp;&nbsp;"h3_index": "891fb466257ffff",<br />
              &nbsp;&nbsp;"chunk_coords": [12, 4, -3],<br />
              &nbsp;&nbsp;"local_voxel": [14, 2, 31],<br />
              &nbsp;&nbsp;"action": "DESTROY",<br />
              &nbsp;&nbsp;"player_id": "usr_9981a"<br />
              &#125;
            </div>
          </div>
          <p className="text-[11px] text-slate-400">
            <strong>Anti-Cheat :</strong> Vitesse &gt; 30 km/h rejetée, détection Mock Location GPS, validation de la portée &lt; 25m côté serveur.
          </p>
        </div>
      )
    },
    {
      id: 8,
      title: '8. Stack Logicielle Définitive',
      icon: Database,
      badge: 'Technologies',
      content: (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-xs font-mono">
          <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800">
            <div className="text-slate-400 text-[10px]">Client Mobile</div>
            <div className="text-amber-400 font-bold">Godot 4.3 .NET C#</div>
          </div>
          <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800">
            <div className="text-slate-400 text-[10px]">Serveur Réseau</div>
            <div className="text-cyan-400 font-bold">Nakama / Go</div>
          </div>
          <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800">
            <div className="text-slate-400 text-[10px]">Base Données</div>
            <div className="text-emerald-400 font-bold">PostgreSQL 16 PostGIS</div>
          </div>
          <div className="bg-slate-900 p-2.5 rounded-xl border border-slate-800">
            <div className="text-slate-400 text-[10px]">Indexation Spatiale</div>
            <div className="text-purple-400 font-bold">Uber H3 (Res 9)</div>
          </div>
        </div>
      )
    }
  ];

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Header */}
      <div className="bg-gradient-to-r from-orange-950/40 via-slate-900 to-slate-950 border border-orange-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-orange-500/20 border border-orange-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(249,115,22,0.3)]">
            📋
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Cahier des Charges & GDD : Projet Terre Zéro</h2>
              <span className="text-[10px] bg-orange-950 text-orange-300 border border-orange-800 px-2 py-0.5 rounded font-mono">
                Godot 4 · Micro-Voxel 20cm · Uber H3
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Spécifications techniques, pipeline de conversion procédural et architecture multi-sprints.
            </p>
          </div>
        </div>
      </div>

      {/* Accordion Sections */}
      <div className="space-y-3 max-w-5xl mx-auto w-full pb-8">
        {gddSections.map((sec) => {
          const Icon = sec.icon;
          const isExpanded = activeSection === sec.id;

          return (
            <div
              key={sec.id}
              className={`bg-slate-900/90 border transition-all rounded-2xl overflow-hidden shadow-lg ${
                isExpanded ? 'border-orange-500/60 shadow-[0_0_20px_rgba(249,115,22,0.15)]' : 'border-slate-800 hover:border-slate-700'
              }`}
            >
              <button
                onClick={() => setActiveSection(isExpanded ? 0 : sec.id)}
                className="w-full p-4 flex items-center justify-between text-left transition-colors hover:bg-slate-800/40"
              >
                <div className="flex items-center gap-3">
                  <div className={`p-2.5 rounded-xl border ${isExpanded ? 'bg-orange-500 text-slate-950 border-orange-400' : 'bg-slate-800 text-slate-400 border-slate-700'}`}>
                    <Icon className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="font-tech font-bold text-sm text-white">{sec.title}</h3>
                    <span className="text-[10px] font-mono text-orange-400/80">{sec.badge}</span>
                  </div>
                </div>

                <div className="text-slate-400">
                  {isExpanded ? <ChevronUp className="w-5 h-5 text-orange-400" /> : <ChevronDown className="w-5 h-5" />}
                </div>
              </button>

              {isExpanded && (
                <div className="p-4 pt-1 border-t border-slate-800/60 bg-slate-950/40 animate-fadeIn">
                  {sec.content}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
