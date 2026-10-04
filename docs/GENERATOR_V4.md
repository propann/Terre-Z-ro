# Terre Zéro — Generator v4

## Purpose

Generator v4 is a deterministic world-geometry migration focused on urban readability.

## Visual/world changes

- building aprons use sidewalk/stone instead of asphalt;
- building ground generation preserves roads that already exist;
- vehicle roads receive deterministic shoulders;
- urban shoulders use sidewalk material;
- motorway/trunk shoulders use concrete;
- footways, paths, cycleways and pedestrian roads use sidewalk material by default;
- major roads receive deterministic dashed center markings;
- retail/pharmacy ground floors receive stronger storefront glazing;
- industrial buildings keep more closed facades;
- building roofs receive a small deterministic parapet.

## Version boundary

WorldVersion remains 1.

GeneratorVersion changes from 3 to 4.

This is intentional because base voxel placement changed.

Consequences:

- generator-v3 voxel deltas are not replayed onto generator-v4 base geometry;
- newly emitted client deltas carry generator_version=4;
- backend validation expects generator_version=4;
- new PostgreSQL rows default to generator_version=4;
- existing generator-v3 rows remain preserved for migration/debugging.

## PostgreSQL

`schema.sql` updates defaults to 4 and includes ALTER COLUMN statements so an already-created database adopts v4 defaults without rewriting historic rows.

## Determinism

No runtime randomness was added. Road markings, facade patterns, storefronts and parapets are generated from stable geometry/tags/seed inputs.