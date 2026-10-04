# Terre Zéro — Fondations Gameplay

## Objectif

Cette architecture sert de socle aux prochaines mécaniques sans mélanger UI, règles de jeu, monde et persistance.

~~~text
MONDE / OSM / H3
      │
      ▼
WorldBootstrap
      │
      ├───────────────┐
      ▼               ▼
Gameplay services   Chimères
      │               │
      └───────┬───────┘
              ▼
           GameState
              │
              ▼
       GlobalSaveStore
~~~

## 1. État global

### GameState

Contient les états locaux canoniques du prototype PC :
- PlayerProfileState
- InventoryState
- QuestJournalState

Les Chimères restent actuellement dans ChimereGameState mais leur sauvegarde est orchestrée par GlobalSaveStore.

À terme, les données sensibles au multijoueur seront rendues autoritaires par le backend.

## 2. Profil joueur

PlayerProfileState contient : niveau, XP, santé, endurance, radiation, crédits, points de maîtrise, distance cumulée, captures, victoires et bâtiments explorés.

## 3. Inventaire

InventoryState gère les stacks, le poids, la limite de transport, l’ajout/retrait et la validation via ItemCatalog.

Catégories : Resource, Consumable, CaptureDevice, Crafting, Quest, Equipment.

Objets de base : ferraille, cuivre, circuit, gel médical, ration, cellule d’énergie, module de capture standard, module stabilisé et module Alpha.

## 4. Capture

La capture ne dépend plus d’un compteur abstrait. CaptureDeviceService sélectionne un vrai objet de l’inventaire.

Pour une Alpha : module Alpha si disponible, sinon stabilisé, sinon standard.

Chaque module possède un coefficient de qualité appliqué à la formule de capture.

## 5. Loot

LootDirector est déterministe à partir de source_id + contexte + seed.

- pharmacie / hôpital → soins et modules
- industriel / ferroviaire → ferraille, cuivre, circuits
- parc / nature → rations, soins, modules standards
- urbain → loot généraliste

## 6. Progression

GameplayProgressionService centralise capture, victoire, collecte, exploration et distance parcourue. Il met à jour ensemble profil, inventaire et quêtes.

## 7. Quêtes et événements

QuestJournalState stocke les QuestRecord.

Objectifs V1 : CaptureChimere, WinBattle, CollectItem, ExploreBuilding, ReachDistance.

Quêtes initiales : Premier contact (capturer une Chimère) et Récupération de terrain (5 ferrailles).

## 8. Sauvegarde

GlobalSaveStore orchestre user://terre_zero_save.json pour le profil, l’inventaire et les quêtes, et déclenche aussi la sauvegarde Chimères existante user://chimeres.json.

Cette séparation est temporaire et permet une migration progressive sans casser les sauvegardes actuelles.

## 9. Règle d’architecture

~~~text
Données
  ↓
Services / règles
  ↓
Orchestration
  ↓
UI
~~~

À éviter : règles de loot dans un écran, inventaire stocké dans le HUD, progression calculée dans un bouton, génération aléatoire non seedée, coordonnées GPS utilisées comme identifiant de persistance.

## 10. Prochaines fondations

1. UI inventaire / profil / journal
2. crafting et recettes
3. connexion BunkerManager ↔ InventoryState
4. conteneurs de loot physiques dans le monde
5. santé/endurance/radiation réellement branchées au joueur
6. événements de secteur
7. boss / zones dangereuses
8. migrations de sauvegarde
9. autorité backend pour inventaire, captures et progression