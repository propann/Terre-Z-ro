using System;

namespace TerreZero.Chimeres
{
    public static class ChimereSpeciesCatalog
    {
        public static ChimereCombatant CreateMordrail(int level = 3)
        {
            var c = Base("mordrail", "Mordrail", ChimereAffinity.Scrap, ChimereCombatRole.Breaker, level, 92, 25, 18, 12);
            c.PassiveName = "Mâchoire de chantier";
            c.PassiveDescription = "Spécialisé dans la rupture de stabilité.";
            c.Moves.AddRange(new[]
            {
                Move("ram", "Charge d'acier", ChimereMoveKind.Damage, 18, 6, "Percussion lourde."),
                Move("fang", "Croc d'impact", ChimereMoveKind.StabilityBreak, 10, 24, "Brise la stabilité."),
                Move("roar", "Hurlement brut", ChimereMoveKind.Control, 4, 10, "Affaiblit la cible.", ChimereStatus.Weakened),
                Move("lock", "Verrouillage", ChimereMoveKind.Control, 0, 18, "Marque la cible.", ChimereStatus.Marked)
            });
            return c;
        }

        public static ChimereCombatant CreateNebuli(int level = 3)
        {
            var c = Base("nebuli", "Nébuli", ChimereAffinity.Spectral, ChimereCombatRole.Capturer, level, 72, 18, 12, 21);
            c.PassiveName = "Résonance calme";
            c.PassiveDescription = "Excellent compagnon de capture.";
            c.Moves.AddRange(new[]
            {
                Move("wave", "Onde calme", ChimereMoveKind.StabilityBreak, 7, 20, "Désynchronise la cible."),
                Move("mist", "Brume liée", ChimereMoveKind.Control, 3, 12, "Ralentit.", ChimereStatus.Slowed),
                Move("anchor", "Marque d'ancrage", ChimereMoveKind.Control, 0, 22, "Prépare la capture.", ChimereStatus.Marked),
                Move("pulse", "Impulsion spectrale", ChimereMoveKind.Damage, 15, 5, "Attaque rapide.")
            });
            return c;
        }

        public static ChimereCombatant CreateCerfEcorce(int level = 2)
        {
            var c = Base("cerf_ecorce", "Cerf d'Écorce", ChimereAffinity.Organic, ChimereCombatRole.Guardian, level, 100, 20, 20, 14);
            c.PassiveName = "Écorce vivante";
            c.PassiveDescription = "Très grande endurance.";
            c.Moves.AddRange(new[]
            {
                Move("antler", "Bois fractal", ChimereMoveKind.Damage, 17, 7, "Charge frontale."),
                Move("root", "Racines d'arrêt", ChimereMoveKind.Control, 3, 16, "Ralentit.", ChimereStatus.Slowed),
                Move("bark", "Écorce réflexe", ChimereMoveKind.Support, 10, 8, "Régénération courte."),
                Move("spore", "Spore docile", ChimereMoveKind.StabilityBreak, 6, 18, "Apaise la cible.")
            });
            return c;
        }

        public static ChimereCombatant CreateVoltac(int level = 3)
        {
            var c = Base("voltac", "Voltac", ChimereAffinity.Electric, ChimereCombatRole.Assault, level, 76, 27, 11, 24);
            c.PassiveName = "Surcharge";
            c.PassiveDescription = "Rapide et dangereux contre les Chimères hydriques.";
            c.Moves.AddRange(new[]
            {
                Move("arc", "Arc statique", ChimereMoveKind.Damage, 20, 6, "Décharge concentrée."),
                Move("surge", "Surtension", ChimereMoveKind.StabilityBreak, 9, 21, "Dérègle les réflexes."),
                Move("flash", "Flash synaptique", ChimereMoveKind.Control, 2, 12, "Étourdit.", ChimereStatus.Stunned),
                Move("charge", "Recharge", ChimereMoveKind.Support, 9, 6, "Récupère de l'intégrité.")
            });
            return c;
        }

        public static ChimereCombatant CreateHydrune(int level = 3)
        {
            var c = Base("hydrune", "Hydrune", ChimereAffinity.Hydro, ChimereCombatRole.Support, level, 86, 18, 16, 17);
            c.PassiveName = "Cycle fermé";
            c.PassiveDescription = "Support durable, efficace contre le thermique.";
            c.Moves.AddRange(new[]
            {
                Move("jet", "Jet sous pression", ChimereMoveKind.Damage, 16, 8, "Impact liquide."),
                Move("wash", "Onde de lavage", ChimereMoveKind.StabilityBreak, 6, 18, "Déloge les défenses."),
                Move("veil", "Voile d'eau", ChimereMoveKind.Support, 12, 8, "Restaure intégrité et stabilité."),
                Move("drag", "Courant inverse", ChimereMoveKind.Control, 3, 14, "Ralentit.", ChimereStatus.Slowed)
            });
            return c;
        }

        public static ChimereCombatant CreateCendrex(int level = 4)
        {
            var c = Base("cendrex", "Cendrex", ChimereAffinity.Thermal, ChimereCombatRole.Assault, level, 82, 29, 12, 18);
            c.PassiveName = "Cœur incandescent";
            c.PassiveDescription = "Très offensif contre l'organique.";
            c.Moves.AddRange(new[]
            {
                Move("ember", "Croc de braise", ChimereMoveKind.Damage, 22, 5, "Frappe thermique."),
                Move("heat", "Vague sèche", ChimereMoveKind.StabilityBreak, 8, 19, "Sature les capteurs."),
                Move("smoke", "Fumée noire", ChimereMoveKind.Control, 2, 11, "Affaiblit.", ChimereStatus.Weakened),
                Move("furnace", "Four interne", ChimereMoveKind.Support, 8, 5, "Se régénère.")
            });
            return c;
        }

        public static ChimereCombatant CreateMycoryx(int level = 3)
        {
            var c = Base("mycoryx", "Mycoryx", ChimereAffinity.Toxic, ChimereCombatRole.Control, level, 88, 19, 15, 15);
            c.PassiveName = "Nuage de spores";
            c.PassiveDescription = "Contrôle la stabilité des organismes.";
            c.Moves.AddRange(new[]
            {
                Move("bite", "Morsure fongique", ChimereMoveKind.Damage, 15, 8, "Attaque organique toxique."),
                Move("spores", "Spore nerveuse", ChimereMoveKind.StabilityBreak, 6, 23, "Brise la stabilité."),
                Move("mold", "Moisissure lente", ChimereMoveKind.Control, 2, 15, "Affaiblit.", ChimereStatus.Weakened),
                Move("symbio", "Symbiose", ChimereMoveKind.Support, 11, 7, "Régénération.")
            });
            return c;
        }

        public static ChimereCombatant CreatePrismole(int level = 4)
        {
            var c = Base("prismole", "Prismole", ChimereAffinity.Radiant, ChimereCombatRole.Capturer, level, 70, 21, 13, 23);
            c.PassiveName = "Spectre pur";
            c.PassiveDescription = "Excellent contre les formes spectrales.";
            c.Moves.AddRange(new[]
            {
                Move("ray", "Rayon prismatique", ChimereMoveKind.Damage, 18, 7, "Trait lumineux."),
                Move("focus", "Focalisation", ChimereMoveKind.StabilityBreak, 5, 22, "Concentre la pression."),
                Move("seal", "Sceau blanc", ChimereMoveKind.Control, 0, 20, "Marque pour capture.", ChimereStatus.Marked),
                Move("halo", "Halo", ChimereMoveKind.Support, 10, 10, "Stabilise le compagnon.")
            });
            return c;
        }

        public static ChimereCombatant CreateFerrale(int level = 3)
        {
            var c = Base("ferrale", "Ferrale", ChimereAffinity.Mineral, ChimereCombatRole.Guardian, level, 108, 19, 24, 9);
            c.PassiveName = "Masse rocheuse";
            c.PassiveDescription = "Défense extrême, lente mais fiable.";
            c.Moves.AddRange(new[]
            {
                Move("slam", "Écrasement", ChimereMoveKind.Damage, 19, 8, "Impact lourd."),
                Move("crack", "Faille", ChimereMoveKind.StabilityBreak, 9, 20, "Brise les appuis."),
                Move("wall", "Mur minéral", ChimereMoveKind.Support, 10, 9, "Renforce l'intégrité."),
                Move("dust", "Poussière dense", ChimereMoveKind.Control, 1, 12, "Ralentit.", ChimereStatus.Slowed)
            });
            return c;
        }

        public static ChimereCombatant CreateBySpecies(string speciesId, int level = 1)
        {
            return speciesId switch
            {
                "mordrail" => CreateMordrail(level),
                "nebuli" => CreateNebuli(level),
                "cerf_ecorce" => CreateCerfEcorce(level),
                "voltac" => CreateVoltac(level),
                "hydrune" => CreateHydrune(level),
                "cendrex" => CreateCendrex(level),
                "mycoryx" => CreateMycoryx(level),
                "prismole" => CreatePrismole(level),
                "ferrale" => CreateFerrale(level),
                _ => CreateNebuli(level)
            };
        }

        public static ChimereCombatant CreateWildFromContext(string context, int seed)
        {
            var random = new Random(seed);
            int level = random.Next(1, 7);
            string normalized = (context ?? string.Empty).ToLowerInvariant();

            if (normalized.Contains("industrial") || normalized.Contains("railway"))
            {
                return random.Next(0, 3) switch
                {
                    0 => CreateMordrail(level),
                    1 => CreateVoltac(level),
                    _ => CreateFerrale(level)
                };
            }

            if (normalized.Contains("park") || normalized.Contains("wood") || normalized.Contains("natural"))
            {
                return random.Next(0, 3) switch
                {
                    0 => CreateCerfEcorce(level),
                    1 => CreateMycoryx(level),
                    _ => CreateHydrune(level)
                };
            }

            return random.Next(0, 4) switch
            {
                0 => CreateNebuli(level),
                1 => CreateCendrex(level),
                2 => CreatePrismole(level),
                _ => CreateVoltac(level)
            };
        }

        private static ChimereCombatant Base(
            string speciesId,
            string name,
            ChimereAffinity affinity,
            ChimereCombatRole role,
            int level,
            int hp,
            int attack,
            int defense,
            int speed)
        {
            int levelBonus = Math.Max(0, level - 1);

            return new ChimereCombatant
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                SpeciesId = speciesId,
                Name = name,
                Affinity = affinity,
                Role = role,
                Level = level,
                MaxHp = hp + levelBonus * 5,
                CurrentHp = hp + levelBonus * 5,
                Attack = attack + levelBonus * 2,
                Defense = defense + levelBonus * 2,
                Speed = speed + levelBonus,
                MaxStability = 100 + levelBonus * 3,
                Stability = 100 + levelBonus * 3
            };
        }

        private static ChimereMove Move(
            string id,
            string name,
            ChimereMoveKind kind,
            int power,
            int stability,
            string description,
            ChimereStatus status = ChimereStatus.None)
        {
            return new ChimereMove
            {
                Id = id,
                Name = name,
                Kind = kind,
                Power = power,
                StabilityPower = stability,
                Description = description,
                AppliesStatus = status
            };
        }
    }
}
