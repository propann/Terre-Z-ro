using Godot;
using TerreZero.Chimeres;

namespace TerreZero.UI
{
    public partial class ChimereBattlePortrait : Control
    {
        private ChimereCombatant _chimere;
        private bool _enemy;
        private float _pulse;
        private float _flash;

        public override void _Process(double delta)
        {
            _pulse += (float)delta;
            _flash = Mathf.Max(0f, _flash - (float)delta * 4f);
            QueueRedraw();
        }

        public void Configure(ChimereCombatant chimere, bool enemy)
        {
            _chimere = chimere;
            _enemy = enemy;
            QueueRedraw();
        }

        public void HitFlash()
        {
            _flash = 1f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_chimere == null)
                return;

            Vector2 size = Size;
            Vector2 center = size * 0.5f;
            float pulse = 1f + Mathf.Sin(_pulse * 2.4f) * 0.025f;
            Color affinity = ChimereWorldActor.AffinityColor(_chimere.Affinity);
            Color body = _flash > 0f
                ? Colors.White
                : affinity.Darkened(_enemy ? 0.20f : 0.05f);

            float aura = _chimere.Rarity switch
            {
                ChimereRarity.Rare => 0.18f,
                ChimereRarity.Alpha => 0.30f,
                _ => 0.10f
            };

            DrawCircle(center + new Vector2(0, 14), 68f * pulse, new Color(affinity, aura));
            DrawArc(center + new Vector2(0, 14), 74f * pulse, 0, Mathf.Tau, 64, new Color(affinity, 0.55f), 2);

            switch (_chimere.SpeciesId)
            {
                case "nebuli":
                    DrawWisp(center, body, affinity);
                    break;
                case "cerf_ecorce":
                    DrawStag(center, body, affinity);
                    break;
                case "voltac":
                    DrawRaptor(center, body, affinity);
                    break;
                case "hydrune":
                    DrawSerpent(center, body, affinity);
                    break;
                case "cendrex":
                    DrawFurnace(center, body, affinity);
                    break;
                case "mycoryx":
                    DrawFungal(center, body, affinity);
                    break;
                case "prismole":
                    DrawPrism(center, body, affinity);
                    break;
                case "ferrale":
                    DrawGolem(center, body, affinity);
                    break;
                default:
                    DrawBeast(center, body, affinity);
                    break;
            }

            if (_chimere.Rarity == ChimereRarity.Alpha)
            {
                DrawArc(center, 92f, 0, Mathf.Tau, 48, Colors.White, 3);
                DrawString(
                    ThemeDB.FallbackFont,
                    center + new Vector2(-34, 96),
                    "ALPHA",
                    HorizontalAlignment.Left,
                    -1,
                    16,
                    Colors.White
                );
            }
            else if (_chimere.Rarity == ChimereRarity.Rare)
            {
                DrawArc(center, 86f, 0, Mathf.Tau, 48, affinity.Lightened(0.45f), 2);
            }

            Color scan = new Color(0, 0, 0, 0.16f);
            for (float y = 0; y < size.Y; y += 6)
                DrawLine(new Vector2(0, y), new Vector2(size.X, y), scan, 1);
        }

        private void DrawBeast(Vector2 c, Color body, Color accent)
        {
            DrawRect(new Rect2(c.X - 44, c.Y - 10, 88, 54), body);
            DrawCircle(c + new Vector2(0, -42), 28, body);
            Horn(c + new Vector2(-22, -62), -1, body);
            Horn(c + new Vector2(22, -62), 1, body);
            DrawCircle(c + new Vector2(0, 8), 11, accent.Lightened(0.4f));
        }

        private void DrawWisp(Vector2 c, Color body, Color accent)
        {
            DrawCircle(c, 46, body);
            DrawCircle(c + new Vector2(-34, -14), 18, new Color(body, 0.8f));
            DrawCircle(c + new Vector2(36, 2), 15, new Color(body, 0.7f));
            DrawCircle(c + new Vector2(4, 46), 19, new Color(body, 0.65f));
            DrawCircle(c, 13, accent.Lightened(0.5f));
        }

        private void DrawStag(Vector2 c, Color body, Color accent)
        {
            DrawRect(new Rect2(c.X - 28, c.Y - 15, 56, 80), body);
            DrawCircle(c + new Vector2(0, -42), 24, body);
            DrawLine(c + new Vector2(-18, -58), c + new Vector2(-42, -96), body, 8);
            DrawLine(c + new Vector2(18, -58), c + new Vector2(42, -96), body, 8);
            DrawLine(c + new Vector2(-34, -82), c + new Vector2(-50, -72), body, 6);
            DrawLine(c + new Vector2(34, -82), c + new Vector2(50, -72), body, 6);
            DrawCircle(c + new Vector2(0, 12), 10, accent.Lightened(0.35f));
        }

        private void DrawRaptor(Vector2 c, Color body, Color accent)
        {
            DrawPolygon(
                new[]
                {
                    c + new Vector2(-54, 28),
                    c + new Vector2(-10, -48),
                    c + new Vector2(52, -20),
                    c + new Vector2(26, 46)
                },
                new[] { body, body, body, body }
            );
            DrawLine(c + new Vector2(-10, -2), c + new Vector2(-78, 10), body, 10);
            DrawLine(c + new Vector2(20, 8), c + new Vector2(80, 28), body, 10);
            DrawCircle(c + new Vector2(8, -16), 9, accent.Lightened(0.45f));
        }

        private void DrawSerpent(Vector2 c, Color body, Color accent)
        {
            DrawArc(c + new Vector2(-4, 10), 50, -1.2f, 1.5f, 24, body, 24);
            DrawCircle(c + new Vector2(28, -42), 23, body);
            DrawCircle(c + new Vector2(-22, 34), 12, accent.Lightened(0.45f));
        }

        private void DrawFurnace(Vector2 c, Color body, Color accent)
        {
            DrawRect(new Rect2(c.X - 40, c.Y - 45, 80, 94), body);
            DrawRect(new Rect2(c.X - 58, c.Y - 18, 18, 66), body);
            DrawRect(new Rect2(c.X + 40, c.Y - 18, 18, 66), body);
            DrawCircle(c + new Vector2(0, 4), 16, accent.Lightened(0.5f));
            DrawLine(c + new Vector2(0, -46), c + new Vector2(0, -86), body, 14);
        }

        private void DrawFungal(Vector2 c, Color body, Color accent)
        {
            DrawRect(new Rect2(c.X - 20, c.Y - 12, 40, 74), body);
            DrawCircle(c + new Vector2(0, -34), 54, body);
            DrawCircle(c + new Vector2(-36, -20), 16, accent.Darkened(0.1f));
            DrawCircle(c + new Vector2(34, -12), 13, accent.Darkened(0.1f));
            DrawCircle(c + new Vector2(4, -54), 11, accent.Lightened(0.45f));
        }

        private void DrawPrism(Vector2 c, Color body, Color accent)
        {
            DrawPolygon(
                new[]
                {
                    c + new Vector2(0, -72),
                    c + new Vector2(46, -12),
                    c + new Vector2(20, 62),
                    c + new Vector2(-20, 62),
                    c + new Vector2(-46, -12)
                },
                new[] { body, body, body, body, body }
            );
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.Tau / 5f - Mathf.Pi / 2f;
                DrawLine(
                    c,
                    c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 70,
                    accent.Lightened(0.4f),
                    4
                );
            }
            DrawCircle(c, 10, Colors.White);
        }

        private void DrawGolem(Vector2 c, Color body, Color accent)
        {
            DrawRect(new Rect2(c.X - 48, c.Y - 36, 96, 88), body);
            DrawRect(new Rect2(c.X - 74, c.Y - 18, 26, 74), body);
            DrawRect(new Rect2(c.X + 48, c.Y - 18, 26, 74), body);
            DrawRect(new Rect2(c.X - 30, c.Y - 72, 60, 34), body);
            DrawCircle(c + new Vector2(0, 8), 9, accent.Lightened(0.45f));
        }

        private void Horn(Vector2 p, float dir, Color color)
        {
            DrawPolygon(
                new[]
                {
                    p,
                    p + new Vector2(16 * dir, -34),
                    p + new Vector2(6 * dir, -4)
                },
                new[] { color, color, color }
            );
        }
    }
}
