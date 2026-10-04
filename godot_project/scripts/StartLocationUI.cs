using System;
using Godot;
using TerreZero.World.Geo;

namespace TerreZero.UI
{
    public partial class StartLocationUI : Control
    {
        public event Action<double, double, int> StartConfirmed;
        public event Action<bool> PermissionChanged;

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

        public void ShowMachineLocationResult(
            bool resolved,
            double latitude,
            double longitude,
            double accuracyMeters,
            string source)
        {
            ConfigureMachinePoint(latitude, longitude);

            _status.Text = resolved
                ? $"POINT MACHINE // {source} • précision ~{accuracyMeters:F0} m — choisissez votre zone."
                : "GÉOLOCALISATION SYSTÈME INDISPONIBLE — coordonnées configurées utilisées.";

            _map.MouseFilter = PermissionGranted
                ? MouseFilterEnum.Stop
                : MouseFilterEnum.Ignore;
        }

        public override void _Draw()
        {
            if (_map == null)
                return;
        }

        private void OnPermissionToggled(bool enabled)
        {
            PermissionChanged?.Invoke(enabled);

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

}
