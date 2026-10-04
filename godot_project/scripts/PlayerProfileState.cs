using System;

namespace TerreZero.Gameplay
{
    public sealed class PlayerProfileState
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; }
        public int MaxHealth { get; set; } = 100;
        public int Health { get; set; } = 100;
        public int MaxStamina { get; set; } = 100;
        public int Stamina { get; set; } = 100;
        public int Radiation { get; set; }
        public int Credits { get; set; }
        public int MasteryPoints { get; set; }
        public double DistanceWalkedKm { get; set; }
        public int ChimeresCaptured { get; set; }
        public int BattlesWon { get; set; }
        public int BuildingsExplored { get; set; }

        public bool IsDown => Health <= 0;

        public void Heal(int amount)
        {
            Health = Math.Clamp(Health + Math.Max(0, amount), 0, MaxHealth);
        }

        public void Damage(int amount)
        {
            Health = Math.Clamp(Health - Math.Max(0, amount), 0, MaxHealth);
        }

        public void AddExperience(int xp)
        {
            Experience += Math.Max(0, xp);

            while (Experience >= ExperienceForLevel(Level))
            {
                Experience -= ExperienceForLevel(Level);
                Level++;
                MaxHealth += 5;
                MaxStamina += 3;
                Health = MaxHealth;
                Stamina = MaxStamina;
                MasteryPoints++;
            }
        }

        public static int ExperienceForLevel(int level) =>
            100 + Math.Max(1, level) * Math.Max(1, level) * 35;
    }
}
