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
            Color body = _flash > 0f ? Colors.White : affinity.Darkened(_enemy ? 0.20f : 0.05f);

            DrawCircle(center + new Vector2(0, 14), 62f * pulse, new Color(affinity, 0.10f));
            DrawArc(center + new Vector2(0, 14), 68f * pulse, 0, Mathf.Tau, 64, new Color(affinity, 0.45f), 2);

            // Silhouette 2D rétro volontairement simple, lisible et commune aux espèces.
            Rect2 torso = new Rect2(center.X - 34, center.Y - 22, 68, 72);
            DrawRect(torso, body);
            DrawCircle(center + new Vector2(0, -48), 29, body);

            Vector2 leftHorn = center + new Vector2(-24, -69);
            Vector2 rightHorn = center + new Vector2(24, -69);
            DrawPolygon(
                new[] { leftHorn, leftHorn + new Vector2(-18, -28), leftHorn + new Vector2(8, -8) },
                new[] { body, body, body }
            );
            DrawPolygon(
                new[] { rightHorn, rightHorn + new Vector2(18, -28), rightHorn + new Vector2(-8, -8) },
                new[] { body, body, body }
            );

            Color core = affinity.Lightened(0.45f);
            DrawCircle(center + new Vector2(0, 8), 12, core);
            DrawCircle(center + new Vector2(-10, -50), 4, Colors.Black);
            DrawCircle(center + new Vector2(10, -50), 4, Colors.Black);

            // Petite trame façon sprite/pixel-art sans perdre la propreté 3D/HD.
            Color scan = new Color(0, 0, 0, 0.16f);
            for (float y = 0; y < size.Y; y += 6)
                DrawLine(new Vector2(0, y), new Vector2(size.X, y), scan, 1);
        }
    }
}
