using System;
using System.Collections.Generic;
using System.Linq;

namespace TerreZero.Chimeres
{
    public sealed class ChimereRoster
    {
        public const int MaxTeamSize = 3;

        private readonly List<ChimereCombatant> _team = new();
        private readonly List<ChimereCombatant> _reserve = new();

        public IReadOnlyList<ChimereCombatant> Team => _team;
        public IReadOnlyList<ChimereCombatant> Reserve => _reserve;
        public ChimereCombatant Active => _team.Count > 0 ? _team[0] : null;

        public ChimereRoster()
        {
            _team.Add(ChimereSpeciesCatalog.CreateNebuli(3));
        }

        public bool Capture(ChimereCombatant chimere)
        {
            if (chimere == null)
                return false;

            chimere.CurrentHp = chimere.MaxHp;
            chimere.Stability = chimere.MaxStability;
            chimere.Status = ChimereStatus.None;
            chimere.Bond = Math.Max(chimere.Bond, 5);

            if (_team.Count < MaxTeamSize)
                _team.Add(chimere);
            else
                _reserve.Add(chimere);

            return true;
        }

        public bool SetActive(string chimereId)
        {
            int index = _team.FindIndex(c => c.Id == chimereId);
            if (index <= 0)
                return index == 0;

            var selected = _team[index];
            _team.RemoveAt(index);
            _team.Insert(0, selected);
            return true;
        }

        public bool SwapWithReserve(string teamId, string reserveId)
        {
            int teamIndex = _team.FindIndex(c => c.Id == teamId);
            int reserveIndex = _reserve.FindIndex(c => c.Id == reserveId);

            if (teamIndex < 0 || reserveIndex < 0)
                return false;

            var tmp = _team[teamIndex];
            _team[teamIndex] = _reserve[reserveIndex];
            _reserve[reserveIndex] = tmp;
            return true;
        }

        public void HealTeam()
        {
            foreach (var chimere in _team)
            {
                chimere.CurrentHp = chimere.MaxHp;
                chimere.Stability = chimere.MaxStability;
                chimere.Status = ChimereStatus.None;
            }
        }

        public ChimereCombatant FirstAvailable() =>
            _team.FirstOrDefault(c => !c.IsDefeated);
    }

    public static class ChimereGameState
    {
        public static ChimereRoster Roster { get; } = new();

        public static int CaptureModules { get; set; } = 8;
        public static int TrainingPoints { get; set; } = 6;

        public static bool TryTrain(string chimereId, int intensity = 1)
        {
            var chimere = Roster.Team.Concat(Roster.Reserve)
                .FirstOrDefault(c => c.Id == chimereId);

            if (chimere == null)
                return false;

            int cost = Math.Clamp(intensity, 1, 3);
            if (TrainingPoints < cost)
                return false;

            TrainingPoints -= cost;
            ChimereCombatEngine.Train(chimere, cost);
            return true;
        }
    }
}
