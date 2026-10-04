# ☢️ TERRE ZÉRO

Jeu de survie / exploration post-apocalyptique en **Godot 4 .NET (C#)**, construit à partir du monde réel.

Le joueur choisit un point de départ proche de sa position machine. Ce point devient l'ancre fixe de la session : **H3 + OpenStreetMap + météo réelle** alimentent alors le monde 3D micro-voxel.

## Direction actuelle

TERRE ZÉRO est un jeu, pas une application Web.

Stack principale :

~~~text
Godot 4 .NET
    ↓
Go backend
    ↓
PostgreSQL 16 + PostGIS
    ├── OSM/H3
    ├── monde persistant
    └── deltas voxel

Open-Meteo → météo réelle
GeoClue    → position desktop Linux avec consentement
Overpass   → import OSM développement/cache
~~~

## Monde

- voxel : **20 cm**
- chunk : **32³ voxels = 6,4 m**
- greedy meshing
- shader voxel rétro/toon
- destruction et construction multi-chunks
- streaming spatial H3 résolution 9
- bâtiments et routes issus d'OpenStreetMap
- intérieurs déterministes générés à partir des empreintes OSM
- persistance des modifications sous forme de deltas
- GeneratorVersion actuelle : **4**

Generator v4 ajoute notamment :

- routes différenciées ;
- trottoirs ;
- accotements ;
- marquages routiers ;
- vitrines contextuelles ;
- parapets de toit ;
- façades liées au type de bâtiment.

## Monde réel

### Localisation PC

Sur Linux / Pop!_OS :

1. le joueur autorise la localisation ;
2. GeoClue fournit un point machine approximatif ;
3. le joueur choisit son départ dans un rayon de **5 km ou 10 km** ;
4. le backend valide le point ;
5. ce point devient l'ancre fixe du monde.

Il n'y a pas de suivi GPS continu sur PC.

### OpenStreetMap

Le backend peut remplir automatiquement une cellule H3 depuis Overpass lorsque le cache PostGIS est absent ou périmé.

Le cache OSM conserve notamment :

- bâtiments ;
- types de bâtiments ;
- noms ;
- commerces / amenities ;
- hauteurs / niveaux ;
- routes ;
- surfaces ;
- nombre de voies.

Voir `docs/OSM_INGESTION.md`.

**Données cartographiques © OpenStreetMap contributors.**

### Météo réelle

La météo du point d'ancrage influence réellement le rendu :

- heure locale ;
- température ;
- humidité ;
- couverture nuageuse ;
- pluie ;
- neige ;
- vent ;
- brouillard ;
- orages ;
- lumière du jour ;
- surfaces mouillées ;
- flaques ;
- neige au sol ;
- lampadaires ;
- végétation animée par le vent.

Le backend met les réponses météo en cache pendant 10 minutes.

Voir `docs/REAL_WORLD_WEATHER.md`.

## Direction artistique

Le monde reste une 3D propre, lisible et relativement low-poly/voxel, avec une identité rétro volontaire :

- palette post-apocalyptique désaturée ;
- toon shading léger ;
- patine procédurale ;
- météo intégrée aux matériaux ;
- HUD sombre / ambre ;
- interfaces combat inspirées des JRPG rétro ;
- signalétique OSM discrète ;
- lampadaires, barrières, débris, végétation et flaques procéduraux.

## Chimères

Le système jouable comprend déjà :

- rencontres visibles dans le monde 3D ;
- combat rapide 1v1 ;
- équipe de 3 ;
- changement de compagnon ;
- 4 techniques maximum ;
- dégâts / stabilité / contrôle / support ;
- affinités ;
- capture ;
- variantes Rare et Alpha ;
- XP et niveaux ;
- lien ;
- dressage ;
- deux stades d'évolution ;
- sauvegarde locale.

Le premier bestiaire contient actuellement 9 espèces :

- Mordrail
- Nébuli
- Cerf d'Écorce
- Voltac
- Hydrune
- Cendrex
- Mycoryx
- Prismole
- Ferrale

Voir `docs/CHIMERES_COMBAT_GDD.md`.

## Survie et progression

Fondations actuelles :

- santé ;
- endurance ;
- sprint ;
- radiation ;
- soins ;
- inventaire avec poids ;
- loot déterministe ;
- crafting ;
- quêtes ;
- bunker ;
- raffinerie ;
- infirmerie ;
- dressage ;
- stockage ;
- progression joueur ;
- exploration persistante.

Voir `docs/GAMEPLAY_FOUNDATIONS.md`.

## Contrôles PC actuels

~~~text
ZQSD / WASD   déplacement
Souris        caméra
Shift         sprint
Molette       changer le matériau de construction
Clic gauche   extraire / détruire
Clic droit    construire
F             scanner X-Ray
I             terminal de terrain
C             rencontre Chimère debug
T             dressage
H             soin
Échap         libérer la souris
~~~

Le voxel ciblé est surligné avec la couleur du matériau actuellement sélectionné.

## Lancement Pop!_OS

### Vérifier l'environnement

~~~bash
bash tools/check_popos_dev.sh
~~~

### Backend + PostGIS

~~~bash
docker compose up -d --build
~~~

Vérifier :

~~~bash
curl http://127.0.0.1:8080/api/v1/health
~~~

### Godot

Ouvrir :

~~~text
godot_project/project.godot
~~~

Pour utiliser les données réelles, activer `UseRemoteWorldData` sur `WorldBootstrap`.

Voir `docs/POP_OS_SETUP.md`.

## Structure utile

~~~text
Terre-Z-ro/
├── godot_project/
│   ├── project.godot
│   ├── scenes/
│   ├── scripts/
│   ├── shaders/
│   └── server_go/
│       ├── main.go
│       ├── weather.go
│       ├── osm_ingest.go
│       ├── schema.sql
│       └── Dockerfile
├── docs/
│   ├── CHIMERES_COMBAT_GDD.md
│   ├── GAMEPLAY_FOUNDATIONS.md
│   ├── GENERATOR_V4.md
│   ├── OSM_INGESTION.md
│   ├── POP_OS_SETUP.md
│   └── REAL_WORLD_WEATHER.md
├── tools/
│   ├── check_popos_dev.sh
│   └── validate_godot_resources.py
└── compose.yml
~~~

## CI

La CI vérifie uniquement les composants utiles au jeu :

- ressources/scènes Godot ;
- compilation C# ;
- backend Go ;
- tests Go ;
- build Docker du backend.

Le prototype Web historique a été retiré de la branche principale.

## Limites actuelles connues

- l'import OSM automatique gère les `way` bâtiments/routes ; les relations multipolygon viendront plus tard ;
- les intérieurs sont déterministes mais encore simples ;
- les Chimères 3D utilisent encore des formes procédurales temporaires ;
- l'authentification multijoueur n'est pas encore en place ;
- les captures/inventaires ne sont pas encore autoritaires côté serveur ;
- les collisions voxel sont reconstruites par chunk et devront être optimisées pour les gros mondes.

## Principe de développement

~~~text
monde réel stable
    ↓
génération déterministe
    ↓
deltas persistants
    ↓
gameplay
    ↓
présentation rétro propre
~~~

Les changements de géométrie déterministe doivent toujours incrémenter `GeneratorVersion`.