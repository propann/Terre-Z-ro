# Terre Zéro — Combat, Capture et Dressage des Chimères

## Direction

Terre Zéro conserve une exploration 3D propre avec une identité visuelle rétro 2D/2.5D pour les interfaces et les combats.

Le combat doit rester rapide :
- 1 Chimère active à la fois ;
- équipe de 3 maximum ;
- 4 techniques maximum par Chimère ;
- alternance immédiate joueur / adversaire ;
- peu d'actions, mais des rôles très lisibles.

## Boucle

```text
exploration 3D
  ↓
rencontre
  ↓
combat rapide
  ↓
affaiblir PV + stabilité
  ↓
marquer / contrôler
  ↓
capture ou neutralisation
  ↓
XP + lien + dressage
  ↓
évolution
  ↓
retour exploration
```

## Statistiques

Chaque Chimère possède :
- PV ;
- attaque ;
- défense ;
- vitesse ;
- stabilité ;
- lien ;
- dressage ;
- niveau ;
- expérience ;
- stade d'évolution.

La stabilité est distincte des PV et sert principalement à ouvrir une fenêtre de capture.

## Techniques

Quatre types :
- Damage ;
- StabilityBreak ;
- Control ;
- Support.

Une Chimère possède au maximum 4 techniques.

## Capture

La chance de capture dépend de :
- PV manquants ;
- stabilité restante ;
- statut Marqué ou Étourdi ;
- rôle Capturer de la Chimère active ;
- lien du compagnon ;
- différence de niveau ;
- qualité future du module de capture.

La cible doit idéalement être affaiblie sans être neutralisée.

## Équipe

- 3 Chimères maximum dans l'équipe active ;
- la première est la Chimère active ;
- les captures supplémentaires vont en réserve ;
- si l'active tombe, la première Chimère encore disponible prend le relais.

## Dressage

Le dressage consomme des points.
Intensité :
- léger : 1 point ;
- soutenu : 2 points ;
- intensif : 3 points.

Le dressage augmente :
- lien ;
- valeur de dressage ;
- certaines statistiques à intervalles réguliers.

## Évolution

L'évolution n'est pas déclenchée uniquement par le niveau.

Stade 1 :
- niveau 6 ;
- lien 25 ;
- dressage 18.

Stade 2 :
- niveau 12 ;
- lien 60 ;
- dressage 45.

L'évolution améliore fortement les statistiques et change le nom de la forme.

## Affinités V1

- Organic
- Scrap
- Electric
- Toxic
- Spectral
- Mineral
- Thermal
- Hydro
- Unstable
- Radiant

Premiers avantages :
- Electric > Hydro
- Hydro > Thermal
- Thermal > Organic
- Organic > Mineral
- Mineral > Scrap
- Spectral > Unstable
- Radiant > Spectral
- Toxic > Organic

Une même affinité contre elle-même subit une légère réduction.

## Bestiaire V1

- Mordrail — Scrap / Breaker
- Nébuli — Spectral / Capturer
- Cerf d'Écorce — Organic / Guardian
- Voltac — Electric / Assault
- Hydrune — Hydro / Support
- Cendrex — Thermal / Assault
- Mycoryx — Toxic / Control
- Prismole — Radiant / Capturer
- Ferrale — Mineral / Guardian

## Contexte OSM

La rencontre peut être influencée par le contexte de la zone :
- industriel / ferroviaire → Scrap, Electric, Mineral ;
- parc / bois / naturel → Organic, Toxic, Hydro ;
- urbain générique → Spectral, Thermal, Radiant, Electric.

## Contrôles PC prototype

- C : provoquer une rencontre Chimère ;
- T : ouvrir le dressage ;
- F : scanner X-Ray ;
- clic gauche : extraire ;
- clic droit : construire ;
- Échap : libérer la souris.

## Sauvegarde

L'escouade, la réserve, le niveau, le lien, le dressage, l'évolution, les modules de capture et les points de dressage sont sauvegardés localement dans :

```text
user://chimeres.json
```

## Étapes suivantes

- sprites / silhouettes 2D animées pour chaque espèce dans la scène de combat ;
- effets sonores et impacts ;
- véritable apparition de Chimères dans le monde 3D au lieu du raccourci C ;
- objets de capture de qualités différentes ;
- échange actif entre les 3 membres en combat ;
- boss et formes rares ;
- backend autoritaire pour les captures multijoueur.
