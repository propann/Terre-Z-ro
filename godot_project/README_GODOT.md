# Terre Zéro — Client Godot 4 C# / .NET

Ce dossier contient le client de jeu principal de **Terre Zéro**.

Terre Zéro est un jeu de survie post-apocalyptique géolocalisé dans lequel le monde réel est reconstruit en micro-voxels à partir d'OpenStreetMap.

## Prérequis

- Godot Engine 4.3 **.NET / C#**
- .NET 8 SDK

## Lancement

1. Ouvrir Godot 4.3 .NET.
2. Importer `godot_project/project.godot`.
3. Compiler les scripts C#.
4. Lancer la scène principale avec **F5**.

## Contrôles actuels

- **Z/Q/S/D ou W/A/S/D** : déplacement
- **Souris** : caméra
- **Espace** : saut
- **Clic gauche** : forage / destruction micro-voxel
- **Clic droit** : pose de bloc
- **F** : scanner X-Ray

## Architecture

Les Chimères sont des créatures du monde de Terre Zéro. Elles ne constituent plus l'identité du projet.

Le namespace racine du client est désormais `TerreZero`.
