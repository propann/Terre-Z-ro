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
  Play
} from 'lucide-react';

export const RoadmapPhasesView: React.FC = () => {
  const [expandedPhase, setExpandedPhase] = useState<number>(1);

  const phases = [
    {
      number: 1,
      title: 'Socle géospatial et pipeline de données',
      timeline: 'Semaines 1 à 3',
      icon: Layers,
      color: 'text-cyan-400',
      badgeBg: 'bg-cyan-950 border-cyan-500 text-cyan-300',
      goal: "Extraire le monde réel et le transformer en données consommables par le moteur de jeu.",
      missions: [
        {
          id: 'M1.1',
          role: 'Backend / Data',
          desc: "Monter une base PostgreSQL 16 + PostGIS. Configurer l'import d'extraits régionaux OpenStreetMap via osm2pgsql ou imposm3.",
          deliverable: "Serveur PostGIS local opérationnel avec schémas routes/bâtiments.",
          status: 'ready'
        },
        {
          id: 'M1.2',
          role: 'Backend / Data',
          desc: "Intégrer la bibliothèque spatiale Uber H3 (résolution 9 ou 10, mailles de 60 à 100 m) pour indexer automatiquement chaque polygone et point d'intérêt.",
          deliverable: "Fonction SQL : renvoie tous les éléments OSM pour un index H3 donné.",
          status: 'ready'
        },
        {
          id: 'M1.3',
          role: 'Backend',
          desc: "Créer une API REST/gRPC légère (Go ou Node.js) servant les données vectorielles d'une cellule H3 et de ses voisines immédiates en JSON compressé.",
          deliverable: "Endpoint GET /api/v1/cells/{h3_index} fonctionnel et rapide (< 50 ms).",
          status: 'ready'
        },
        {
          id: 'M1.4',
          role: 'Client / 3D',
          desc: "Développer l'algorithme d'extrusion procédurale (Godot 4 ou Unity) : transformer les coordonnées GPS relatives en maillage 3D voxelisé ou bas-poly.",
          deliverable: "Affichage dans le moteur des bâtiments extrudés et du réseau routier brut.",
          status: 'ready'
        }
      ]
    },
    {
      number: 2,
      title: 'Client mobile, géolocalisation et rendu procédural',
      timeline: 'Semaines 4 à 6',
      icon: Smartphone,
      color: 'text-emerald-400',
      badgeBg: 'bg-emerald-950 border-emerald-500 text-emerald-300',
      goal: "Avoir un avatar qui se déplace sur son smartphone dans sa propre rue en 3D.",
      missions: [
        {
          id: 'M2.1',
          role: 'Client Mobile',
          desc: "Implémenter le pont natif GPS (Android/iOS) avec filtrage par filtre de Kalman (pour lisser la marche et éviter les téléportations de signal).",
          deliverable: "Avatar virtuel qui suit fidèlement la position du téléphone sans saccades.",
          status: 'ready'
        },
        {
          id: 'M2.2',
          role: 'Client / Rendu',
          desc: "Système de streaming de chunks géographiques : charger les cellules H3 à 200 m devant le joueur et décharger celles en arrière-plan.",
          deliverable: "Balade fluide à 60 FPS sans coupure réseau ni pic mémoire.",
          status: 'ready'
        },
        {
          id: 'M2.3',
          role: 'Direction Artistique',
          desc: "Créer un shader d'ambiance post-apo : textures de béton fissuré, végétation envahissante sur les façades, ciel orageux/pollué et brouillard de guerre.",
          deliverable: "Rendu visuel cohérent type « fin du monde » appliqué sur les blocs OSM.",
          status: 'ready'
        },
        {
          id: 'M2.4',
          role: 'UI / Ergonomie',
          desc: "Concevoir l'interface mobile 'nomade' : lisible en plein soleil, navigation à une seule main, minimap radar et boussole.",
          deliverable: "HUD fonctionnel avec radar de proximité des points d'intérêt (POI).",
          status: 'ready'
        }
      ]
    },
    {
      number: 3,
      title: 'Boucle de survie, capture et combat',
      timeline: 'Semaines 7 à 9',
      icon: Sword,
      color: 'text-red-400',
      badgeBg: 'bg-red-950 border-red-500 text-red-300',
      goal: "Introduire les interactions de gameplay réelles de scavenging et de dressage.",
      missions: [
        {
          id: 'M3.1',
          role: 'Gameplay / Data',
          desc: "Mapper les tags OSM vers des tables de loot : amenity=pharmacy → Kits de soin, shop=hardware → Ferraille, amenity=restaurant → Rations.",
          deliverable: "Tables de drop complètes associées aux polygones réels.",
          status: 'ready'
        },
        {
          id: 'M3.2',
          role: 'Gameplay',
          desc: "Système de récolte sécurisé : rayon d'interaction de 20 à 30 mètres autour des bâtiments avec mini-jeu rapide de fouille (1 tap/hold).",
          deliverable: "Possibilité de fouiller un vrai bâtiment depuis le trottoir d'en face.",
          status: 'ready'
        },
        {
          id: 'M3.3',
          role: 'Gameplay / IA',
          desc: "Génération de chimères mutantes selon le biome : Aquatiques le long des cours d'eau, Drones en zone industrielle, Bêtes dans les parcs.",
          deliverable: "Monstres visibles sur la carte qui patrouillent autour de leurs nids.",
          status: 'ready'
        },
        {
          id: 'M3.4',
          role: 'Gameplay',
          desc: "Système de combat/capture au tour par tour : affaiblir la créature, lui injecter une balise de piratage/soumission, puis l'ajouter à son escouade.",
          deliverable: "Capture complète d'un mutant avec fiche de stats (dégâts, vitesse, transport).",
          status: 'ready'
        }
      ]
    },
    {
      number: 4,
      title: 'Sédentarisation, bunker et voxel crafting',
      timeline: 'Semaines 10 à 12',
      icon: Home,
      color: 'text-amber-400',
      badgeBg: 'bg-amber-950 border-amber-500 text-amber-300',
      goal: "Le gameplay à la maison, hors des dangers de la rue, avec construction et craft.",
      missions: [
        {
          id: 'M4.1',
          role: 'Gameplay / Client',
          desc: "Déclaration du « Hub / Abri » : le joueur enregistre un point GPS fixe (ex. son domicile) qui devient une zone sécurisée permanente.",
          deliverable: "Point de respawn et inventaire de stockage persistant.",
          status: 'ready'
        },
        {
          id: 'M4.2',
          role: 'Client / Voxel',
          desc: "Moteur de construction de blocs à l'intérieur du périmètre du bunker : poser/détruire des blocs, placer des établis, tourelles et générateurs.",
          deliverable: "Éditeur de base fluide avec physique de blocs élémentaire.",
          status: 'ready'
        },
        {
          id: 'M4.3',
          role: 'Gameplay',
          desc: "Arbre de craft et gestion des ressources : raffiner les matières premières, soigner/nourrir ses chimères, assembler des modules d'équipement.",
          deliverable: "Économie fermée fonctionnelle entre le sac à dos d'expédition et le coffre d'abri.",
          status: 'ready'
        }
      ]
    },
    {
      number: 5,
      title: 'Multijoueur, persistance et synchronisation delta',
      timeline: 'Semaines 13 à 16',
      icon: Users,
      color: 'text-purple-400',
      badgeBg: 'bg-purple-950 border-purple-500 text-purple-300',
      goal: "Connecter les survivants entre eux sur un même territoire géolocalisé.",
      missions: [
        {
          id: 'M5.1',
          role: 'Backend Réseau',
          desc: "Déployer un cluster Nakama ou serveur WebSocket dédié pour gérer la présence des joueurs par cellule H3 (qui est dans la zone ?).",
          deliverable: "Visibilité en temps réel des autres joueurs se déplaçant dans le même quartier.",
          status: 'ready'
        },
        {
          id: 'M5.2',
          role: 'Backend / Sync',
          desc: "Gestion des deltas de monde : quand un joueur fortifie ou détruit un point d'intérêt public, enregistrer uniquement la modification spatiale en base.",
          deliverable: "Persistance collective : un barrage érigé dans une rue est visible par tous.",
          status: 'ready'
        },
        {
          id: 'M5.3',
          role: 'Gameplay / Réseau',
          desc: "Système d'échange de proximité (troc peer-to-peer sécurisé) et boîtes aux lettres numériques (déposer du matériel dans une cache pour un allié).",
          deliverable: "Échange fonctionnel d'objets et de créatures entre deux téléphones proches.",
          status: 'ready'
        },
        {
          id: 'M5.4',
          role: 'Gameplay / PvP',
          desc: "Contrôle de points stratégiques réels (châteaux d'eau, sous-stations électriques) générant des ressources pour la faction qui les défend.",
          deliverable: "Guerres de territoire asynchrones avec placement de créatures en sentinelles.",
          status: 'ready'
        }
      ]
    },
    {
      number: 6,
      title: 'Véhicules, équilibrage et bêta fermée',
      timeline: 'Semaines 17 à 20',
      icon: Truck,
      color: 'text-blue-400',
      badgeBg: 'bg-blue-950 border-blue-500 text-blue-300',
      goal: "Expansion du rayon d'action avec véhicules et sécurisation anti-triche.",
      missions: [
        {
          id: 'M6.1',
          role: 'Gameplay / Client',
          desc: "Véhicules assemblés (buggy, vélo tout-terrain) permettant d'augmenter le rayon d'exploration et la capacité d'emport de fret.",
          deliverable: "Système de transport débloqué avec consommation de carburant/batterie.",
          status: 'ready'
        },
        {
          id: 'M6.2',
          role: 'Sécurité / Anti-cheat',
          desc: "Intégration de sécurités anti-spoofing GPS (vérification des vitesses anormales, détection des faux mocks de localisation sur Android).",
          deliverable: "Bannissement automatique des téléportations impossibles.",
          status: 'ready'
        },
        {
          id: 'M6.3',
          role: 'QA / Prod',
          desc: "Test de charge sur un secteur géographique restreint (ex. un arrondissement ou une ville moyenne) avec 50 à 100 bêta-testeurs.",
          deliverable: "Rapport de perfs réseau, consommation de batterie mobile et corrections de bugs.",
          status: 'ready'
        }
      ]
    }
  ];

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Top Banner */}
      <div className="bg-gradient-to-r from-emerald-950/40 via-slate-900 to-slate-950 border border-emerald-500/30 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-emerald-500/20 border border-emerald-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(16,185,129,0.2)]">
            📋
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Feuille de Route Complète du Projet (Phases 1 à 6)</h2>
              <span className="text-[10px] bg-emerald-950 text-emerald-300 border border-emerald-800 px-2 py-0.5 rounded font-mono">
                Semaines 1 à 20
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Architecture technique validée : PostGIS + Uber H3 + Rendu Voxel 3D + Core Loop Nomade/Sédentaire.
            </p>
          </div>
        </div>
      </div>

      {/* Sprint 0 Immediate Execution Card */}
      <div className="bg-gradient-to-r from-amber-950/50 to-slate-900 border-2 border-amber-500/60 rounded-2xl p-4 mb-4 shadow-lg">
        <div className="flex items-center gap-2 mb-2">
          <Terminal className="w-5 h-5 text-amber-400" />
          <h3 className="font-tech text-sm font-bold text-amber-300 uppercase">
            Sprint 0 — Ordre d'exécution immédiat (Démarrage dès aujourd'hui)
          </h3>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5 text-xs">
          <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800">
            <div className="font-bold text-white mb-1">1. Valider le Moteur 3D</div>
            <p className="text-slate-400">Valider <strong>Godot 4 (C#)</strong> ou <strong>Unity</strong> pour le pipeline d'extrusion de maillage 3D.</p>
          </div>
          <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800">
            <div className="font-bold text-white mb-1">2. Exécuter M1.1 & M1.3</div>
            <p className="text-slate-400">Poser le serveur PostGIS et extraire un extrait OSM (Overpass / .pbf) de 500m à 5km².</p>
          </div>
          <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800">
            <div className="font-bold text-white mb-1">3. Exécuter M1.4</div>
            <p className="text-slate-400">Afficher les premiers cubes et polygones extrudés 3D correspondant aux vrais immeubles.</p>
          </div>
        </div>
      </div>

      {/* Phases Accordions */}
      <div className="space-y-3 flex-1 overflow-y-auto">
        {phases.map((phase) => {
          const Icon = phase.icon;
          const isExpanded = expandedPhase === phase.number;

          return (
            <div
              key={phase.number}
              className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow transition-all"
            >
              {/* Header */}
              <button
                onClick={() => setExpandedPhase(isExpanded ? 0 : phase.number)}
                className="w-full p-4 flex items-center justify-between text-left hover:bg-slate-850 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <div className={`w-10 h-10 rounded-xl bg-slate-950 border border-slate-800 flex items-center justify-center ${phase.color}`}>
                    <Icon className="w-5 h-5" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-tech font-bold text-white text-sm">
                        Phase {phase.number} — {phase.title}
                      </span>
                      <span className={`text-[10px] px-2 py-0.5 rounded-full border font-mono ${phase.badgeBg}`}>
                        {phase.timeline}
                      </span>
                    </div>
                    <p className="text-xs text-slate-400 mt-0.5">{phase.goal}</p>
                  </div>
                </div>

                <div className="p-1 rounded-lg bg-slate-800 text-slate-400">
                  {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                </div>
              </button>

              {/* Body */}
              {isExpanded && (
                <div className="px-4 pb-4 border-t border-slate-800/80 pt-3">
                  <div className="space-y-2.5">
                    {phase.missions.map((m) => (
                      <div
                        key={m.id}
                        className="bg-slate-950 border border-slate-800/80 rounded-xl p-3 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs"
                      >
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <span className="font-mono font-bold text-cyan-400 bg-cyan-950/80 border border-cyan-800 px-1.5 py-0.2 rounded text-[11px]">
                              {m.id}
                            </span>
                            <span className="text-[11px] font-mono text-amber-400">{m.role}</span>
                          </div>
                          <p className="text-slate-300 font-medium mb-1">{m.desc}</p>
                          <div className="text-[11px] text-slate-400">
                            🎯 <strong>Livrable :</strong> {m.deliverable}
                          </div>
                        </div>

                        <div className="flex items-center gap-1.5 self-start sm:self-center">
                          <span className="text-[10px] bg-emerald-950 text-emerald-400 border border-emerald-800 px-2 py-0.5 rounded-full font-mono flex items-center gap-1">
                            <CheckCircle2 className="w-3 h-3" /> Intégré dans le prototype
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
