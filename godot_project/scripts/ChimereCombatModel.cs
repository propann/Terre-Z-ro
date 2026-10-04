using System;
using System.Collections.Generic;

namespace TerreZero.Chimeres
{
    public enum ChimereAffinity
    {
        Organic,
        Scrap,
        Electric,
        Toxic,
        Spectral,
        Mineral,
        Thermal,
        Hydro,
        Unstable,
        Radiant
    }

    public enum ChimereRarity
    {
        Common,
        Rare,
        Alpha
    }

    public enum ChimereCombatRole
    {
        Assault,
        Control,
        Support,
        Breaker,
        Capturer,
        Guardian
    }

    public enum ChimereStatus
    {
        None,
        Marked,
        Stunned,
        Slowed,
        Weakened,
        Guarded
    }

    public enum ChimereMoveKind
    {
        Damage,
        StabilityBreak,
        Control,
        Support
    }

    public sealed class ChimereMove
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ChimereMoveKind Kind { get; init; }
        public int Power { get; init; }
        public int StabilityPower { get; init; }
        public ChimereStatus AppliesStatus { get; init; }
        public int Priority { get; init; }
    }

    public sealed class ChimereCombatant
    {
        public string Id { get; set; } = string.Empty;
        public string SpeciesId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ChimereAffinity Affinity { get; set; }
        public ChimereCombatRole Role { get; set; }
        public ChimereRarity Rarity { get; set; } = ChimereRarity.Common;
        public int Level { get; set; } = 1;
        public int Experience { get; set; }
        public int MaxHp { get; set; } = 80;
        public int CurrentHp { get; set; } = 80;
        public int Attack { get; set; } = 20;
        public int Defense { get; set; } = 12;
        public int Speed { get; set; } = 15;
        public int MaxStability { get; set; } = 100;
        public int Stability { get; set; } = 100;
        public int Bond { get; set; }
        public int Training { get; set; }
        public int EvolutionStage { get; set; }
        public ChimereStatus Status { get; set; }
        public string PassiveName { get; set; } = string.Empty;
        public string PassiveDescription { get; set; } = string.Empty;
        public List<ChimereMove> Moves { get; } = new();

        public bool IsDefeated => CurrentHp <= 0;
        public bool IsCaptureReady => !IsDefeated && Stability <= 35 && CurrentHp <= MaxHp * 0.55f;

        public ChimereCombatant Clone()
        {
            var clone = new ChimereCombatant
            {
                Id = Id,
                SpeciesId = SpeciesId,
                Name = Name,
                Affinity = Affinity,
                Role = Role,
                Rarity = Rarity,
                Level = Level,
                Experience = Experience,
                MaxHp = MaxHp,
                CurrentHp = CurrentHp,
                Attack = Attack,
                Defense = Defense,
                Speed = Speed,
                MaxStability = MaxStability,
                Stability = Stability,
                Bond = Bond,
                Training = Training,
                EvolutionStage = EvolutionStage,
                Status = Status,
                PassiveName = PassiveName,
                PassiveDescription = PassiveDescription
            };

            clone.Moves.AddRange(Moves);
            return clone;
        }
    }

    public readonly struct BattleActionResult
    {
        public string Message { get; }
        public int HpDamage { get; }
        public int StabilityDamage { get; }
        public ChimereStatus AppliedStatus { get; }

        public BattleActionResult(
            string message,
            int hpDamage,
            int stabilityDamage,
            ChimereStatus appliedStatus)
        {
            Message = message;
            HpDamage = hpDamage;
            StabilityDamage = stabilityDamage;
            AppliedStatus = appliedStatus;
        }
    }

    public readonly struct CaptureResult
    {
        public bool Success { get; }
        public float Chance { get; }
        public string Message { get; }

        public CaptureResult(bool success, float chance, string message)
        {
            Success = success;
            Chance = chance;
            Message = message;
        }
    }
}
