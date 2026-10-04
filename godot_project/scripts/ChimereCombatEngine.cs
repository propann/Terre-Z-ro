using System;
using Godot;

namespace TerreZero.Chimeres
{
    public sealed class ChimereCombatEngine
    {
        private readonly Random _random;

        public ChimereCombatEngine(int seed)
        {
            _random = new Random(seed);
        }

        public BattleActionResult ExecuteMove(
            ChimereCombatant attacker,
            ChimereCombatant defender,
            ChimereMove move)
        {
            if (attacker.IsDefeated || defender.IsDefeated)
                return new BattleActionResult("Aucune action possible.", 0, 0, ChimereStatus.None);

            float variance = 0.90f + (float)_random.NextDouble() * 0.20f;
            float levelFactor = 1f + Math.Max(0, attacker.Level - defender.Level) * 0.025f;
            float affinityFactor = GetAffinityMultiplier(attacker.Affinity, defender.Affinity);

            int hpDamage = 0;
            int stabilityDamage = 0;

            switch (move.Kind)
            {
                case ChimereMoveKind.Damage:
                    hpDamage = Math.Max(
                        1,
                        Mathf.RoundToInt(
                            (move.Power + attacker.Attack * 0.70f - defender.Defense * 0.35f)
                            * variance
                            * levelFactor
                            * affinityFactor
                        )
                    );
                    stabilityDamage = Math.Max(2, move.StabilityPower);
                    break;

                case ChimereMoveKind.StabilityBreak:
                    hpDamage = Math.Max(
                        1,
                        Mathf.RoundToInt(
                            (move.Power + attacker.Attack * 0.35f - defender.Defense * 0.20f)
                            * variance
                        )
                    );
                    stabilityDamage = Math.Max(
                        4,
                        Mathf.RoundToInt((move.StabilityPower + attacker.Training * 0.15f) * variance)
                    );
                    break;

                case ChimereMoveKind.Control:
                    hpDamage = Math.Max(0, Mathf.RoundToInt(move.Power * 0.35f * variance));
                    stabilityDamage = Math.Max(3, move.StabilityPower);
                    break;

                case ChimereMoveKind.Support:
                    int heal = Math.Max(4, move.Power + attacker.Bond / 10);
                    attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
                    attacker.Stability = Math.Min(attacker.MaxStability, attacker.Stability + move.StabilityPower);
                    return new BattleActionResult(
                        $"{attacker.Name} utilise {move.Name} et récupère {heal} PV.",
                        -heal,
                        -move.StabilityPower,
                        ChimereStatus.None
                    );
            }

            if (defender.Status == ChimereStatus.Guarded)
                hpDamage = Mathf.RoundToInt(hpDamage * 0.65f);

            defender.CurrentHp = Math.Max(0, defender.CurrentHp - hpDamage);
            defender.Stability = Math.Max(0, defender.Stability - stabilityDamage);

            if (move.AppliesStatus != ChimereStatus.None && !defender.IsDefeated)
                defender.Status = move.AppliesStatus;

            string message =
                $"{attacker.Name} utilise {move.Name} : -{hpDamage} PV / -{stabilityDamage} stabilité.";

            return new BattleActionResult(
                message,
                hpDamage,
                stabilityDamage,
                move.AppliesStatus
            );
        }

        public CaptureResult AttemptCapture(
            ChimereCombatant wild,
            ChimereCombatant active,
            float deviceQuality = 1f)
        {
            if (wild.IsDefeated)
                return new CaptureResult(false, 0f, "La Chimère est neutralisée : capture impossible.");

            float hpFactor = 1f - wild.CurrentHp / (float)Math.Max(1, wild.MaxHp);
            float stabilityFactor = 1f - wild.Stability / (float)Math.Max(1, wild.MaxStability);
            float statusBonus = wild.Status is ChimereStatus.Marked or ChimereStatus.Stunned ? 0.16f : 0f;
            float roleBonus = active.Role == ChimereCombatRole.Capturer ? 0.12f : 0f;
            float bondBonus = Math.Min(0.10f, active.Bond / 1000f);
            float levelPenalty = Math.Max(0f, wild.Level - active.Level) * 0.025f;

            float chance =
                0.08f +
                hpFactor * 0.30f +
                stabilityFactor * 0.38f +
                statusBonus +
                roleBonus +
                bondBonus -
                levelPenalty;

            chance = Mathf.Clamp(chance * deviceQuality, 0.05f, 0.95f);

            bool success = _random.NextDouble() <= chance;

            return new CaptureResult(
                success,
                chance,
                success
                    ? $"{wild.Name} est liée à votre escouade."
                    : $"{wild.Name} rejette le lien de capture."
            );
        }

        public static int ExperienceForLevel(int level) => 40 + level * level * 12;

        public static bool GrantExperience(ChimereCombatant chimere, int xp)
        {
            chimere.Experience += Math.Max(0, xp);
            bool leveled = false;

            while (chimere.Experience >= ExperienceForLevel(chimere.Level))
            {
                chimere.Experience -= ExperienceForLevel(chimere.Level);
                chimere.Level++;
                chimere.MaxHp += 6;
                chimere.CurrentHp = chimere.MaxHp;
                chimere.Attack += 2;
                chimere.Defense += 2;
                chimere.Speed += 1;
                chimere.MaxStability += 3;
                chimere.Stability = chimere.MaxStability;
                leveled = true;
            }

            return leveled;
        }

        public static bool TryEvolve(ChimereCombatant chimere)
        {
            if (chimere == null || chimere.EvolutionStage >= 2)
                return false;

            int requiredLevel = chimere.EvolutionStage == 0 ? 6 : 12;
            int requiredBond = chimere.EvolutionStage == 0 ? 25 : 60;
            int requiredTraining = chimere.EvolutionStage == 0 ? 18 : 45;

            if (chimere.Level < requiredLevel ||
                chimere.Bond < requiredBond ||
                chimere.Training < requiredTraining)
                return false;

            chimere.EvolutionStage++;
            chimere.MaxHp += chimere.EvolutionStage == 1 ? 18 : 30;
            chimere.CurrentHp = chimere.MaxHp;
            chimere.Attack += chimere.EvolutionStage == 1 ? 5 : 8;
            chimere.Defense += chimere.EvolutionStage == 1 ? 4 : 7;
            chimere.Speed += 2;
            chimere.MaxStability += chimere.EvolutionStage == 1 ? 8 : 12;
            chimere.Stability = chimere.MaxStability;

            chimere.Name = chimere.SpeciesId switch
            {
                "mordrail" => chimere.EvolutionStage == 1 ? "Mordrail Prime" : "Mordrail Titan",
                "nebuli" => chimere.EvolutionStage == 1 ? "Nébuli Synapse" : "Nébuli Astral",
                "cerf_ecorce" => chimere.EvolutionStage == 1 ? "Cerf Bastion" : "Cerf-Monde",
                _ => chimere.Name
            };

            return true;
        }

        private static float GetAffinityMultiplier(
            ChimereAffinity attacker,
            ChimereAffinity defender)
        {
            if (attacker == defender)
                return 0.90f;

            return (attacker, defender) switch
            {
                (ChimereAffinity.Electric, ChimereAffinity.Hydro) => 1.25f,
                (ChimereAffinity.Hydro, ChimereAffinity.Thermal) => 1.25f,
                (ChimereAffinity.Thermal, ChimereAffinity.Organic) => 1.25f,
                (ChimereAffinity.Organic, ChimereAffinity.Mineral) => 1.20f,
                (ChimereAffinity.Mineral, ChimereAffinity.Scrap) => 1.20f,
                (ChimereAffinity.Spectral, ChimereAffinity.Unstable) => 1.25f,
                (ChimereAffinity.Radiant, ChimereAffinity.Spectral) => 1.25f,
                (ChimereAffinity.Toxic, ChimereAffinity.Organic) => 1.20f,
                _ => 1.0f
            };
        }

        public static void Train(ChimereCombatant chimere, int intensity = 1)
        {
            int safeIntensity = Math.Clamp(intensity, 1, 3);
            chimere.Training += safeIntensity * 3;
            chimere.Bond = Math.Min(100, chimere.Bond + safeIntensity * 2);

            if (chimere.Training % 15 == 0)
            {
                chimere.Attack += 1;
                chimere.Defense += 1;
            }
        }
    }
}
