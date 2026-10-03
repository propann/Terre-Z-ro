import React, { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';
import JSZip from 'jszip';
import {
  VoxelChunk,
  VoxelMaterial,
  VOXEL_PALETTE,
  CHUNK_SIZE,
  VOXEL_SIZE_METERS,
  voxelizeOsmBuildingBlock,
  computeGreedyMesh,
  MeshingResult,
  serializeVoxelDeltaRLE
} from '../services/voxelEngine';
import {
  GODOT4_VOXEL_CHUNK_CS,
  GODOT4_GREEDY_MESHER_CS,
  GODOT4_OSM_VOXELIZER_CS
} from '../services/godotVoxelScripts';
import { soundFx } from '../services/soundFx';
import {
  Box,
  Flame,
  Radio,
  Hammer,
  RotateCcw,
  Sparkles,
  Zap,
  Check,
  Copy,
  Download,
  Terminal,
  Activity,
  Maximize2,
  FileCode,
  Crosshair,
  Shield,
  Eye,
  Layers,
  Gamepad2,
  Package,
  FolderArchive,
  ArrowUp,
  ArrowDown,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';
import confetti from 'canvas-confetti';

interface MicroVoxelStudioProps {
  onCollectLoot: (matName: string, count: number) => void;
}

export const MicroVoxelStudio: React.FC<MicroVoxelStudioProps> = ({ onCollectLoot }) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<THREE.Scene | null>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);
  const voxelMeshGroupRef = useRef<THREE.Group | null>(null);
  const blockHighlightRef = useRef<THREE.LineSegments | null>(null);
  const particleGroupRef = useRef<THREE.Points | null>(null);
  const animationFrameRef = useRef<number | null>(null);

  // Voxel Chunk State
  const [activeBuildingType, setActiveBuildingType] = useState<'pharmacy' | 'bank' | 'residential' | 'industrial'>('pharmacy');
  const chunkRef = useRef<VoxelChunk>(voxelizeOsmBuildingBlock('pharmacy'));
  const [meshingStats, setMeshingStats] = useState<MeshingResult | null>(null);
  const [editsHistory, setEditsHistory] = useState<{ x: number; y: number; z: number; mat: VoxelMaterial }[]>([]);

  // Camera & Play Mode
  const [viewMode, setViewMode] = useState<'orbit' | 'firstPerson'>('firstPerson');
  const [currentTool, setCurrentTool] = useState<'drill' | 'xray' | 'build'>('drill');
  const [brushSize, setBrushSize] = useState<number>(1.5);
  const [selectedBuildMat, setSelectedBuildMat] = useState<VoxelMaterial>(VoxelMaterial.STEEL_BARRICADE);
  const [useGreedyMeshing, setUseGreedyMeshing] = useState<boolean>(true);
  const [showWireframe, setShowWireframe] = useState<boolean>(false);
  const [copiedScript, setCopiedScript] = useState<string | null>(null);
  const [activeScriptTab, setActiveScriptTab] = useState<'chunk' | 'mesher' | 'osm' | 'delta'>('chunk');
  const [isExportingZip, setIsExportingZip] = useState<boolean>(false);
  const [isDrilling, setIsDrilling] = useState<boolean>(false);

  // First-Person Player Physics
  const playerPos = useRef(new THREE.Vector3(0, 2.0, 5.5));
  const keyState = useRef<{ [key: string]: boolean }>({});
  const isDraggingRef = useRef(false);
  const prevMousePos = useRef({ x: 0, y: 0 });
  const cameraAngle = useRef({ theta: Math.PI / 4, phi: Math.PI / 3.8, radius: 14 });
  const fpsLook = useRef({ pitch: -0.1, yaw: 0 });

  // Initialize Three.js Viewport
  useEffect(() => {
    if (!containerRef.current) return;
    const width = containerRef.current.clientWidth;
    const height = containerRef.current.clientHeight;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x060913);
    scene.fog = new THREE.FogExp2(0x060913, 0.02);
    sceneRef.current = scene;

    const camera = new THREE.PerspectiveCamera(60, width / height, 0.05, 120);
    cameraRef.current = camera;

    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    rendererRef.current = renderer;

    containerRef.current.replaceChildren(renderer.domElement);

    // Studio Lighting
    const ambientLight = new THREE.AmbientLight(0x64748b, 1.4);
    scene.add(ambientLight);

    const sunLight = new THREE.DirectionalLight(0xffedd5, 1.8);
    sunLight.position.set(12, 25, 18);
    sunLight.castShadow = true;
    scene.add(sunLight);

    const blueFill = new THREE.DirectionalLight(0x0284c7, 0.8);
    blueFill.position.set(-15, 10, -10);
    scene.add(blueFill);

    // Ground Plane with Grid
    const groundGeo = new THREE.PlaneGeometry(32, 32);
    const groundMat = new THREE.MeshStandardMaterial({ color: 0x090d16, roughness: 0.9 });
    const ground = new THREE.Mesh(groundGeo, groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = -0.01;
    scene.add(ground);

    const grid = new THREE.GridHelper(32, 64, 0x0284c7, 0x1e293b);
    grid.position.y = 0;
    scene.add(grid);

    // Voxel Mesh Group
    const meshGroup = new THREE.Group();
    meshGroup.position.set(-CHUNK_SIZE * VOXEL_SIZE_METERS / 2, 0, -CHUNK_SIZE * VOXEL_SIZE_METERS / 2);
    scene.add(meshGroup);
    voxelMeshGroupRef.current = meshGroup;

    // Block Targeting Wireframe Box
    const highlightGeo = new THREE.EdgesGeometry(new THREE.BoxGeometry(VOXEL_SIZE_METERS * 1.05, VOXEL_SIZE_METERS * 1.05, VOXEL_SIZE_METERS * 1.05));
    const highlightMat = new THREE.LineBasicMaterial({ color: 0x38bdf8, linewidth: 2 });
    const highlight = new THREE.LineSegments(highlightGeo, highlightMat);
    highlight.visible = false;
    scene.add(highlight);
    blockHighlightRef.current = highlight;

    // Spark Particles for Mining
    const pCount = 80;
    const pGeo = new THREE.BufferGeometry();
    const pPos = new Float32Array(pCount * 3);
    for (let i = 0; i < pCount * 3; i++) pPos[i] = 0;
    pGeo.setAttribute('position', new THREE.BufferAttribute(pPos, 3));
    const pMat = new THREE.PointsMaterial({ color: 0xf97316, size: 0.08, transparent: true, opacity: 0 });
    const particles = new THREE.Points(pGeo, pMat);
    scene.add(particles);
    particleGroupRef.current = particles;

    // Keyboard Listeners for First-Person
    const handleKeyDown = (e: KeyboardEvent) => {
      keyState.current[e.code] = true;
      if (e.code === 'KeyF') {
        setCurrentTool(prev => (prev === 'xray' ? 'drill' : 'xray'));
        soundFx.playRadarPing(900);
      }
      if (e.code >= 'Digit1' && e.code <= '9') {
        const matIdx = parseInt(e.code.replace('Digit', ''), 10);
        const mats = [
          VoxelMaterial.CONCRETE,
          VoxelMaterial.BRICK,
          VoxelMaterial.ASPHALT,
          VoxelMaterial.SIDEWALK,
          VoxelMaterial.REINFORCED_GLASS,
          VoxelMaterial.COPPER_WIRING,
          VoxelMaterial.MED_CACHE,
          VoxelMaterial.STEEL_BARRICADE,
          VoxelMaterial.TURRET_BASE
        ];
        if (mats[matIdx - 1]) {
          setSelectedBuildMat(mats[matIdx - 1]);
          setCurrentTool('build');
          soundFx.playRadarPing(800);
        }
      }
    };

    const handleKeyUp = (e: KeyboardEvent) => {
      keyState.current[e.code] = false;
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);

    // Resize
    const handleResize = () => {
      if (!containerRef.current || !rendererRef.current || !cameraRef.current) return;
      const w = containerRef.current.clientWidth;
      const h = containerRef.current.clientHeight;
      cameraRef.current.aspect = w / h;
      cameraRef.current.updateProjectionMatrix();
      rendererRef.current.setSize(w, h);
    };
    window.addEventListener('resize', handleResize);

    const clock = new THREE.Clock();

    // Render loop
    const animate = () => {
      animationFrameRef.current = requestAnimationFrame(animate);
      const delta = Math.min(0.1, clock.getDelta());

      if (viewMode === 'firstPerson' && cameraRef.current) {
        // WASD & Arrow Movement
        const speed = 4.5;
        const forward = new THREE.Vector3(-Math.sin(fpsLook.current.yaw), 0, -Math.cos(fpsLook.current.yaw));
        const right = new THREE.Vector3(Math.cos(fpsLook.current.yaw), 0, -Math.sin(fpsLook.current.yaw));

        const moveDir = new THREE.Vector3(0, 0, 0);
        if (keyState.current['KeyW'] || keyState.current['KeyZ'] || keyState.current['ArrowUp']) moveDir.add(forward);
        if (keyState.current['KeyS'] || keyState.current['ArrowDown']) moveDir.sub(forward);
        if (keyState.current['KeyD'] || keyState.current['ArrowRight']) moveDir.add(right);
        if (keyState.current['KeyA'] || keyState.current['KeyQ'] || keyState.current['ArrowLeft']) moveDir.sub(right);

        if (moveDir.lengthSq() > 0) {
          moveDir.normalize();
          playerPos.current.addScaledVector(moveDir, speed * delta);
        }

        // Jump (Space) / Crouch (Shift)
        if (keyState.current['Space']) {
          playerPos.current.y = Math.min(6.5, playerPos.current.y + 4 * delta);
        } else if (keyState.current['ShiftLeft']) {
          playerPos.current.y = Math.max(1.0, playerPos.current.y - 4 * delta);
        }

        // Keep inside bounds
        playerPos.current.x = Math.max(-10, Math.min(10, playerPos.current.x));
        playerPos.current.z = Math.max(-10, Math.min(10, playerPos.current.z));

        cameraRef.current.position.copy(playerPos.current);
        const lookTarget = playerPos.current.clone().add(new THREE.Vector3(
          -Math.sin(fpsLook.current.yaw) * Math.cos(fpsLook.current.pitch),
          Math.sin(fpsLook.current.pitch),
          -Math.cos(fpsLook.current.yaw) * Math.cos(fpsLook.current.pitch)
        ));
        cameraRef.current.lookAt(lookTarget);

        // Center Raycast for block targeting
        const raycaster = new THREE.Raycaster();
        raycaster.setFromCamera(new THREE.Vector2(0, 0), cameraRef.current);
        if (voxelMeshGroupRef.current && blockHighlightRef.current) {
          const intersects = raycaster.intersectObjects(voxelMeshGroupRef.current.children, true);
          if (intersects.length > 0 && intersects[0].distance < 7.0) {
            const hit = intersects[0];
            const localPoint = voxelMeshGroupRef.current.worldToLocal(hit.point.clone());
            const vx = Math.round(localPoint.x / VOXEL_SIZE_METERS);
            const vy = Math.round(localPoint.y / VOXEL_SIZE_METERS);
            const vz = Math.round(localPoint.z / VOXEL_SIZE_METERS);

            const worldBoxPos = voxelMeshGroupRef.current.localToWorld(
              new THREE.Vector3(vx * VOXEL_SIZE_METERS, vy * VOXEL_SIZE_METERS, vz * VOXEL_SIZE_METERS)
            );
            blockHighlightRef.current.position.copy(worldBoxPos);
            blockHighlightRef.current.visible = true;
          } else {
            blockHighlightRef.current.visible = false;
          }
        }
      } else if (cameraRef.current) {
        // Orbit Camera Mode
        const { theta, phi, radius } = cameraAngle.current;
        const cx = radius * Math.sin(phi) * Math.sin(theta);
        const cy = radius * Math.cos(phi);
        const cz = radius * Math.sin(phi) * Math.cos(theta);

        cameraRef.current.position.set(cx, cy + 2.5, cz);
        cameraRef.current.lookAt(0, 2.5, 0);
        if (blockHighlightRef.current) blockHighlightRef.current.visible = false;
      }

      renderer.render(scene, camera);
    };

    animate();

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
      window.removeEventListener('resize', handleResize);
      if (animationFrameRef.current) cancelAnimationFrame(animationFrameRef.current);
      renderer.dispose();
    };
  }, [viewMode]);

  // Rebuild 3D Mesh when chunk or meshing mode changes
  const rebuild3DMesh = () => {
    const group = voxelMeshGroupRef.current;
    if (!group) return;
    group.clear();

    const chunk = chunkRef.current;
    const meshResult = computeGreedyMesh(chunk);
    setMeshingStats(meshResult);

    if (useGreedyMeshing) {
      // SPRINT 2.1 : GREEDY MESHED QUADS
      const positionsByMat = new Map<VoxelMaterial, number[]>();

      meshResult.quads.forEach((q) => {
        // X-Ray Mode: culled exterior non-loot walls
        if (currentTool === 'xray') {
          const isLoot = q.material === VoxelMaterial.COPPER_WIRING || q.material === VoxelMaterial.MED_CACHE;
          if (!isLoot && Math.random() < 0.85) return;
        }

        const scale = VOXEL_SIZE_METERS;

        const p0 = [q.x, q.y, q.z];
        const du = [0, 0, 0]; du[(q.axis + 1) % 3] = q.w;
        const dv = [0, 0, 0]; dv[(q.axis + 2) % 3] = q.h;

        const v0 = new THREE.Vector3(p0[0] * scale, p0[1] * scale, p0[2] * scale);
        const v1 = new THREE.Vector3((p0[0] + du[0]) * scale, (p0[1] + du[1]) * scale, (p0[2] + du[2]) * scale);
        const v2 = new THREE.Vector3((p0[0] + du[0] + dv[0]) * scale, (p0[1] + du[1] + dv[1]) * scale, (p0[2] + du[2] + dv[2]) * scale);
        const v3 = new THREE.Vector3((p0[0] + dv[0]) * scale, (p0[1] + dv[1]) * scale, (p0[2] + du[2]) * scale);

        let quadPositions: number[];
        if (q.normalSign > 0) {
          quadPositions = [
            v0.x, v0.y, v0.z,  v1.x, v1.y, v1.z,  v2.x, v2.y, v2.z,
            v0.x, v0.y, v0.z,  v2.x, v2.y, v2.z,  v3.x, v3.y, v3.z
          ];
        } else {
          quadPositions = [
            v0.x, v0.y, v0.z,  v2.x, v2.y, v2.z,  v1.x, v1.y, v1.z,
            v0.x, v0.y, v0.z,  v3.x, v3.y, v3.z,  v2.x, v2.y, v2.z
          ];
        }

        if (!positionsByMat.has(q.material)) positionsByMat.set(q.material, []);
        positionsByMat.get(q.material)!.push(...quadPositions);
      });

      positionsByMat.forEach((positions, mat) => {
        if (positions.length === 0) return;
        const geom = new THREE.BufferGeometry();
        geom.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
        geom.computeVertexNormals();

        const pal = VOXEL_PALETTE[mat] || VOXEL_PALETTE[VoxelMaterial.CONCRETE];

        let threeMat: THREE.Material;
        if (currentTool === 'xray' && (mat === VoxelMaterial.COPPER_WIRING || mat === VoxelMaterial.MED_CACHE)) {
          threeMat = new THREE.MeshBasicMaterial({
            color: mat === VoxelMaterial.COPPER_WIRING ? 0xf97316 : 0x10b981,
            wireframe: false
          });
        } else {
          threeMat = new THREE.MeshStandardMaterial({
            color: pal.color,
            roughness: mat === VoxelMaterial.REINFORCED_GLASS ? 0.1 : 0.85,
            metalness: mat === VoxelMaterial.COPPER_WIRING || mat === VoxelMaterial.STEEL_BARRICADE ? 0.8 : 0.1,
            wireframe: showWireframe,
            transparent: mat === VoxelMaterial.REINFORCED_GLASS,
            opacity: mat === VoxelMaterial.REINFORCED_GLASS ? 0.6 : 1.0
          });
        }

        const mesh = new THREE.Mesh(geom, threeMat);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        mesh.userData = { isVoxelMesh: true };
        group.add(mesh);
      });
    } else {
      // Raw unoptimized cubes
      const boxGeo = new THREE.BoxGeometry(VOXEL_SIZE_METERS * 0.95, VOXEL_SIZE_METERS * 0.95, VOXEL_SIZE_METERS * 0.95);
      const chunk = chunkRef.current;

      for (let x = 0; x < CHUNK_SIZE; x++) {
        for (let y = 0; y < CHUNK_SIZE; y++) {
          for (let z = 0; z < CHUNK_SIZE; z++) {
            const mat = chunk.getMaterial(x, y, z);
            if (mat !== VoxelMaterial.AIR) {
              const pal = VOXEL_PALETTE[mat];
              const mesh = new THREE.Mesh(boxGeo, new THREE.MeshStandardMaterial({ color: pal.color, wireframe: showWireframe }));
              mesh.position.set((x + 0.5) * VOXEL_SIZE_METERS, (y + 0.5) * VOXEL_SIZE_METERS, (z + 0.5) * VOXEL_SIZE_METERS);
              group.add(mesh);
            }
          }
        }
      }
    }
  };

  useEffect(() => {
    rebuild3DMesh();
  }, [useGreedyMeshing, showWireframe, currentTool, activeBuildingType]);

  // Reset building
  const handleResetBuilding = (type: typeof activeBuildingType) => {
    setActiveBuildingType(type);
    chunkRef.current = voxelizeOsmBuildingBlock(type);
    setEditsHistory([]);
    rebuild3DMesh();
    soundFx.playRadarPing(880);
  };

  // Drag look
  const handlePointerMove = (e: React.PointerEvent) => {
    if (!isDraggingRef.current) return;
    const dx = e.movementX || (e.clientX - prevMousePos.current.x);
    const dy = e.movementY || (e.clientY - prevMousePos.current.y);
    prevMousePos.current = { x: e.clientX, y: e.clientY };

    if (viewMode === 'firstPerson') {
      fpsLook.current.yaw -= dx * 0.003;
      fpsLook.current.pitch = Math.max(-Math.PI / 2.2, Math.min(Math.PI / 2.2, fpsLook.current.pitch - dy * 0.003));
    } else {
      cameraAngle.current.theta -= dx * 0.008;
      cameraAngle.current.phi = Math.max(0.1, Math.min(Math.PI / 2.1, cameraAngle.current.phi + dy * 0.008));
    }
  };

  // Trigger Mining or Placement at targeted position
  const triggerVoxelAction = (isPlacement: boolean = false) => {
    if (!cameraRef.current || !voxelMeshGroupRef.current) return;

    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(
      viewMode === 'firstPerson' ? new THREE.Vector2(0, 0) : new THREE.Vector2(0, 0),
      cameraRef.current
    );

    const intersects = raycaster.intersectObjects(voxelMeshGroupRef.current.children, true);
    if (intersects.length > 0) {
      const hit = intersects[0];
      const localPoint = voxelMeshGroupRef.current.worldToLocal(hit.point.clone());

      const vx = Math.round(localPoint.x / VOXEL_SIZE_METERS);
      const vy = Math.round(localPoint.y / VOXEL_SIZE_METERS);
      const vz = Math.round(localPoint.z / VOXEL_SIZE_METERS);

      if (!isPlacement && currentTool === 'drill') {
        // Mine / Carve Sphere
        const result = chunkRef.current.destroySphere(vx, vy, vz, brushSize);
        soundFx.playHitSound();

        if (result.destroyedCount > 0) {
          setEditsHistory(prev => [...prev, { x: vx, y: vy, z: vz, mat: VoxelMaterial.AIR }]);
          rebuild3DMesh();

          result.lootFound.forEach(item => {
            const pal = VOXEL_PALETTE[item.mat];
            onCollectLoot(pal.name, item.count);
            soundFx.playLootPickup();
            try {
              confetti({ particleCount: 50, spread: 70, origin: { y: 0.7 } });
            } catch {}
          });
        }
      } else if (isPlacement || currentTool === 'build') {
        // Place Block
        chunkRef.current.setVoxel(vx, vy, vz, selectedBuildMat);
        soundFx.playRadarPing(700);
        setEditsHistory(prev => [...prev, { x: vx, y: vy, z: vz, mat: selectedBuildMat }]);
        rebuild3DMesh();
      }
    }
  };

  // Canvas Click
  const handleCanvasClick = (e: React.MouseEvent) => {
    if (!containerRef.current || !cameraRef.current || !sceneRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const mouse = viewMode === 'firstPerson'
      ? new THREE.Vector2(0, 0)
      : new THREE.Vector2(
          ((e.clientX - rect.left) / rect.width) * 2 - 1,
          -((e.clientY - rect.top) / rect.height) * 2 + 1
        );

    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(mouse, cameraRef.current);

    if (voxelMeshGroupRef.current) {
      const intersects = raycaster.intersectObjects(voxelMeshGroupRef.current.children, true);
      if (intersects.length > 0) {
        const hit = intersects[0];
        const localPoint = voxelMeshGroupRef.current.worldToLocal(hit.point.clone());
        const vx = Math.round(localPoint.x / VOXEL_SIZE_METERS);
        const vy = Math.round(localPoint.y / VOXEL_SIZE_METERS);
        const vz = Math.round(localPoint.z / VOXEL_SIZE_METERS);

        if (currentTool === 'drill') {
          const result = chunkRef.current.destroySphere(vx, vy, vz, brushSize);
          soundFx.playHitSound();

          if (result.destroyedCount > 0) {
            setEditsHistory(prev => [...prev, { x: vx, y: vy, z: vz, mat: VoxelMaterial.AIR }]);
            rebuild3DMesh();

            result.lootFound.forEach(item => {
              const pal = VOXEL_PALETTE[item.mat];
              onCollectLoot(pal.name, item.count);
              soundFx.playLootPickup();
              try {
                confetti({ particleCount: 50, spread: 70, origin: { y: 0.7 } });
              } catch {}
            });
          }
        } else if (currentTool === 'build') {
          chunkRef.current.setVoxel(vx, vy, vz, selectedBuildMat);
          soundFx.playRadarPing(700);
          setEditsHistory(prev => [...prev, { x: vx, y: vy, z: vz, mat: selectedBuildMat }]);
          rebuild3DMesh();
        }
      }
    }
  };

  // Export Godot 4 ZIP Archive
  const handleExportFullGodotZip = async () => {
    setIsExportingZip(true);
    try {
      const zip = new JSZip();

      zip.file('project.godot', `; Engine configuration file for Godot 4.3 (.NET / C#)
config_version=5

[application]
config/name="TerreZero_MicroVoxel_Godot4"
config/description="Terre Zero - Micro-Voxel Post-Apo Survival Game (1 voxel = 20cm) with Greedy Meshing"
run/main_scene="res://scenes/MainWorld.tscn"
config/features=PackedStringArray("4.3", "C#", "Forward Plus")

[dotnet]
project/assembly_name="TerreZero_MicroVoxel_Godot4"
`);

      const scripts = zip.folder('scripts');
      scripts?.file('VoxelChunk.cs', GODOT4_VOXEL_CHUNK_CS);
      scripts?.file('GreedyMesher.cs', GODOT4_GREEDY_MESHER_CS);
      scripts?.file('OSMVoxelizer.cs', GODOT4_OSM_VOXELIZER_CS);
      scripts?.file('KalmanGpsFilter.cs', `using System;
using Godot;

namespace TerreZero.GPS
{
    public class KalmanGpsFilter
    {
        private double _lat;
        private double _lon;
        private double _variance = -1.0;
        private readonly double _qMetersPerSecond = 3.0;
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

        public bool ProcessGpsSample(double rawLat, double rawLon, double accuracyMeters, ulong timestampMs, out string antiCheatWarning)
        {
            antiCheatWarning = null;
            if (_variance < 0) { SetInitialPosition(rawLat, rawLon, accuracyMeters); return true; }
            ulong dtMs = timestampMs - _lastTimestampMs;
            if (dtMs <= 0) return false;
            double dtSec = dtMs / 1000.0;

            double rawDistMeters = ComputeHaversineDistance(_lat, _lon, rawLat, rawLon);
            _currentSpeedKmH = (rawDistMeters / dtSec) * 3.6;
            if (_currentSpeedKmH > 30.0 && rawDistMeters > 30.0)
            {
                antiCheatWarning = $"[ANTI-CHEAT] Vitesse anormale : {_currentSpeedKmH:F1} km/h (Max: 30 km/h)";
                return false;
            }

            _variance += dtSec * _qMetersPerSecond * _qMetersPerSecond;
            double rVariance = accuracyMeters * accuracyMeters;
            double kGain = _variance / (_variance + rVariance);
            _lat += kGain * (rawLat - _lat);
            _lon += kGain * (rawLon - _lon);
            _variance = (1.0 - kGain) * _variance;
            _lastTimestampMs = timestampMs;
            return true;
        }

        public static double ComputeHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            double dLat = (lat2 - lat1) * (Math.PI / 180.0);
            double dLon = (lon2 - lon1) * (Math.PI / 180.0);
            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }
    }
}`);
      scripts?.file('ChimereManager.cs', `using System;
using Godot;

namespace TerreZero.Chimeres
{
    public enum ChimereType { MechaDrone, BioMutant, RadioactiveBeast }
    public enum BunkerRole { None, DefenseTurret, PowerGenerator, RadarScout, CargoMule }

    public class ChimereInstance
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ChimereType Type { get; set; }
        public int Level { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public BunkerRole AssignedRole { get; set; } = BunkerRole.None;

        public float CalculateCaptureChance(float chipMultiplier = 1.0f)
        {
            float hpFactor = (1.0f - ((float)CurrentHp / MaxHp)) * 0.7f;
            return Mathf.Clamp((0.25f + hpFactor) * chipMultiplier, 0.05f, 0.95f);
        }
    }
}`);
      scripts?.file('BunkerManager.cs', `using System;
using Godot;

namespace TerreZero.Bunker
{
    public class BunkerData
    {
        public bool IsAnchored { get; set; } = false;
        public double AnchorLat { get; set; }
        public double AnchorLon { get; set; }
        public int EnergyLevelKW { get; set; } = 40;
        public int RawScrap { get; set; } = 45;
        public int RefinedTitaniumPlates { get; set; } = 8;
        public int ElectronicCircuits { get; set; } = 12;

        public bool AnchorHome(double lat, double lon)
        {
            AnchorLat = lat; AnchorLon = lon; IsAnchored = true;
            GD.Print($"[BUNKER] Abri ancré aux coordonnées : {lat:F5}, {lon:F5}");
            return true;
        }
    }
}`);
      scripts?.file('DeltaSyncManager.cs', `using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace TerreZero.Network
{
    public class VoxelDeltaEvent
    {
        public string h3_index { get; set; }
        public int[] chunk_coords { get; set; }
        public int[] local_voxel { get; set; }
        public string action { get; set; }
        public byte material_id { get; set; }
        public string player_id { get; set; }
    }
}`);

      const shaders = zip.folder('shaders');
      shaders?.file('retro_voxel.gdshader', `shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_toon;
void fragment() {
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 0.85;
}
`);

      zip.file('README_GODOT.md', `# Projet Godot 4 (C# / .NET) : Terre Zéro (Micro-Voxel 20cm)
1. Ouvrez Godot Engine 4.3 .NET.
2. Cliquez sur 'Importer' et sélectionnez 'project.godot'.
3. Cliquez sur 'Build' (C# .NET) puis appuyez sur F5 pour jouer !
`);

      const content = await zip.generateAsync({ type: 'blob' });
      const url = URL.createObjectURL(content);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'Godot4_TerreZero_MicroVoxel_Project.zip';
      a.click();
      URL.revokeObjectURL(url);
      soundFx.playCaptureSuccess();
    } catch {
      alert("Erreur lors de l'export du projet ZIP.");
    } finally {
      setIsExportingZip(false);
    }
  };

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-100 p-4 select-none overflow-y-auto">
      {/* Top Banner */}
      <div className="bg-gradient-to-r from-orange-950/40 via-slate-900 to-slate-950 border border-orange-500/40 rounded-2xl p-4 mb-4 flex items-center justify-between shadow-xl">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-orange-500/20 border border-orange-500 flex items-center justify-center text-2xl shadow-[0_0_15px_rgba(249,115,22,0.3)]">
            🧊
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold font-tech text-white">Terre Zéro : Moteur Micro-Voxel Godot 4 (20cm)</h2>
              <span className="text-[10px] bg-orange-950 text-orange-300 border border-orange-800 px-2 py-0.5 rounded font-mono">
                1 Voxel = 20cm · Greedy Meshing (-98% triangles)
              </span>
            </div>
            <p className="text-xs text-slate-400 font-mono">
              Monde réel en cubes miniatures destructibles, scanner radar de cuivre et export complet du projet Godot 4 (.ZIP).
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleExportFullGodotZip}
            disabled={isExportingZip}
            className="px-3.5 py-2 bg-gradient-to-r from-orange-600 to-amber-600 hover:from-orange-500 hover:to-amber-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg transition-all flex items-center gap-1.5 active:scale-95"
          >
            <FolderArchive className="w-4 h-4" />
            <span>{isExportingZip ? 'Génération...' : 'Télécharger Projet Godot 4 (.ZIP)'}</span>
          </button>
        </div>
      </div>

      {/* Main Grid: 3D Viewport on Left + Live Workbench on Right */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 flex-1 min-h-[520px]">
        {/* LEFT 2 COLS: 3D Interactive Voxel Canvas */}
        <div className="lg:col-span-2 bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden flex flex-col relative shadow-2xl">
          {/* Top Canvas Toolbar */}
          <div className="absolute top-3 left-3 right-3 flex items-center justify-between z-10 pointer-events-none">
            {/* View Mode Toggle: Orbit vs First-Person */}
            <div className="flex items-center gap-1.5 bg-slate-950/90 backdrop-blur border border-slate-800 p-1 rounded-xl shadow-lg pointer-events-auto">
              <button
                onClick={() => {
                  setViewMode('firstPerson');
                  soundFx.playRadarPing(800);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold flex items-center gap-1.5 transition-all ${
                  viewMode === 'firstPerson' ? 'bg-orange-500 text-slate-950 shadow' : 'text-slate-400 hover:text-white'
                }`}
              >
                <Gamepad2 className="w-3.5 h-3.5" />
                <span>Jouer (Z/Q/S/D)</span>
              </button>

              <button
                onClick={() => {
                  setViewMode('orbit');
                  soundFx.playRadarPing(600);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold flex items-center gap-1.5 transition-all ${
                  viewMode === 'orbit' ? 'bg-orange-500 text-slate-950 shadow' : 'text-slate-400 hover:text-white'
                }`}
              >
                <Eye className="w-3.5 h-3.5" />
                <span>Vue Libre</span>
              </button>
            </div>

            {/* Tool Selection */}
            <div className="flex items-center gap-1.5 bg-slate-950/90 backdrop-blur border border-slate-800 p-1 rounded-xl shadow-lg pointer-events-auto">
              <button
                onClick={() => {
                  setCurrentTool('drill');
                  soundFx.playRadarPing(700);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold flex items-center gap-1.5 transition-all ${
                  currentTool === 'drill' ? 'bg-red-500 text-white shadow' : 'text-slate-400 hover:text-white'
                }`}
                title="Minage & Forage au Laser dans la façade"
              >
                <Flame className="w-3.5 h-3.5" />
                <span>Foreuse Laser</span>
              </button>

              <button
                onClick={() => {
                  setCurrentTool('xray');
                  soundFx.playRadarPing(900);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold flex items-center gap-1.5 transition-all ${
                  currentTool === 'xray' ? 'bg-emerald-500 text-slate-950 shadow' : 'text-slate-400 hover:text-white'
                }`}
                title="Sonde & Scanner radar révélant le cuivre et trousses dans les cloisons (Touche F)"
              >
                <Radio className="w-3.5 h-3.5" />
                <span>Scanner X-Ray (F)</span>
              </button>

              <button
                onClick={() => {
                  setCurrentTool('build');
                  soundFx.playRadarPing(800);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-tech font-bold flex items-center gap-1.5 transition-all ${
                  currentTool === 'build' ? 'bg-cyan-500 text-slate-950 shadow' : 'text-slate-400 hover:text-white'
                }`}
                title="Mode Construction de blocs voxel (Bunker)"
              >
                <Hammer className="w-3.5 h-3.5" />
                <span>Poser Blocs</span>
              </button>
            </div>

            {/* Optimization Switch */}
            <div className="flex items-center gap-2 pointer-events-auto">
              <button
                onClick={() => setUseGreedyMeshing(!useGreedyMeshing)}
                className={`px-3 py-1.5 rounded-xl text-xs font-mono font-bold border flex items-center gap-1.5 transition-all shadow ${
                  useGreedyMeshing ? 'bg-emerald-950/80 border-emerald-500 text-emerald-300' : 'bg-red-950/80 border-red-500 text-red-300'
                }`}
              >
                <Layers className="w-3.5 h-3.5" />
                <span>Greedy : {useGreedyMeshing ? 'ACTIF (-98%)' : 'BRUT'}</span>
              </button>
            </div>
          </div>

          {/* 3D Mount */}
          <div
            ref={containerRef}
            className="w-full h-full cursor-crosshair min-h-[420px]"
            onPointerDown={(e) => {
              isDraggingRef.current = true;
              prevMousePos.current = { x: e.clientX, y: e.clientY };
            }}
            onPointerMove={handlePointerMove}
            onPointerUp={() => {
              isDraggingRef.current = false;
            }}
            onClick={handleCanvasClick}
          />

          {/* First-Person HUD Crosshair & On-Screen Touch / Action Controls */}
          {viewMode === 'firstPerson' && (
            <div className="absolute inset-0 pointer-events-none flex flex-col justify-between p-4">
              {/* Crosshair Center */}
              <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 flex items-center justify-center">
                <Crosshair className="w-6 h-6 text-cyan-400 opacity-90 animate-pulse" />
              </div>

              {/* Top Left Navigation Helper */}
              <div className="bg-slate-950/80 backdrop-blur border border-slate-800 rounded-xl p-2 self-start text-[11px] font-mono text-slate-300 pointer-events-auto">
                <div>🕹️ <strong>Z / Q / S / D :</strong> Déplacement</div>
                <div>⛏️ <strong>Clic :</strong> Forer / Miner</div>
                <div>📡 <strong>F :</strong> Scanner X-Ray</div>
              </div>

              {/* Bottom Touch Controls & Hotbar */}
              <div className="flex flex-col items-center gap-2">
                {/* Quick Action Buttons for Touch / Mobile */}
                <div className="flex items-center gap-2 pointer-events-auto">
                  <button
                    onClick={() => triggerVoxelAction(false)}
                    className="px-4 py-2 bg-red-600 hover:bg-red-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg active:scale-95 flex items-center gap-1.5"
                  >
                    <Flame className="w-4 h-4" />
                    <span>Forer / Miner</span>
                  </button>

                  <button
                    onClick={() => triggerVoxelAction(true)}
                    className="px-4 py-2 bg-cyan-600 hover:bg-cyan-500 text-white font-tech font-bold text-xs rounded-xl shadow-lg active:scale-95 flex items-center gap-1.5"
                  >
                    <Hammer className="w-4 h-4" />
                    <span>Poser Bloc</span>
                  </button>

                  <button
                    onClick={() => {
                      setCurrentTool(prev => (prev === 'xray' ? 'drill' : 'xray'));
                      soundFx.playRadarPing(900);
                    }}
                    className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-slate-950 font-tech font-bold text-xs rounded-xl shadow-lg active:scale-95 flex items-center gap-1.5"
                  >
                    <Radio className="w-4 h-4" />
                    <span>Scanner (F)</span>
                  </button>
                </div>

                {/* Hotbar (1-9) */}
                <div className="bg-slate-950/90 border-2 border-slate-700 rounded-2xl p-1.5 flex items-center gap-1 shadow-2xl pointer-events-auto overflow-x-auto max-w-full">
                  {[
                    { id: 1, mat: VoxelMaterial.CONCRETE, name: 'Béton', icon: '🧱' },
                    { id: 2, mat: VoxelMaterial.BRICK, name: 'Brique', icon: '🧱' },
                    { id: 3, mat: VoxelMaterial.ASPHALT, name: 'Asphalte', icon: '🛣️' },
                    { id: 4, mat: VoxelMaterial.SIDEWALK, name: 'Trottoir', icon: '🚶' },
                    { id: 5, mat: VoxelMaterial.REINFORCED_GLASS, name: 'Verre', icon: '🪟' },
                    { id: 6, mat: VoxelMaterial.COPPER_WIRING, name: 'Cuivre', icon: '⚡' },
                    { id: 7, mat: VoxelMaterial.MED_CACHE, name: 'Soin', icon: '💉' },
                    { id: 8, mat: VoxelMaterial.STEEL_BARRICADE, name: 'Titane', icon: '🛡️' },
                    { id: 9, mat: VoxelMaterial.TURRET_BASE, name: 'Tourelle', icon: '🔫' }
                  ].map(slot => (
                    <button
                      key={slot.id}
                      onClick={() => {
                        setSelectedBuildMat(slot.mat);
                        setCurrentTool('build');
                      }}
                      className={`w-10 h-10 rounded-xl flex flex-col items-center justify-center text-xs transition-all relative ${
                        selectedBuildMat === slot.mat && currentTool === 'build'
                          ? 'bg-amber-500 text-slate-950 font-bold border-2 border-white scale-110 shadow-lg'
                          : 'bg-slate-900 text-slate-300 border border-slate-800 hover:bg-slate-800'
                      }`}
                    >
                      <span className="text-sm">{slot.icon}</span>
                      <span className="text-[8px] font-mono absolute bottom-0.5 right-1 text-slate-400">{slot.id}</span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Bottom Diagnostic Bar */}
          <div className="absolute bottom-3 left-3 right-3 bg-slate-950/90 backdrop-blur-md border border-slate-800 rounded-xl p-2.5 flex items-center justify-between text-xs font-mono">
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1.5">
                <span className="text-slate-400">Triangles bruts :</span>
                <span className="text-red-400 font-bold">{meshingStats?.rawTrianglesCount.toLocaleString() || '0'}</span>
              </div>
              <span>→</span>
              <div className="flex items-center gap-1.5">
                <span className="text-slate-400">Greedy Meshing :</span>
                <span className="text-emerald-400 font-bold">{meshingStats?.greedyTrianglesCount.toLocaleString() || '0'}</span>
              </div>
              <span className="text-[10px] bg-emerald-950 text-emerald-400 border border-emerald-800 px-2 py-0.5 rounded-full font-bold">
                -{meshingStats?.reductionPercentage || 0}% GPU
              </span>
            </div>

            <div className="text-slate-400 text-[11px] hidden sm:block">
              {currentTool === 'drill' ? '🔥 Visez et cliquez pour forer des brèches' : currentTool === 'xray' ? '📡 Scanner actif : Cuivre (Orange) & Trousses (Vert)' : '🔨 Pose de blocs voxel'}
            </div>
          </div>
        </div>

        {/* RIGHT COL: Godot 4 Code Export & Delta Network Sync */}
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col justify-between shadow-2xl">
          <div>
            {/* Header */}
            <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-3">
              <div className="flex items-center gap-2">
                <FileCode className="w-5 h-5 text-orange-400" />
                <h3 className="font-tech text-sm font-bold text-white uppercase">Scripts C# Godot 4 (.NET)</h3>
              </div>
            </div>

            {/* Script Tabs */}
            <div className="grid grid-cols-3 gap-1.5 mb-3">
              <button
                onClick={() => setActiveScriptTab('chunk')}
                className={`py-1.5 px-2 rounded-lg text-xs font-mono font-bold transition-all ${
                  activeScriptTab === 'chunk' ? 'bg-orange-500 text-slate-950' : 'bg-slate-950 text-slate-400 hover:text-white'
                }`}
              >
                VoxelChunk.cs
              </button>
              <button
                onClick={() => setActiveScriptTab('mesher')}
                className={`py-1.5 px-2 rounded-lg text-xs font-mono font-bold transition-all ${
                  activeScriptTab === 'mesher' ? 'bg-orange-500 text-slate-950' : 'bg-slate-950 text-slate-400 hover:text-white'
                }`}
              >
                GreedyMesher.cs
              </button>
              <button
                onClick={() => setActiveScriptTab('osm')}
                className={`py-1.5 px-2 rounded-lg text-xs font-mono font-bold transition-all ${
                  activeScriptTab === 'osm' ? 'bg-orange-500 text-slate-950' : 'bg-slate-950 text-slate-400 hover:text-white'
                }`}
              >
                OSMVoxelizer.cs
              </button>
            </div>

            {/* Code Box */}
            <div className="bg-slate-950 border border-slate-800/80 rounded-xl p-3 overflow-y-auto font-mono text-[11px] text-slate-300 max-h-52 leading-relaxed">
              <pre className="whitespace-pre-wrap select-text">
                {activeScriptTab === 'chunk' && GODOT4_VOXEL_CHUNK_CS}
                {activeScriptTab === 'mesher' && GODOT4_GREEDY_MESHER_CS}
                {activeScriptTab === 'osm' && GODOT4_OSM_VOXELIZER_CS}
              </pre>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2 mt-3">
              <button
                onClick={() => {
                  const code = activeScriptTab === 'chunk' ? GODOT4_VOXEL_CHUNK_CS : activeScriptTab === 'mesher' ? GODOT4_GREEDY_MESHER_CS : GODOT4_OSM_VOXELIZER_CS;
                  navigator.clipboard.writeText(code);
                  setCopiedScript(activeScriptTab);
                  setTimeout(() => setCopiedScript(null), 2000);
                }}
                className="flex-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-mono rounded-xl border border-slate-700 transition-all flex items-center justify-center gap-1.5"
              >
                {copiedScript === activeScriptTab ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
                <span>{copiedScript === activeScriptTab ? 'Copié !' : 'Copier le script C#'}</span>
              </button>
            </div>
          </div>

          {/* Sprint 4.1 : Delta Network Sync Stats */}
          <div className="bg-slate-950 border border-slate-800 rounded-xl p-3 mt-3">
            <div className="flex items-center justify-between text-xs font-mono text-slate-400 mb-1">
              <span>Synchronisation Delta (Sprint 4.1) :</span>
              <span className="text-cyan-400 font-bold">{editsHistory.length} modifications</span>
            </div>
            <div className="text-[11px] font-mono text-slate-400">
              Taille paquet RLE : <strong>{(serializeVoxelDeltaRLE(editsHistory).length)} octets</strong> (vs 64 Ko chunk brut)
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
