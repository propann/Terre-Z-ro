using System;
using System.Collections.Generic;
using System.Linq;

namespace TerreZero.Gameplay
{
    public enum QuestState
    {
        Locked,
        Active,
        Completed,
        Failed
    }

    public enum QuestObjectiveType
    {
        CaptureChimere,
        WinBattle,
        CollectItem,
        ExploreBuilding,
        ReachDistance
    }

    public sealed class QuestObjective
    {
        public string Id { get; set; } = string.Empty;
        public QuestObjectiveType Type { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public int Required { get; set; } = 1;
        public int Progress { get; set; }

        public bool Completed => Progress >= Required;
    }

    public sealed class QuestRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QuestState State { get; set; } = QuestState.Active;
        public List<QuestObjective> Objectives { get; set; } = new();

        public bool ObjectivesComplete =>
            Objectives.Count > 0 && Objectives.All(o => o.Completed);
    }

    public sealed class QuestJournalState
    {
        public List<QuestRecord> Quests { get; set; } = new();

        public QuestRecord Get(string questId) =>
            Quests.FirstOrDefault(q => q.Id == questId);

        public bool Add(QuestRecord quest)
        {
            if (quest == null ||
                string.IsNullOrWhiteSpace(quest.Id) ||
                Get(quest.Id) != null)
                return false;

            Quests.Add(quest);
            return true;
        }

        public void RegisterProgress(
            QuestObjectiveType type,
            string targetId = "",
            int amount = 1)
        {
            foreach (var quest in Quests)
            {
                if (quest.State != QuestState.Active)
                    continue;

                foreach (var objective in quest.Objectives)
                {
                    if (objective.Type != type || objective.Completed)
                        continue;

                    if (!string.IsNullOrWhiteSpace(objective.TargetId) &&
                        !string.Equals(
                            objective.TargetId,
                            targetId,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    objective.Progress = Math.Min(
                        objective.Required,
                        objective.Progress + Math.Max(0, amount)
                    );
                }

                if (quest.ObjectivesComplete)
                    quest.State = QuestState.Completed;
            }
        }

        public void SeedIntroQuests()
        {
            if (Quests.Count > 0)
                return;

            Add(new QuestRecord
            {
                Id = "first_contact",
                Title = "Premier contact",
                Description = "Comprendre les Chimères et établir un premier lien.",
                Objectives = new List<QuestObjective>
                {
                    new()
                    {
                        Id = "capture_one",
                        Type = QuestObjectiveType.CaptureChimere,
                        Required = 1
                    }
                }
            });

            Add(new QuestRecord
            {
                Id = "field_salvage",
                Title = "Récupération de terrain",
                Description = "Ramener des ressources pour préparer la survie.",
                Objectives = new List<QuestObjective>
                {
                    new()
                    {
                        Id = "scrap_five",
                        Type = QuestObjectiveType.CollectItem,
                        TargetId = "scrap",
                        Required = 5
                    }
                }
            });
        }
    }
}
