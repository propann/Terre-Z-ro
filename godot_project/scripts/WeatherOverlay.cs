using Godot;

namespace TerreZero.World.Weather
{
    public partial class WeatherOverlay : Control
    {
        private float _rainIntensity;
        private float _snowIntensity;
        private float _windSlant;
        private float _phase;

        public void Configure(float rain, float snow, float windSlant)
        {
            _rainIntensity = Mathf.Clamp(rain, 0f, 1f);
            _snowIntensity = Mathf.Clamp(snow, 0f, 1f);
            _windSlant = Mathf.Clamp(windSlant, -1f, 1f);
            Visible = _rainIntensity > 0.01f || _snowIntensity > 0.01f;
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (!Visible)
                return;

            _phase += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 size = Size;

            if (_rainIntensity > 0.01f)
            {
                int count = 24 + Mathf.RoundToInt(_rainIntensity * 70f);
                for (int i = 0; i < count; i++)
                {
                    float seed = i * 37.13f;
                    float x = Mathf.PosMod(seed * 13f + _phase * (110f + i % 7), size.X);
                    float y = Mathf.PosMod(seed * 29f + _phase * (280f + i % 11), size.Y);
                    float len = 10f + _rainIntensity * 18f;
                    Vector2 start = new(x, y);
                    Vector2 end = start + new Vector2(_windSlant * len * 0.7f, len);
                    DrawLine(
                        start,
                        end,
                        new Color(0.72f, 0.82f, 0.90f, 0.18f + _rainIntensity * 0.32f),
                        1.2f
                    );
                }
            }

            if (_snowIntensity > 0.01f)
            {
                int count = 18 + Mathf.RoundToInt(_snowIntensity * 55f);
                for (int i = 0; i < count; i++)
                {
                    float seed = i * 51.77f;
                    float x = Mathf.PosMod(seed * 9f + _phase * (22f + i % 5), size.X);
                    float y = Mathf.PosMod(seed * 17f + _phase * (38f + i % 7), size.Y);
                    float radius = 1.5f + (i % 3) * 0.7f;
                    DrawCircle(
                        new Vector2(x, y),
                        radius,
                        new Color(0.92f, 0.95f, 1f, 0.28f + _snowIntensity * 0.4f)
                    );
                }
            }
        }
    }
}
