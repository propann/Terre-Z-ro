using System;
using Godot;

namespace TerreZero.UI
{
    public partial class TacticalMapSurface : Control
    {
        private int _radiusKm = 10;
        private Vector2 _selected = new(0.5f, 0.5f);
        private bool _hasSelection;
        private Color _accent = new Color("e8a23a");
        private Color _background = new Color("111820");

        public void Configure(
            int radiusKm,
            Vector2 selected,
            bool hasSelection,
            Color accent,
            Color background)
        {
            _radiusKm = radiusKm;
            _selected = selected;
            _hasSelection = hasSelection;
            _accent = accent;
            _background = background;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 size = Size;
            DrawRect(new Rect2(Vector2.Zero, size), _background);

            Color grid = new Color(0.34f, 0.45f, 0.48f, 0.16f);
            for (int i = 0; i <= 12; i++)
            {
                float x = size.X * i / 12f;
                float y = size.Y * i / 12f;
                DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), grid, 1);
                DrawLine(new Vector2(0, y), new Vector2(size.X, y), grid, 1);
            }

            Vector2 center = size * 0.5f;
            float radius = Math.Min(size.X, size.Y) * 0.42f;

            DrawCircle(center, radius, new Color(_accent, 0.055f));
            DrawArc(center, radius, 0, Mathf.Tau, 96, new Color(_accent, 0.7f), 2);
            DrawArc(center, radius * 0.5f, 0, Mathf.Tau, 96, new Color(_accent, 0.25f), 1);

            DrawLine(center - new Vector2(16, 0), center + new Vector2(16, 0), _accent, 1.5f);
            DrawLine(center - new Vector2(0, 16), center + new Vector2(0, 16), _accent, 1.5f);
            DrawCircle(center, 5, _accent);

            if (_hasSelection)
            {
                Vector2 point = new Vector2(
                    _selected.X * size.X,
                    _selected.Y * size.Y
                );

                DrawLine(center, point, new Color(_accent, 0.5f), 1.5f);
                DrawCircle(point, 10, new Color(0.06f, 0.08f, 0.09f, 1));
                DrawCircle(point, 6, _accent);
            }

            var font = ThemeDB.FallbackFont;
            DrawString(
                font,
                new Vector2(18, 28),
                $"ZONE AUTORISÉE  •  {_radiusKm} KM",
                HorizontalAlignment.Left,
                -1,
                15,
                new Color(0.78f, 0.84f, 0.84f, 0.9f)
            );
            DrawString(
                font,
                new Vector2(18, size.Y - 16),
                "CLIQUEZ DANS LE PÉRIMÈTRE",
                HorizontalAlignment.Left,
                -1,
                13,
                new Color(0.55f, 0.65f, 0.66f, 0.9f)
            );
        }
    }
}
