using Godot;
using TerreZero.Gameplay;
using TerreZero.World.Voxel;

namespace TerreZero.UI
{
    public partial class GameHUD : CanvasLayer
    {
        private Label _sector;
        private Label _mode;
        private Label _material;
        private Label _hint;
        private Label _survival;
        private Label _weather;

        public override void _Ready()
        {
            _sector = GetNode<Label>("%Sector");
            _mode = GetNode<Label>("%Mode");
            _material = GetNode<Label>("%Material");
            _hint = GetNode<Label>("%Hint");
            _survival = GetNodeOrNull<Label>("%Survival");
            _weather = GetNodeOrNull<Label>("%Weather");
            SetSector("LOCAL / INITIALISATION");
            SetMaterial(VoxelMaterial.SteelBarricade);
        }

        public override void _Process(double delta)
        {
            if (_survival == null)
                return;

            var p = GameState.Player;
            _survival.Text =
                $"PV {p.Health}/{p.MaxHealth}  •  END {p.Stamina}/{p.MaxStamina}  •  RAD {p.Radiation}%";
        }

        public void SetSector(string value)
        {
            if (_sector != null)
                _sector.Text = $"SECTEUR // {value}";
        }

        public void SetMaterial(VoxelMaterial material)
        {
            if (_material != null)
                _material.Text = $"MATÉRIAU  {material.ToString().ToUpperInvariant()}";
        }

        public void SetScanner(bool active)
        {
            if (_mode != null)
                _mode.Text = active ? "SCAN X-RAY // ACTIF" : "SURVIE // NORMAL";
        }

        public void SetWeather(string text)
        {
            if (_weather != null)
                _weather.Text = $"MÉTÉO // {text}";
        }

        public void SetHint(string text)
        {
            if (_hint != null)
                _hint.Text = text;
        }
    }
}
