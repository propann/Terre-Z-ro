using Godot;
using TerreZero.World.Voxel;

namespace TerreZero.UI
{
    public partial class GameHUD : CanvasLayer
    {
        private Label _sector;
        private Label _mode;
        private Label _material;
        private Label _hint;

        public override void _Ready()
        {
            _sector = GetNode<Label>("%Sector");
            _mode = GetNode<Label>("%Mode");
            _material = GetNode<Label>("%Material");
            _hint = GetNode<Label>("%Hint");
            SetSector("LOCAL / INITIALISATION");
            SetMaterial(VoxelMaterial.SteelBarricade);
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

        public void SetHint(string text)
        {
            if (_hint != null)
                _hint.Text = text;
        }
    }
}
