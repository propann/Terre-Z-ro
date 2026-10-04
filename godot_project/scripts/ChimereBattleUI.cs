using System;
using Godot;
using TerreZero.Chimeres;

namespace TerreZero.UI
{
    public partial class ChimereBattleUI : CanvasLayer
    {
        public event Action BattleClosed;
        public event Action<ChimereCombatant> ChimereCaptured;

        private Label _enemyName;
        private Label _playerName;
        private Label _enemyStats;
        private Label _playerStats;
        private ProgressBar _enemyHp;
        private ProgressBar _enemyStability;
        private ProgressBar _playerHp;
        private Label _log;
        private Label _captureChance;
        private Button _capture;
        private Button _flee;
        private Button[] _moveButtons;
        private Button[] _switchButtons;
        private ChimereBattlePortrait _enemyPortrait;
        private ChimereBattlePortrait _playerPortrait;
        private Control _root;
        private ColorRect _backdrop;

        public bool EncounterResolved { get; private set; }

        private ChimereCombatEngine _engine;
        private ChimereCombatant _wild;
        private ChimereCombatant _active;
        private bool _busy;

        public override void _Ready()
        {
            _enemyName = GetNode<Label>("%EnemyName");
            _playerName = GetNode<Label>("%PlayerName");
            _enemyStats = GetNode<Label>("%EnemyStats");
            _playerStats = GetNode<Label>("%PlayerStats");
            _enemyHp = GetNode<ProgressBar>("%EnemyHP");
            _enemyStability = GetNode<ProgressBar>("%EnemyStability");
            _playerHp = GetNode<ProgressBar>("%PlayerHP");
            _log = GetNode<Label>("%BattleLog");
            _captureChance = GetNode<Label>("%CaptureChance");
            _capture = GetNode<Button>("%Capture");
            _flee = GetNode<Button>("%Flee");
            _enemyPortrait = GetNode<ChimereBattlePortrait>("%EnemyPortrait");
            _playerPortrait = GetNode<ChimereBattlePortrait>("%PlayerPortrait");
            _root = GetNode<Control>("Root");
            _backdrop = GetNode<ColorRect>("Backdrop");

            _moveButtons = new[]
            {
                GetNode<Button>("%Move1"),
                GetNode<Button>("%Move2"),
                GetNode<Button>("%Move3"),
                GetNode<Button>("%Move4")
            };

            for (int i = 0; i < _moveButtons.Length; i++)
            {
                int index = i;
                _moveButtons[i].Pressed += () => OnMovePressed(index);
            }

            _switchButtons = new[]
            {
                GetNode<Button>("%Switch1"),
                GetNode<Button>("%Switch2"),
                GetNode<Button>("%Switch3")
            };

            for (int i = 0; i < _switchButtons.Length; i++)
            {
                int index = i;
                _switchButtons[i].Pressed += () => OnSwitchPressed(index);
            }

            _capture.Pressed += OnCapturePressed;
            _flee.Pressed += CloseBattle;
            Visible = false;
        }

        public void StartBattle(ChimereCombatant wild, int seed)
        {
            _active = ChimereGameState.Roster.FirstAvailable();
            if (_active == null || wild == null)
                return;

            _wild = wild.Clone();
            _engine = new ChimereCombatEngine(seed);
            EncounterResolved = false;
            _busy = false;
            Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _log.Text = $"Une Chimère sauvage apparaît : {_wild.Name}.";
            Refresh();
            PlayEnterTransition();
        }

        private async void OnMovePressed(int index)
        {
            if (_busy || _wild == null || _active == null || index >= _active.Moves.Count)
                return;

            _busy = true;
            SetButtonsEnabled(false);

            BattleActionResult playerResult =
                _engine.ExecuteMove(_active, _wild, _active.Moves[index]);

            _log.Text = playerResult.Message;
            _enemyPortrait.HitFlash();
            Refresh();

            if (_wild.IsDefeated)
            {
                ResolveVictory();
                return;
            }

            await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
            ExecuteEnemyTurn();

            if (_active.IsDefeated)
            {
                ChimereCombatant next = ChimereGameState.Roster.FirstAvailable();
                if (next == null)
                {
                    _log.Text = "Votre escouade est hors combat.";
                    _flee.Text = "REPLI";
                    _flee.Disabled = false;
                    return;
                }

                _active = next;
                _log.Text += $"\n{_active.Name} prend le relais.";
            }

            _busy = false;
            SetButtonsEnabled(true);
            Refresh();
        }

        private void ExecuteEnemyTurn()
        {
            if (_wild.Moves.Count == 0 || _wild.IsDefeated)
                return;

            int moveIndex = Math.Abs(
                (_wild.CurrentHp + _wild.Stability + _wild.Level) % _wild.Moves.Count
            );

            var move = _wild.Moves[moveIndex];
            BattleActionResult result = _engine.ExecuteMove(_wild, _active, move);
            _playerPortrait.HitFlash();
            _log.Text += $"\n{result.Message}";
        }

        private void OnCapturePressed()
        {
            if (_busy || _wild == null || _active == null)
                return;

            if (ChimereGameState.CaptureModules <= 0)
            {
                _log.Text = "Aucun module de capture disponible.";
                return;
            }

            ChimereGameState.CaptureModules--;
            CaptureResult result = _engine.AttemptCapture(_wild, _active);
            _log.Text = $"{result.Message}  ({result.Chance * 100f:F0} %)";

            if (result.Success)
            {
                ChimereGameState.Roster.Capture(_wild);
                _active.Bond = Math.Min(100, _active.Bond + 4);
                ChimereSaveStore.Save();
                EncounterResolved = true;
                ChimereCaptured?.Invoke(_wild);
                SetButtonsEnabled(false);
                _capture.Disabled = true;
                _flee.Text = "CONTINUER";
                _flee.Disabled = false;
                return;
            }

            ExecuteEnemyTurn();
            Refresh();
        }

        private void OnSwitchPressed(int index)
        {
            if (_busy || index < 0 || index >= ChimereGameState.Roster.Team.Count)
                return;

            ChimereCombatant target = ChimereGameState.Roster.Team[index];
            if (target.IsDefeated || target.Id == _active.Id)
                return;

            _busy = true;
            ChimereGameState.Roster.SetActive(target.Id);
            _active = ChimereGameState.Roster.Active;
            _log.Text = $"{_active.Name} entre en synchronisation.";

            ExecuteEnemyTurn();

            _busy = false;
            Refresh();
        }

        private void ResolveVictory()
        {
            int rarityBonus = _wild.Rarity switch
            {
                ChimereRarity.Rare => 12,
                ChimereRarity.Alpha => 30,
                _ => 0
            };
            int xp = 18 + _wild.Level * 9 + rarityBonus;
            bool levelUp = ChimereCombatEngine.GrantExperience(_active, xp);
            _active.Bond = Math.Min(100, _active.Bond + 2);
            ChimereSaveStore.Save();
            EncounterResolved = true;

            _log.Text =
                $"{_wild.Name} est neutralisée. +{xp} XP." +
                (levelUp ? $" {_active.Name} monte niveau {_active.Level} !" : "");

            SetButtonsEnabled(false);
            _capture.Disabled = true;
            _flee.Text = "CONTINUER";
            _flee.Disabled = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_wild == null || _active == null)
                return;

            _enemyName.Text = _wild.Name.ToUpperInvariant();
            _playerName.Text = _active.Name.ToUpperInvariant();
            _enemyStats.Text =
                $"NIV {_wild.Level:00}  //  {_wild.Affinity.ToString().ToUpperInvariant()}  //  {_wild.Rarity.ToString().ToUpperInvariant()}";
            _playerStats.Text = $"NIV {_active.Level:00}  //  LIEN {_active.Bond:00}";

            _enemyHp.MaxValue = _wild.MaxHp;
            _enemyHp.Value = _wild.CurrentHp;
            _enemyStability.MaxValue = _wild.MaxStability;
            _enemyStability.Value = _wild.Stability;
            _playerHp.MaxValue = _active.MaxHp;
            _playerHp.Value = _active.CurrentHp;

            _enemyPortrait.Configure(_wild, true);
            _playerPortrait.Configure(_active, false);

            for (int i = 0; i < _switchButtons.Length; i++)
            {
                if (i < ChimereGameState.Roster.Team.Count)
                {
                    ChimereCombatant member = ChimereGameState.Roster.Team[i];
                    _switchButtons[i].Visible = true;
                    _switchButtons[i].Text =
                        $"{member.Name.ToUpperInvariant()}  NIV {member.Level}";
                    _switchButtons[i].Disabled =
                        member.IsDefeated || member.Id == _active.Id || _busy;
                }
                else
                {
                    _switchButtons[i].Visible = false;
                }
            }

            for (int i = 0; i < _moveButtons.Length; i++)
            {
                if (i < _active.Moves.Count)
                {
                    var move = _active.Moves[i];
                    _moveButtons[i].Visible = true;
                    _moveButtons[i].Text =
                        $"{move.Name.ToUpperInvariant()}\n{move.Description}";
                }
                else
                {
                    _moveButtons[i].Visible = false;
                }
            }

            float preview = PreviewCaptureChance();
            _captureChance.Text =
                $"CAPTURE {preview * 100f:F0}%  //  MODULES {ChimereGameState.CaptureModules}";
            _capture.Disabled =
                _wild.IsDefeated || ChimereGameState.CaptureModules <= 0;
        }

        private float PreviewCaptureChance()
        {
            float hpFactor =
                1f - _wild.CurrentHp / (float)Math.Max(1, _wild.MaxHp);
            float stabilityFactor =
                1f - _wild.Stability / (float)Math.Max(1, _wild.MaxStability);
            float statusBonus =
                _wild.Status is ChimereStatus.Marked or ChimereStatus.Stunned
                    ? 0.16f
                    : 0f;
            float roleBonus =
                _active.Role == ChimereCombatRole.Capturer ? 0.12f : 0f;
            float bondBonus = Math.Min(0.10f, _active.Bond / 1000f);
            float levelPenalty =
                Math.Max(0f, _wild.Level - _active.Level) * 0.025f;

            return Mathf.Clamp(
                0.08f +
                hpFactor * 0.30f +
                stabilityFactor * 0.38f +
                statusBonus +
                roleBonus +
                bondBonus -
                levelPenalty,
                0.05f,
                0.95f
            );
        }

        private void PlayEnterTransition()
        {
            _root.Modulate = new Color(1, 1, 1, 0);
            _root.Scale = new Vector2(1.04f, 1.04f);
            _root.PivotOffset = _root.Size * 0.5f;
            _backdrop.Color = new Color(0.95f, 0.72f, 0.30f, 0.92f);

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(_root, "modulate:a", 1.0f, 0.22f);
            tween.TweenProperty(_root, "scale", Vector2.One, 0.28f);
            tween.TweenProperty(
                _backdrop,
                "color",
                new Color(0.01f, 0.015f, 0.02f, 0.88f),
                0.34f
            );
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (var button in _moveButtons)
                button.Disabled = !enabled;

            _capture.Disabled = !enabled;
            _flee.Disabled = !enabled;
        }

        private void CloseBattle()
        {
            Visible = false;
            BattleClosed?.Invoke();
        }
    }
}
