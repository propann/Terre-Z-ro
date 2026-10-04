using System;
using System.Collections.Generic;

namespace TerreZero.Chimeres
{
    public static class ChimereSpeciesCatalog
    {
        public static ChimereCombatant CreateMordrail(int level = 3)
        {
            var chimere = Base(
                "mordrail",
                "Mordrail",
                ChimereAffinity.Scrap,
                ChimereCombatRole.Breaker,
                level,
                hp: 92,
                attack: 25,
                defense: 18,
                speed: 12
            );

            chimere.PassiveName = "Mâchoire de chantier";
            chimere.PassiveDescription = "Les techniques Briseur infligent +10% de dégâts de stabilité.";
            chimere.Moves.AddRange(new[]
            {
                Move("ram", "Charge d'acier", ChimereMoveKind.Damage, 18, 6, "Percussion lourde."),
                Move("fang", "Croc d'impact", ChimereMoveKind.StabilityBreak, 10, 24, "Brise la stabilité."),
                Move("roar", "Hurlement brut", ChimereMoveKind.Control, 4, 10, "Affaiblit la cible.", ChimereStatus.Weakened),
                Move("lock", "Verrouillage", ChimereMoveKind.Control, 0, 18, "Marque la cible pour la capture.", ChimereStatus.Marked)
            });
            return chimere;
        }

        public static ChimereCombatant CreateNebuli(int level = 3)
        {
            var chimere = Base(
                "nebuli",
                "Nébuli",
                ChimereAffinity.Spectral,
                ChimereCombatRole.Capturer,
                level,
                hp: 72,
                attack: 18,
                defense: 12,
                speed: 21
            );

            chimere.PassiveName = "Résonance calme";
            chimere.PassiveDescription = "Bonus de capture sur les cibles marquées.";
            chimere.Moves.AddRange(new[]
            {
                Move("wave", "Onde calme", ChimereMoveKind.StabilityBreak, 7, 20, "Désynchronise la cible."),
                Move("mist", "Brume liée", ChimereMoveKind.Control, 3, 12, "Ralentit l'adversaire.", ChimereStatus.Slowed),
                Move("anchor", "Marque d'ancrage", ChimereMoveKind.Control, 0, 22, "Prépare la capture.", ChimereStatus.Marked),
                Move("pulse", "Impulsion spectrale", ChimereMoveKind.Damage, 15, 5, "Attaque rapide.")
            });
            return chimere;
        }

        public static ChimereCombatant CreateCerfEcorce(int level = 2)
        {
            var chimere = Base(
                "cerf_ecorce",
                "Cerf d'Écorce",
                ChimereAffinity.Organic,
                ChimereCombatRole.Guardian,
                level,
                hp: 100,
                attack: 20,
                defense: 20,
                speed: 14
            );

            chimere.PassiveName = "Écorce vivante";
            chimere.PassiveDescription = "Résiste mieux aux premières frappes.";
            chimere.Moves.AddRange(new[]
            {
                Move("antler", "Bois fractal", ChimereMoveKind.Damage, 17, 7, "Charge frontale."),
                Move("root", "Racines d'arrêt", ChimereMoveKind.Control, 3, 16, "Bloque la mobilité.", ChimereStatus.Slowed),
                Move("bark", "Écorce réflexe", ChimereMoveKind.Support, 10, 8, "Régénération courte."),
                Move("spore", "Spore docile", ChimereMoveKind.StabilityBreak, 6, 18, "Apaise la cible.")
            });
            return chimere;
        }

        public static ChimereCombatant CreateWildFromContext(string context, int seed)
        {
            var random = new Random(seed);
            int level = random.Next(1, 6);
            string normalized = (context ?? string.Empty).ToLowerInvariant();

            if (normalized.Contains("industrial") || normalized.Contains("railway"))
                return CreateMordrail(level);

            if (normalized.Contains("park") || normalized.Contains("wood") || normalized.Contains("natural"))
                return CreateCerfEcorce(level);

            return random.Next(0, 2) == 0
                ? CreateNebuli(level)
                : CreateMordrail(level);
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
                Stability = 100 + levelBonus * 3,
                Bond = 0,
                Training = 0
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
