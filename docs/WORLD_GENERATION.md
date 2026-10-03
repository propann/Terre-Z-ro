# Génération du monde — Terre Zéro

## Principe

Terre Zéro ne demande pas au GPS de dessiner une carte.

Le GPS donne **l'ancrage géographique du joueur**. À partir de cette position, le moteur charge la cellule spatiale correspondante et récupère les données OpenStreetMap de la zone.

OpenStreetMap fournit la réalité extérieure : empreintes des bâtiments, routes, chemins, parcs et certains attributs. Terre Zéro transforme ensuite cette géométrie en monde jouable.

## Pipeline canonique

```text
GPS
  ↓
latitude / longitude du joueur
  ↓
cellule H3
  ↓
données OpenStreetMap
  ↓
empreintes extérieures réelles
  ↓
voxelisation Terre Zéro
  ↓
génération procédurale déterministe
  ├── façades
  ├── étages
  ├── pièces
  ├── cloisons
  ├── portes
  ├── escaliers
  ├── réseaux techniques
  └── caches contextuelles
  ↓
application des deltas persistants du serveur
  ↓
monde final affiché au joueur
```

## Déterminisme

Un bâtiment possède une graine dérivée de :

```text
world_version
+ generator_version
+ osm_id
+ building_type
+ amenity
```

Aucun `Math.random()` ne doit intervenir dans la génération canonique.

Deux joueurs qui chargent le même bâtiment avec les mêmes versions obtiennent donc exactement la même structure de départ.

## Monde de base vs monde modifié

Le serveur ne doit pas enregistrer des millions de voxels générés.

Il enregistre uniquement les modifications du monde de base :

- voxel détruit ;
- voxel posé ;
- structure renforcée ;
- ressource consommée ;
- état persistant nécessaire au gameplay.

Au chargement :

```text
monde déterministe OSM/procédural
+
deltas persistants
=
état courant de Terre Zéro
```

## Versions

`world_version` identifie une époque logique du monde.

`generator_version` identifie l'algorithme procédural utilisé pour construire les intérieurs.

Changer l'algorithme sans changer `generator_version` est interdit dès que des joueurs ont commencé à modifier le monde, car leurs deltas pourraient alors viser des voxels différents.

## Limite actuelle

Le prototype actuel sait générer une structure déterministe à l'intérieur d'un chunk 32³.

La prochaine évolution consiste à transformer une vraie empreinte OSM, potentiellement plus grande qu'un chunk, en grille multi-chunks puis à streamer les chunks autour du joueur.
