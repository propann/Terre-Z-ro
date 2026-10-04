using Godot;
using TerreZero.Chimeres;

namespace TerreZero.UI
{
    public partial class ChimereBattleFx : Control
    {
        private ChimereAffinity _affinity;
        private float _life;
        private bool _enemyTarget;
        private ChimereMoveKind _kind;

        public override void _Process(double delta)
        {
            if (_life <= 0f)
                return;

            _life = Mathf.Max(0f, _life - (float)delta * 2.8f);
            QueueRedraw();
        }

        public void Play(
            ChimereAffinity affinity,
            ChimereMoveKind kind,
            bool enemyTarget)
        {
            _affinity = affinity;
            _kind = kind;
            _enemyTarget = enemyTarget;
            _life = 1f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_life <= 0f)
                return;

            Vector2 center = _enemyTarget
                ? new Vector2(Size.X * 0.74f, Size.Y * 0.34f)
                : new Vector2(Size.X * 0.27f, Size.Y * 0.33f);

            Color color = ChimereWorldActor.AffinityColor(_affinity);
            float alpha = Mathf.Clamp(_life, 0f, 1f);
            color.A *= alpha;

            switch (_affinity)
            {
                case ChimereAffinity.Electric:
                    DrawElectric(center, color);
                    break;
                case ChimereAffinity.Thermal:
                    DrawThermal(center, color);
                    break;
                case ChimereAffinity.Hydro:
                    DrawHydro(center, color);
                    break;
                case ChimereAffinity.Spectral:
                    DrawSpectral(center, color);
                    break;
                case ChimereAffinity.Toxic:
                    DrawToxic(center, color);
                    break;
                case ChimereAffinity.Radiant:
                    DrawRadiant(center, color);
                    break;
                case ChimereAffinity.Mineral:
                    DrawMineral(center, color);
                    break;
                case ChimereAffinity.Organic:
                    DrawOrganic(center, color);
                    break;
                default:
                    DrawImpact(center, color);
                    break;
            }
        }

        private void DrawElectric(Vector2 c, Color color)
        {
            float spread = 62f * _life;
            for (int i = 0; i < 5; i++)
            {
                float x = -spread + i * spread * 0.5f;
                DrawPolyline(
                    new[]
                    {
                        c + new Vector2(x, -58),
                        c + new Vector2(x + 18, -22),
                        c + new Vector2(x - 2, 12),
                        c + new Vector2(x + 24, 52)
                    },
                    color,
                    5
                );
            }
        }

        private void DrawThermal(Vector2 c, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                float radius = 18f + i * 12f * _life;
                DrawCircle(c + new Vector2((i - 2) * 14f, 6f), radius, new Color(color, 0.13f));
                DrawArc(c, radius + 18f, 0, Mathf.Tau, 28, color, 3);
            }
        }

        private void DrawHydro(Vector2 c, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                float radius = 24f + i * 16f;
                DrawArc(c, radius, -2.6f, 0.6f, 30, color, 4);
            }
        }

        private void DrawSpectral(Vector2 c, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = new Vector2(Mathf.Sin(i * 1.7f) * 30f, -i * 18f);
                DrawCircle(c + offset, 34f - i * 5f, new Color(color, 0.14f));
            }
        }

        private void DrawToxic(Vector2 c, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.Tau / 8f;
                Vector2 p = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (26f + 28f * _life);
                DrawCircle(p, 8f + (i % 3) * 3f, new Color(color, 0.55f));
            }
        }

        private void DrawRadiant(Vector2 c, Color color)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.Tau / 12f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(c + dir * 18f, c + dir * (76f * _life + 22f), color, 4);
            }
            DrawCircle(c, 18f, new Color(color, 0.45f));
        }

        private void DrawMineral(Vector2 c, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 22f;
                DrawPolygon(
                    new[]
                    {
                        c + new Vector2(x - 10, 42),
                        c + new Vector2(x, -42f * _life),
                        c + new Vector2(x + 10, 42)
                    },
                    new[] { color, color, color }
                );
            }
        }

        private void DrawOrganic(Vector2 c, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.Tau / 6f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(c, c + dir * (62f * _life), color, 5);
                DrawCircle(c + dir * 48f, 10f, new Color(color, 0.6f));
            }
        }

        private void DrawImpact(Vector2 c, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.Tau / 8f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(c + dir * 12f, c + dir * (70f * _life), color, 4);
            }
        }
    }
}
