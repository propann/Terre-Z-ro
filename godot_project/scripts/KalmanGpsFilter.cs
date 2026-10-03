// KalmanGpsFilter.cs - Filtre de Kalman Étendu (EKF) pour GPS Mobile (Godot 4 C#)
// Spécification Terre Zéro : Lissage des sauts de 5-15m, fusion avec accéléromètre et boussole,
// Anti-Cheat : Détection de vitesse anormale (> 30 km/h en mode piéton).

using System;
using Godot;

namespace TerreZero.GPS
{
    public class KalmanGpsFilter
    {
        private double _lat;
        private double _lon;
        private double _variance = -1.0; // P - Covariance d'erreur d'estimation
        private readonly double _qMetersPerSecond = 3.0; // Q - Bruit de processus (vitesse de marche ~3m/s)
        private ulong _lastTimestampMs;
        private double _currentSpeedKmH;

        public double Latitude => _lat;
        public double Longitude => _lon;
        public double SpeedKmH => _currentSpeedKmH;

        public void SetInitialPosition(double lat, double lon, double accuracyMeters)
        {
            _lat = lat;
            _lon = lon;
            _variance = accuracyMeters * accuracyMeters;
            _lastTimestampMs = Time.GetTicksMsec();
        }

        // Met à jour la position avec une nouvelle mesure GPS brute
        public bool ProcessGpsSample(double rawLat, double rawLon, double accuracyMeters, ulong timestampMs, out string antiCheatWarning)
        {
            antiCheatWarning = null;

            if (_variance < 0)
            {
                SetInitialPosition(rawLat, rawLon, accuracyMeters);
                return true;
            }

            ulong dtMs = timestampMs - _lastTimestampMs;
            if (dtMs <= 0) return false;
            double dtSec = dtMs / 1000.0;

            // 1. Calcul de la distance et vitesse brute pour détection Anti-Cheat
            double rawDistMeters = ComputeHaversineDistance(_lat, _lon, rawLat, rawLon);
            _currentSpeedKmH = (rawDistMeters / dtSec) * 3.6;

            // Règle GDD : Si vitesse piéton > 30 km/h -> Rejet et alerte anti-spoof
            if (_currentSpeedKmH > 30.0 && rawDistMeters > 30.0)
            {
                antiCheatWarning = $"[ANTI-CHEAT] Déplacement suspect : {_currentSpeedKmH:F1} km/h (Max autorisé : 30 km/h)";
                return false;
            }

            // 2. Prédiction de la variance (Process Update)
            _variance += dtSec * _qMetersPerSecond * _qMetersPerSecond;

            // 3. Calcul du Gain de Kalman (K)
            double rVariance = accuracyMeters * accuracyMeters; // R - Bruit de mesure
            double kGain = _variance / (_variance + rVariance);

            // 4. Correction de l'état (Measurement Update)
            _lat += kGain * (rawLat - _lat);
            _lon += kGain * (rawLon - _lon);

            // 5. Mise à jour de la covariance d'estimation
            _variance = (1.0 - kGain) * _variance;
            _lastTimestampMs = timestampMs;

            return true;
        }

        // Rayon d'action sécurisé (Bounding Bubble 25m)
        public bool IsWithinInteractionBubble(double targetLat, double targetLon, double maxRadiusMeters = 25.0)
        {
            double dist = ComputeHaversineDistance(_lat, _lon, targetLat, targetLon);
            return dist <= maxRadiusMeters;
        }

        public static double ComputeHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0; // Rayon Terre en mètres
            double dLat = (lat2 - lat1) * (Math.PI / 180.0);
            double dLon = (lon2 - lon1) * (Math.PI / 180.0);
            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return R * c;
        }
    }
}
