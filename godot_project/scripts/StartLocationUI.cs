using System;
using Godot;
using TerreZero.World.Geo;

namespace TerreZero.UI
{
    public partial class StartLocationUI : Control
    {
        public event Action<double, double, int> StartConfirmed;

        [Export] public Color AccentColor { get; set; } = new Color("e8a23a");
        [Export] public Color DangerColor { get; set; } = new Color("e35d4f");
        [Export] public Color MapBackground { get; set; } = new Color("111820");

        private Control _map;
        private Label _status;
        private Label _coordinates;
        private Button _radius5;
        private Button _radius10;
        private Button _confirm;
        private CheckButton _permission;
        private double _machineLat;
        private double _machineLon;
        private double _selectedLat;
        private double _selectedLon;
        private int _radiusKm = 10;
        private Vector2 _selectedNormalized = new(0.5f, 0.5f);
        private bool _hasSelection;

        public override void _Ready()
        {
            _map = GetNode<Control>("%MapSurface");
            _status = GetNode<Label>("%Status");
            _coordinates = GetNode<Label>("%Coordinates");
            _radius5 = GetNode<Button>("%Radius5");
            _radius10 = GetNode<Button>("%Radius10");
            _confirm = GetNode<Button>("%Confirm");
            _permission = GetNode<CheckButton>("%Permission");

            _map.GuiInput += OnMapInput;
            _radius5.Pressed += () => SetRadius(5);
            _radius10.Pressed += () => SetRadius(10);
            _confirm.Pressed += ConfirmSelection;
            _permission.Toggled += OnPermissionToggled;

            SetRadius(10);
            UpdateState();
            QueueRedraw();
        }

        public void ConfigureMachinePoint(double latitude, double longitude)
        {
            _machineLat = latitude;
            _machineLon = longitude;
            _selectedLat = latitude;
            _selectedLon = longitude;
            _selectedNormalized = new Vector2(0.5f, 0.5f);
            _hasSelection = false;
            UpdateState();
            QueueRedraw();
        }

        public bool PermissionGranted => _permission?.ButtonPressed ?? false;

        public override void _Draw()
        {
            if (_map == null)
                return;
        }

        private void OnPermissionToggled(bool enabled)
        {
            _status.Text = enabled
                ? "POINT MACHINE AUTORISÉ — choisissez votre zone d'émergence."
                : "LOCALISATION NON AUTORISÉE — le mode monde réel reste verrouillé.";

            _map.MouseFilter = enabled
                ? MouseFilterEnum.Stop
                : MouseFilterEnum.Ignore;

            _confirm.Disabled = !enabled || !_hasSelection;
        }

        private void SetRadius(int radius)
        {
            _radiusKm = radius;
            _radius5.ButtonPressed = radius == 5;
            _radius10.ButtonPressed = radius == 10;

            if (_hasSelection)
                ApplySelection(_selectedNormalized);

            QueueMapRedraw();
        }

        private void OnMapInput(InputEvent input)
        {
            if (!_permission.ButtonPressed)
                return;

            if (input is InputEventMouseButton mouse &&
                mouse.ButtonIndex == MouseButton.Left &&
                mouse.Pressed)
            {
                Vector2 size = _map.Size;
                if (size.X <= 0 || size.Y <= 0)
                    return;

                Vector2 normalized = new Vector2(
                    Mathf.Clamp(mouse.Position.X / size.X, 0f, 1f),
                    Mathf.Clamp(mouse.Position.Y / size.Y, 0f, 1f)
                );

                // Le disque central représente exactement le rayon autorisé.
                Vector2 center = new(0.5f, 0.5f);
                Vector2 delta = normalized - center;
                float maxRadius = 0.42f;

                if (delta.Length() > maxRadius)
                    normalized = center + delta.Normalized() * maxRadius;

                ApplySelection(normalized);
                QueueMapRedraw();
            }
        }

        private void ApplySelection(Vector2 normalized)
        {
            _selectedNormalized = normalized;
            _hasSelection = true;

            Vector2 center = new(0.5f, 0.5f);
            Vector2 delta = normalized - center;
            float normalizedRadius = delta.Length() / 0.42f;

            float eastKm = delta.X / 0.42f * _radiusKm;
            float northKm = -delta.Y / 0.42f * _radiusKm;

            // Projection locale suffisante pour un rayon de 10 km.
            var anchor = new GeoAnchor(_machineLat, _machineLon);
            GeoCoordinate geo = anchor.ToGeoCoordinate(
                new Vector2(eastKm * 1000f, northKm * 1000f)
            );

            _selectedLat = geo.Latitude;
            _selectedLon = geo.Longitude;

            _coordinates.Text =
                $"{_selectedLat:F6}, {_selectedLon:F6}  •  " +
                $"{normalizedRadius * _radiusKm:F1} km du point machine";

            _status.Text = "POINT DE DÉPART PRÊT — validation serveur requise.";
            _confirm.Disabled = false;
        }

        private void ConfirmSelection()
        {
            if (!_permission.ButtonPressed || !_hasSelection)
                return;

            _confirm.Disabled = true;
            _status.Text = "VALIDATION DU SECTEUR…";
            StartConfirmed?.Invoke(_selectedLat, _selectedLon, _radiusKm);
        }

        public void ShowValidationResult(bool allowed, string message)
        {
            _status.Text = message;
            _confirm.Disabled = !allowed && !_hasSelection;

            if (!allowed)
                FlashError();
        }

        public void EnterGame()
        {
            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.35f);
            tween.TweenCallback(Callable.From(() =>
            {
                Visible = false;
                MouseFilter = MouseFilterEnum.Ignore;
            }));
        }

        private void FlashError()
        {
            Color original = _status.Modulate;
            _status.Modulate = DangerColor;
            var tween = CreateTween();
            tween.TweenProperty(_status, "modulate", original, 0.5f);
        }

        private void UpdateState()
        {
            if (_status == null)
                return;

            _status.Text = "AUTORISEZ LE POINT MACHINE POUR DÉVERROUILLER LA CARTE.";
            _coordinates.Text = "Aucun point de départ sélectionné";
            _confirm.Disabled = true;
        }

        private void QueueMapRedraw()
        {
            if (_map is TacticalMapSurface tactical)
            {
                tactical.Configure(
                    _radiusKm,
                    _selectedNormalized,
                    _hasSelection,
                    AccentColor,
                    MapBackground
                );
            }
        }
    }

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
