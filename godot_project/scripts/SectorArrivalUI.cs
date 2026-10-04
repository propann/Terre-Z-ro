using Godot;

namespace TerreZero.UI
{
    public partial class SectorArrivalUI : CanvasLayer
    {
        private PanelContainer _panel;
        private Label _title;
        private Label _details;

        public override void _Ready()
        {
            Layer = 40;

            _panel = new PanelContainer
            {
                Name = "SectorArrivalPanel",
                Position = new Vector2(420f, 34f),
                Size = new Vector2(440f, 94f),
                Modulate = new Color(1f, 1f, 1f, 0f)
            };
            AddChild(_panel);

            var box = new VBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Center
            };
            _panel.AddChild(box);

            _title = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "SECTEUR // —"
            };
            _title.AddThemeFontSizeOverride("font_size", 18);
            _title.AddThemeColorOverride(
                "font_color",
                new Color(0.92f, 0.64f, 0.24f)
            );
            box.AddChild(_title);

            _details = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "ANCRE // —"
            };
            _details.AddThemeFontSizeOverride("font_size", 12);
            _details.AddThemeColorOverride(
                "font_color",
                new Color(0.70f, 0.78f, 0.80f)
            );
            box.AddChild(_details);
        }

        public void ShowSector(
            string h3Index,
            double latitude,
            double longitude,
            string context)
        {
            if (_panel == null)
                return;

            _title.Text = $"SECTEUR // {h3Index}";
            _details.Text =
                $"{context.ToUpperInvariant()}  •  " +
                $"{latitude:F5}, {longitude:F5}";

            _panel.Modulate = new Color(1f, 1f, 1f, 0f);
            _panel.Scale = new Vector2(0.96f, 0.96f);
            _panel.PivotOffset = _panel.Size * 0.5f;

            Tween tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(
                _panel,
                "modulate:a",
                1f,
                0.18f
            );
            tween.Parallel().TweenProperty(
                _panel,
                "scale",
                Vector2.One,
                0.22f
            );
            tween.TweenInterval(2.4f);
            tween.SetEase(Tween.EaseType.In);
            tween.TweenProperty(
                _panel,
                "modulate:a",
                0f,
                0.42f
            );
        }
    }
}
