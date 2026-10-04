package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"os"
	"strconv"
	"sync"
	"time"
)

type WeatherCurrent struct {
	Time                string  `json:"time"`
	TemperatureC        float64 `json:"temperature_c"`
	ApparentTemperature float64 `json:"apparent_temperature_c"`
	RelativeHumidity    float64 `json:"relative_humidity_percent"`
	PrecipitationMM     float64 `json:"precipitation_mm"`
	RainMM              float64 `json:"rain_mm"`
	ShowersMM           float64 `json:"showers_mm"`
	SnowfallCM          float64 `json:"snowfall_cm"`
	WeatherCode         int     `json:"weather_code"`
	CloudCoverPercent   float64 `json:"cloud_cover_percent"`
	WindSpeedKmh        float64 `json:"wind_speed_kmh"`
	WindDirectionDeg    float64 `json:"wind_direction_deg"`
	WindGustsKmh        float64 `json:"wind_gusts_kmh"`
	IsDay               int     `json:"is_day"`
}

type WeatherResponse struct {
	Latitude  float64        `json:"latitude"`
	Longitude float64        `json:"longitude"`
	Timezone  string         `json:"timezone"`
	Source    string         `json:"source"`
	FetchedAt time.Time      `json:"fetched_at"`
	Current   WeatherCurrent `json:"current"`
}

type openMeteoResponse struct {
	Latitude  float64 `json:"latitude"`
	Longitude float64 `json:"longitude"`
	Timezone  string  `json:"timezone"`
	Current   struct {
		Time                string  `json:"time"`
		Temperature2M       float64 `json:"temperature_2m"`
		ApparentTemperature float64 `json:"apparent_temperature"`
		RelativeHumidity2M  float64 `json:"relative_humidity_2m"`
		Precipitation       float64 `json:"precipitation"`
		Rain                float64 `json:"rain"`
		Showers             float64 `json:"showers"`
		Snowfall            float64 `json:"snowfall"`
		WeatherCode         int     `json:"weather_code"`
		CloudCover          float64 `json:"cloud_cover"`
		WindSpeed10M        float64 `json:"wind_speed_10m"`
		WindDirection10M    float64 `json:"wind_direction_10m"`
		WindGusts10M        float64 `json:"wind_gusts_10m"`
		IsDay               int     `json:"is_day"`
	} `json:"current"`
}

type weatherCacheEntry struct {
	response WeatherResponse
	expires  time.Time
}

var (
	weatherCacheMu sync.RWMutex
	weatherCache   = map[string]weatherCacheEntry{}
	weatherClient  = &http.Client{Timeout: 5 * time.Second}
)

func handleWeather(w http.ResponseWriter, r *http.Request) {
	lat, err := strconv.ParseFloat(r.URL.Query().Get("lat"), 64)
	if err != nil || lat < -90 || lat > 90 {
		http.Error(w, "latitude invalide", http.StatusBadRequest)
		return
	}

	lon, err := strconv.ParseFloat(r.URL.Query().Get("lon"), 64)
	if err != nil || lon < -180 || lon > 180 {
		http.Error(w, "longitude invalide", http.StatusBadRequest)
		return
	}

	response, err := getWeather(r.Context(), lat, lon)
	if err != nil {
		http.Error(w, "météo indisponible", http.StatusBadGateway)
		return
	}

	writeJSON(w, http.StatusOK, response)
}

func getWeather(ctx context.Context, lat, lon float64) (WeatherResponse, error) {
	key := fmt.Sprintf("%.3f,%.3f", lat, lon)

	weatherCacheMu.RLock()
	entry, ok := weatherCache[key]
	weatherCacheMu.RUnlock()
	if ok && time.Now().Before(entry.expires) {
		return entry.response, nil
	}

	baseURL := os.Getenv("WEATHER_API_BASE_URL")
	if baseURL == "" {
		baseURL = "https://api.open-meteo.com/v1/forecast"
	}

	u, err := url.Parse(baseURL)
	if err != nil {
		return WeatherResponse{}, err
	}

	query := u.Query()
	query.Set("latitude", strconv.FormatFloat(lat, 'f', 6, 64))
	query.Set("longitude", strconv.FormatFloat(lon, 'f', 6, 64))
	query.Set("timezone", "auto")
	query.Set(
		"current",
		"temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,rain,showers,snowfall,weather_code,cloud_cover,wind_speed_10m,wind_direction_10m,wind_gusts_10m",
	)
	u.RawQuery = query.Encode()

	request, err := http.NewRequestWithContext(ctx, http.MethodGet, u.String(), nil)
	if err != nil {
		return WeatherResponse{}, err
	}

	resp, err := weatherClient.Do(request)
	if err != nil {
		return WeatherResponse{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return WeatherResponse{}, fmt.Errorf("weather provider status %d", resp.StatusCode)
	}

	var provider openMeteoResponse
	if err := json.NewDecoder(resp.Body).Decode(&provider); err != nil {
		return WeatherResponse{}, err
	}

	result := WeatherResponse{
		Latitude:  provider.Latitude,
		Longitude: provider.Longitude,
		Timezone:  provider.Timezone,
		Source:    "Open-Meteo",
		FetchedAt: time.Now().UTC(),
		Current: WeatherCurrent{
			Time:                provider.Current.Time,
			TemperatureC:        provider.Current.Temperature2M,
			ApparentTemperature: provider.Current.ApparentTemperature,
			RelativeHumidity:    provider.Current.RelativeHumidity2M,
			PrecipitationMM:     provider.Current.Precipitation,
			RainMM:              provider.Current.Rain,
			ShowersMM:           provider.Current.Showers,
			SnowfallCM:          provider.Current.Snowfall,
			WeatherCode:         provider.Current.WeatherCode,
			CloudCoverPercent:   provider.Current.CloudCover,
			WindSpeedKmh:        provider.Current.WindSpeed10M,
			WindDirectionDeg:    provider.Current.WindDirection10M,
			WindGustsKmh:        provider.Current.WindGusts10M,
			IsDay:               provider.Current.IsDay,
		},
	}

	weatherCacheMu.Lock()
	weatherCache[key] = weatherCacheEntry{
		response: result,
		expires:  time.Now().Add(10 * time.Minute),
	}
	weatherCacheMu.Unlock()

	return result, nil
}
