# Chimères : Prototype AR & Moteur Géospatial Post-Apocalyptique (OSM + H3 + 3D Voxel)

Jeu mobile et web de survie géolocalisé post-apocalyptique dans le monde réel, alimenté par les données **OpenStreetMap (Overpass API)**, le découpage spatial **Uber H3** et un moteur d'extrusion 3D procédural (Godot 4 / Unity / Three.js).

---

## 🎯 Architecture & Concepts Clés

1. **Extraction de Terrain & Polygones Réels :**
   - Requête Overpass API sur un rayon de 200m à 1000m autour des coordonnées GPS du joueur.
   - Extrusion 3D automatique des bâtiments selon les tags `building:levels` et `height`.
   - Réseau routier brut, zones crevassées et parcs bio-mutés.

2. **Système Spatial Hexagonal Uber H3 :**
   - Découpage du monde en cellules hexagonales hiérarchisées (Résolution 9 ~100m).
   - Gestion du brouillard de guerre, des radiations Geiger et de la densité de mutants.

3. **Boucle de Gameplay (Core Loop) :**
   - **Phase Nomade (Dehors) :** Déplacement GPS réel ou simulateur, radar de proximité 50m, notifications haptiques, extraction de ressources en 1-tap à moins de 20m, inventaire limité en poids.
   - **Phase Sédentaire (Bunker à la maison) :** Déchargement dans les coffres, craft à l'établi, gestion de l'énergie, amélioration des défenses et entraînement des Chimères capturées.

4. **Système de Chimères & Biomes :**
   - **Chimères Mécaniques :** Drones industriels réactivés sauvagement (zones commerciales, usines, voies ferrées).
   - **Chimères Bio-mutées :** Faune altérée par les radiations (parcs, forêts, plans d'eau).
   - Combat au tour par tour, pièges à impulsion IEM et affectation des rôles au bunker (Défense, Énergie +15kW, Pistage +25m, Mule cargo +10kg).

---

## 🛠️ Feuille de Route du Projet (Phases 1 à 6)

- **Phase 1 (Semaines 1-3) :** Socle géospatial, PostGIS 16, Uber H3, API `GET /api/v1/cells/{h3_index}`, Extrusion 3D.
- **Phase 2 (Semaines 4-6) :** Pont GPS natif avec filtre de Kalman, streaming de chunks H3 à 60 FPS, shaders post-apo, HUD nomade.
- **Phase 3 (Semaines 7-9) :** Mapping des tags OSM vers tables de loot, rayon de fouille 20-30m, combat et capture de Chimères.
- **Phase 4 (Semaines 10-12) :** Bunker sécurisé, moteur de construction voxel, arbre de craft et économie fermée.
- **Phase 5 (Semaines 13-16) :** Multijoueur Nakama/WebSocket, synchronisation des deltas de monde, troc P2P, guerres de territoire.
- **Phase 6 (Semaines 17-20) :** Véhicules assemblés (buggy/vélo), anti-spoofing GPS et bêta fermée.

---

## 🚀 Démarrage Rapide

```bash
npm install
npm run dev
```

L'application démarre sur `http://localhost:3000`.
