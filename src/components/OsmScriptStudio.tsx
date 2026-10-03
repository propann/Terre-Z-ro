import React, { useState } from 'react';
import {
  OSM_PRESETS,
  buildOverpassQuery,
  exportToWavefrontObj
} from '../services/osmParser';
import {
  PYTHON_OSM_EXTRACTOR_SCRIPT,
  NODE_OSM_EXTRACTOR_SCRIPT
} from '../services/scriptsGenerator';
import {
  ParsedBuilding,
  LootSpot,
  Chimere
} from '../types/game';
import {
  Code2,
  Download,
  Copy,
  Check,
  Play,
  Terminal,
  FileCode,
  Box,
  Layers,
  MapPin,
  Sparkles,
  ExternalLink
} from 'lucide-react';

interface OsmScriptStudioProps {
  currentLat: number;
  currentLon: number;
  currentRadius: number;
  buildings: ParsedBuilding[];
  lootSpots: LootSpot[];
  chimeres: Chimere[];
  onLoadLocation: (lat: number, lon: number, radius: number) => void;
  isLoading: boolean;
}

export const OsmScriptStudio: React.FC<OsmScriptStudioProps> = ({
  currentLat,
  currentLon,
  currentRadius,
  buildings,
  lootSpots,
  chimeres,
  onLoadLocation,
  isLoading
}) => {
  const [activeTab, setActiveTab] = useState<'python' | 'node' | 'query' | 'export'>('python');
  const [copiedCode, setCopiedCode] = useState<boolean>(false);
  const [customLat, setCustomLat] = useState<number>(currentLat);
  const [customLon, setCustomLon] = useState<number>(currentLon);
  const [customRadius, setCustomRadius] = useState<number>(currentRadius);

  const queryQL = buildOverpassQuery(customLat, customLon, customRadius);

  const handleCopy = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedCode(true);
    setTimeout(() => setCopiedCode(false), 2000);
  };

  const handleDownloadFile = (filename: string, content: string, type: string) => {
    const blob = new Blob([content], { type });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleExportObj = () => {
    const objData = exportToWavefrontObj(buildings, currentLat, currentLon);
    handleDownloadFile('chimeres_procedural_buildings.obj', objData, 'text/plain');
  };

  const handleExportJson = () => {
    const data = {
      center: { lat: currentLat, lon: currentLon, radius: currentRadius },
      totalBuildings: buildings.length,
      totalLootSpots: lootSpots.length,
      totalChimeres: chimeres.length,
      buildings,
      lootSpots,
      chimeres
    };
    handleDownloadFile('chimeres_osm_data.json', JSON.stringify(data, null, 2), 'application/json');
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Top Banner */}
      <div className="bg-gradient-to-r from-cyan-950/40 via-slate-900 to-slate-950 border border-cyan-500/30 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-cyan-500/20 border border-cyan-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(6,182,212,0.2)]">
            💻
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Atelier Parseur OSM & Scripts (Étape 1 & 2 MVP)</h2>
              <span className="text-[10px] bg-cyan-950 text-cyan-300 border border-cyan-800 px-2 py-0.5 rounded font-mono">
                Overpass QL + Godot/Unity
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Extraction des polygones OSM réels, extrusion des hauteurs, tables de loot par tags et export 3D Wavefront .OBJ.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleExportObj}
            className="px-3 py-2 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-xs rounded-xl shadow transition-all flex items-center gap-1.5"
            title="Télécharger le maillage 3D .OBJ pour Godot 4 ou Unity"
          >
            <Box className="w-4 h-4" />
            <span>Export .OBJ 3D</span>
          </button>
          <button
            onClick={handleExportJson}
            className="px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 font-tech font-bold text-xs rounded-xl border border-slate-700 shadow transition-all flex items-center gap-1.5"
          >
            <Download className="w-4 h-4" />
            <span>Export .JSON</span>
          </button>
        </div>
      </div>

      {/* Preset Location Picker */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4 mb-4">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <MapPin className="w-4 h-4 text-cyan-400" />
            <span className="text-xs font-tech font-bold uppercase text-slate-200">
              Zones de Test Prédéfinies & Coordonnées GPS
            </span>
          </div>
          <span className="text-xs font-mono text-cyan-400">
            Rayon actif : {currentRadius}m ({buildings.length} bâtiments chargés)
          </span>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mb-3">
          {OSM_PRESETS.map((preset, idx) => (
            <button
              key={idx}
              disabled={isLoading}
              onClick={() => {
                setCustomLat(preset.lat);
                setCustomLon(preset.lon);
                setCustomRadius(preset.radiusMeters);
                onLoadLocation(preset.lat, preset.lon, preset.radiusMeters);
              }}
              className={`p-2.5 rounded-xl border text-left transition-all ${
                Math.abs(currentLat - preset.lat) < 0.001 && Math.abs(currentLon - preset.lon) < 0.001
                  ? 'bg-cyan-950/80 border-cyan-500 text-cyan-300 shadow-md'
                  : 'bg-slate-950/60 border-slate-800 text-slate-300 hover:border-slate-600'
              }`}
            >
              <div className="text-xs font-bold font-tech truncate">{preset.name}</div>
              <div className="text-[10px] text-slate-400 truncate">{preset.city}</div>
            </button>
          ))}
        </div>

        {/* Custom Lat / Lon Inputs */}
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-2 items-center bg-slate-950 p-2.5 rounded-xl border border-slate-800 text-xs font-mono">
          <div>
            <label className="text-[10px] text-slate-400 block mb-0.5">Latitude :</label>
            <input
              type="number"
              step="0.0001"
              value={customLat}
              onChange={(e) => setCustomLat(parseFloat(e.target.value) || 0)}
              className="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200"
            />
          </div>
          <div>
            <label className="text-[10px] text-slate-400 block mb-0.5">Longitude :</label>
            <input
              type="number"
              step="0.0001"
              value={customLon}
              onChange={(e) => setCustomLon(parseFloat(e.target.value) || 0)}
              className="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200"
            />
          </div>
          <div>
            <label className="text-[10px] text-slate-400 block mb-0.5">Rayon ({customRadius}m) :</label>
            <input
              type="range"
              min="200"
              max="800"
              step="50"
              value={customRadius}
              onChange={(e) => setCustomRadius(parseInt(e.target.value, 10))}
              className="w-full"
            />
          </div>
          <div className="flex items-end">
            <button
              disabled={isLoading}
              onClick={() => onLoadLocation(customLat, customLon, customRadius)}
              className="w-full py-2 bg-cyan-600 hover:bg-cyan-500 disabled:opacity-50 text-white font-tech font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 shadow"
            >
              <Play className="w-3.5 h-3.5 fill-current" />
              <span>{isLoading ? 'Chargement...' : 'Exécuter Requête'}</span>
            </button>
          </div>
        </div>
      </div>

      {/* Code Viewer Tabs */}
      <div className="flex-1 bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-3">
          <div className="flex items-center gap-2">
            <button
              onClick={() => setActiveTab('python')}
              className={`px-3 py-1.5 rounded-lg text-xs font-mono font-bold flex items-center gap-1.5 transition-all ${
                activeTab === 'python' ? 'bg-cyan-500 text-slate-950' : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              <FileCode className="w-3.5 h-3.5" />
              <span>Script Python (osm_extractor.py)</span>
            </button>

            <button
              onClick={() => setActiveTab('node')}
              className={`px-3 py-1.5 rounded-lg text-xs font-mono font-bold flex items-center gap-1.5 transition-all ${
                activeTab === 'node' ? 'bg-cyan-500 text-slate-950' : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              <Terminal className="w-3.5 h-3.5" />
              <span>Script Node.js (osm_extractor.js)</span>
            </button>

            <button
              onClick={() => setActiveTab('query')}
              className={`px-3 py-1.5 rounded-lg text-xs font-mono font-bold flex items-center gap-1.5 transition-all ${
                activeTab === 'query' ? 'bg-cyan-500 text-slate-950' : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              <Code2 className="w-3.5 h-3.5" />
              <span>Requête Overpass QL</span>
            </button>
          </div>

          <button
            onClick={() => handleCopy(
              activeTab === 'python'
                ? PYTHON_OSM_EXTRACTOR_SCRIPT
                : activeTab === 'node'
                ? NODE_OSM_EXTRACTOR_SCRIPT
                : queryQL
            )}
            className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-mono rounded-lg border border-slate-700 transition-all flex items-center gap-1.5"
          >
            {copiedCode ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            <span>{copiedCode ? 'Copié !' : 'Copier le script'}</span>
          </button>
        </div>

        {/* Code View Area */}
        <div className="flex-1 bg-slate-950 border border-slate-800/80 rounded-xl p-3 overflow-y-auto font-mono text-xs text-slate-300 leading-relaxed">
          <pre className="whitespace-pre-wrap select-text">
            {activeTab === 'python' && PYTHON_OSM_EXTRACTOR_SCRIPT}
            {activeTab === 'node' && NODE_OSM_EXTRACTOR_SCRIPT}
            {activeTab === 'query' && queryQL}
          </pre>
        </div>
      </div>
    </div>
  );
};
