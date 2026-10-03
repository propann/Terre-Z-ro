# ☢️ TERRE ZÉRO : MMORPG & Survie Géolocalisée Micro-Voxel (Godot 4 C#)

> **Un monde réel en micro-voxels destructibles (1 voxel = 20 cm) généré de manière déterministe via OpenStreetMap, avec Greedy Meshing pour mobile, streaming spatial Uber H3, filtre de Kalman GPS, véhicules modulaires et capture de Chimères.**

---

## 🎮 1. Pitch & Vision Globale

**Terre Zéro** est un jeu de survie post-apocalyptique multijoueur synchrone/asynchrone basé sur la géolocalisation réelle.
La topographie du monde réel (bâtiments, voiries, parcs) est transcrite à la volée en un univers **micro-voxel (20 cm)** destructible chirurgicalement et constructible.

### Les 3 Boucles Fondamentales :
1. **🧭 Exploration Nomade (IRL) :** Déplacements physiques réels avec filtre de Kalman EKF, rayon d'action sécurisé de $25\text{ m}$ (Bounding Bubble), radar de proximité et traque de Chimères.
2. **⛏️ Survie & Forage Voxel :** Extraction chirurgicale des composants dans le décor (creuser la façade d'une usine pour extraire du cuivre, percer un mur de pharmacie pour des kits médicaux).
3. **🏠 Sédentarisation (Le Bunker) :** Ancrage GPS de sa résidence réelle, construction voxel libre, concasseur/fonderie de matières premières, établi d'assemblage et assignation de Chimères.

---

## 🏃 2. Progression Physique du Personnage & Arbres de Maîtrise

### A. Condition Physique Réelle (Paliers Kilométriques Cumulés)
| Distance Cumulée | Trait Débloqué | Impact Concret en Jeu |
| :--- | :--- | :--- |
| **10 km** | **Foulée économique** | Réduit de $15\%$ la fatigue lors des sprints d'évasion face aux meutes. |
| **50 km** | **Dos d'acier** | Débloque $+15\text{ kg}$ de charge maximale dans le sac à dos d'expédition. |
| **100 km** | **Sens du pisteur** | Augmente le rayon de détection passive des créatures rares ($+10\text{ m}$). |
| **250 km** | **Métabolisme durci** | Résistance passive aux zones de retombées radioactives et toxiques ($+20\%$). |
| **500 km** | **Vagabond vétéran** | Vitesse de marche accrue ; consommation de rations réduite de moitié. |

### B. Les 3 Voies de Spécialisation (Arbres de Compétences) :
```
                        [ ARBRE DU SURVIVANT ]
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
  [ INGÉNIEUR ]            [ BIO-PISTEUR ]          [ BRIGAND / COMBAT ]
 (Voxel, Craft,           (Chimères, Scan,         (Armes, Forage lourd,
  Défense de base)         Traque, Troc)            Survie hostile)
```
* **Voie A : Ingénieur de brèche :** *Découpe chirurgicale* ($-30\%$ perte noble), *Sonde de résonance*, *Béton armé rapide*, *Maître recycleur*, **Ultime : Surcharge d'atelier** ($-50\%$ énergie requise au bunker).
* **Voie B : Bio-Pisteur :** *Fréquence de capture* ($+15\%$), *Écholocalisation 500m*, *Empathie mutante*, *Siphon génétique*, **Ultime : Lien synaptique double** (2 Chimères simultanées en escorte).
* **Voie C : Ferrailleur lourd :** *Stabilisateur de tir*, *Perforateur thermique*, *Charge d'impact*, *Cuirasse de récup'*, **Ultime : Dernier rempart** ($5\text{ s}$ d'invulnérabilité en cas de coup fatal).

### C. Implants Cybernétiques (Greffes au Bunker) :
1. **Implant Oculaire (Scanner Spectrométrique) :** Filtre thermique $\rightarrow$ Failles structurelles $\rightarrow$ Spectromètre de métaux.
2. **Implant Rachidien (Exosquelette Dorsal) :** Décuple la charge utile et supprime le malus d'armes lourdes de forage.
3. **Implant Cérébral (Interface de Piratage) :** Réduit de $50\%$ le temps d'injection des modules de contrôle.
4. **Implant Dermique (Blindage Sous-Cutané) :** Grille sous-cutanée réduisant les dégâts d'acide et d'entailles.

### D. Gestion de la Mort & Caisse de Largage (Death Crate 24h) :
* **Réapparition immédiate à l'abri.**
* **Dépôt de la caisse de mort** aux coordonnées GPS exactes avec **fenêtre de 24 heures réelles** pour marcher physiquement sur site et récupérer le sac de minerai.
* **Matériel équipé et Chimères protégés** (seule la durabilité diminue).

---

## 🏎️ 3. Système de Véhicules & Conduite Hybride OSM

* **Collisions Hybrides :** Piste de roulement continue sur les vecteurs routiers OSM (`highway=*`) + broyage des obstacles voxels à la proue.
* **Mode Convoi Nomade Autonome :** Le véhicule suit l'avatar à pied/vélo IRL en convoi, chargeant automatiquement le minerai extrait.
* **3 Châssis Modulaires :** Moto-Scrap, Buggy léger, Camion blindé 6x6.

---

## 🧬 4. Système des Chimères (Écologie OSM & Anatomie Voxel)

* **Classification :** Technoïdes Lourds & Réseau / Biomutants Terrestres & Sylvestres.
* **Ciblage Anatomique :** Membres locomoteurs, réservoirs de bile acide, plaques de blindage et noyau vital (**Overkill = Incapturable !**).
* **Capture :** Puce d'Override IEM (Technoïdes) ou Injecteur Neurotoxique (Biomutants).
* **Utilité au Bunker :** Générateur vivant ($+15\text{ à }+30\text{ kW}$), Sentinelle territoriale, Bête de somme ($+15\text{ kg}$ sac) et Foreuse assistée ($\times 3$).

---

## 📐 5. Métrique & Performance Micro-Voxel

* **Résolution :** $1\text{ voxel} = 0{,}2\text{ m}$ ($20\text{ cm}$).
* **Format Chunk :** $32 \times 32 \times 32\text{ voxels}$ ($6{,}4\text{ m}$ d'arête).
* **Encodage 16-bit (`ushort`) :** Matériaux, durabilité ($0..15$) et flags électriques.
* **Greedy Meshing Mobile :** Réduction de **$-98\%$ de polygones GPU** ($< 600$ triangles par bâtiment contre $> 28\,000$ bruts).

---

## 🛠️ 6. Structure du Projet Godot 4 (.NET / C#)

```
godot_project/
├── project.godot               # Configuration Godot 4.3 (Renderer Forward+, Input Mapping ZQSD)
├── scenes/
│   ├── MainWorld.tscn          # Scène principale 3D, Sky & Fog volumétrique
│   ├── Player.tscn             # CharacterBody3D, Caméra FPS, Raycast minage et Box selector
│   └── VoxelChunk.tscn         # Instance de chunk 32³ avec MeshInstance3D et Trimesh Collision
├── scripts/
│   ├── KalmanGpsFilter.cs      # Filtre EKF GPS, anti-spoof vitesse (>30 km/h) et Bounding Bubble 25m
│   ├── VoxelChunk.cs           # Grille 16-bit compacte et méthode chirurgicale CarveSphere
│   ├── GreedyMesher.cs         # Algorithme Greedy Meshing multithread avec Vertex AO
│   ├── OSMVoxelizer.cs         # Rasteriseur 2.5D des empreintes vectorielles OpenStreetMap
│   ├── PlayerController.cs     # Contrôleur First-Person Minecraft-like, minage laser et pose de blocs
│   ├── PlayerProgression.cs    # Paliers kilométriques, 3 arbres de compétences, implants et Death Crate
│   ├── VehicleController.cs    # Conduite hybride OSM, broyage d'obstacles et convoi autonome nomade
│   ├── ChimereManager.cs       # Spawns contextuels OSM, ciblage anatomique voxel et capture
│   ├── BunkerManager.cs        # Ancrage domicile, fonderie, raffinerie et tourelles
│   └── DeltaSyncManager.cs     # Sérialisation et diffusion réseau des paquets de delta RLE
├── server_go/
│   ├── main.go                 # Serveur HTTP/WebSocket Go avec hub spatial H3 Res 9
│   └── schema.sql              # Schéma PostgreSQL 16 + PostGIS (GIST polygon, index H3)
├── shaders/
│   └── retro_voxel.gdshader    # Shader spatial rétro-moderne Lo-Fi Cyber-Scrap
└── README_GODOT.md             # Guide de démarrage dans Godot Engine 4.3
```
