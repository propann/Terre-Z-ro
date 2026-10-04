using System;
using Godot;
using TerreZero.Chimeres;

namespace TerreZero.UI
{
    public partial class ChimereTrainingUI : CanvasLayer
    {
        public event Action Closed;

        private VBoxContainer _teamList;
        private Label _points;
        private Label _details;
        private ChimereCombatant _selected;

        public override void _Ready()
        {
            _teamList = GetNode<VBoxContainer>("%TeamList");
            _points = GetNode<Label>("%TrainingPoints");
            _details = GetNode<Label>("%Details");
            GetNode<Button>("%Train1").Pressed += () => Train(1);
            GetNode<Button>("%Train2").Pressed += () => Train(2);
            GetNode<Button>("%Train3").Pressed += () => Train(3);
            GetNode<Button>("%Close").Pressed += Close;
            Visible = false;
        }

        public void Open()
        {
            Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            RebuildList();
        }

        private void RebuildList()
        {
            foreach (Node child in _teamList.GetChildren())
                child.QueueFree();

            foreach (var chimere in ChimereGameState.Roster.Team)
            {
                var button = new Button
                {
                    Text = $"{chimere.Name}  NIV {chimere.Level}  LIEN {chimere.Bond}",
                    CustomMinimumSize = new Vector2(300, 38)
                };
                button.Pressed += () => Select(chimere);
                _teamList.AddChild(button);
            }

            _points.Text = $"POINTS DE DRESSAGE : {ChimereGameState.TrainingPoints}";
            if (_selected == null)
                _selected = ChimereGameState.Roster.Active;
            RefreshDetails();
        }

        private void Select(ChimereCombatant chimere)
        {
            _selected = chimere;
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (_selected == null)
            {
                _details.Text = "Aucune Chimère.";
                return;
            }

            _details.Text =
                $"{_selected.Name.ToUpperInvariant()} // {_selected.Affinity}
" +
                $"Niveau {_selected.Level}   PV {_selected.MaxHp}   ATK {_selected.Attack}   DEF {_selected.Defense}
" +
                $"Lien {_selected.Bond}/100   Dressage {_selected.Training}

" +
                $"PASSIF : {_selected.PassiveName}
{_selected.PassiveDescription}";
        }

        private void Train(int intensity)
        {
            if (_selected == null)
                return;

            if (!ChimereGameState.TryTrain(_selected.Id, intensity))
            {
                _details.Text += "

POINTS INSUFFISANTS.";
                return;
            }

            RebuildList();
        }

        private void Close()
        {
            Visible = false;
            Closed?.Invoke();
        }
    }
}
