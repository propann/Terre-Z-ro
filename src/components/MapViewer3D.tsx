import React, { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';
import {
  ParsedBuilding,
  ParsedRoad,
  ParsedPark,
  LootSpot,
  Chimere,
  H3Tile,
  PlayerState
} from '../types/game';
import { latLonToLocalMeters, getDistanceMeters } from '../services/osmParser';
import { Compass, Eye, Maximize2, Move, RotateCcw, ShieldAlert, Crosshair } from 'lucide-react';

interface MapViewer3DProps {
  buildings: ParsedBuilding[];
  roads: ParsedRoad[];
  parks: ParsedPark[];
  lootSpots: LootSpot[];
  chimeres: Chimere[];
  h3Tiles: H3Tile[];
  player: PlayerState;
  centerLat: number;
  centerLon: number;
  onLootClick: (loot: LootSpot) => void;
  onChimereClick: (chimere: Chimere) => void;
  onPlayerMove: (newLat: number, newLon: number) => void;
}

export const MapViewer3D: React.FC<MapViewer3DProps> = ({
  buildings,
  roads,
  parks,
  lootSpots,
  chimeres,
  h3Tiles,
  player,
  centerLat,
  centerLon,
  onLootClick,
  onChimereClick,
  onPlayerMove
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<THREE.Scene | null>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);
  const playerMeshRef = useRef<THREE.Group | null>(null);
  const interactablesGroupRef = useRef<THREE.Group | null>(null);
  const animationFrameRef = useRef<number | null>(null);

  const [showH3, setShowH3] = useState<boolean>(true);
  const [cameraMode, setCameraMode] = useState<'follow' | 'free'>('follow');
  const [selectedEntityInfo, setSelectedEntityInfo] = useState<string | null>(null);

  // Mouse drag for camera orbit
  const isDraggingRef = useRef(false);
  const prevMousePos = useRef({ x: 0, y: 0 });
  const cameraAngle = useRef({ theta: Math.PI / 4, phi: Math.PI / 3.5, radius: 180 });

  // Initialize Three.js Scene
  useEffect(() => {
    if (!containerRef.current) return;
    const width = containerRef.current.clientWidth;
    const height = containerRef.current.clientHeight;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x060913);
    scene.fog = new THREE.FogExp2(0x060913, 0.0035);
    sceneRef.current = scene;

    const camera = new THREE.PerspectiveCamera(45, width / height, 1, 2000);
    cameraRef.current = camera;

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false, powerPreference: 'high-performance' });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    rendererRef.current = renderer;

    containerRef.current.replaceChildren(renderer.domElement);

    // Ambient & Tactical Lights
    const ambientLight = new THREE.AmbientLight(0x4a5568, 1.2);
    scene.add(ambientLight);

    const dirLight = new THREE.DirectionalLight(0xf97316, 1.5);
    dirLight.position.set(150, 250, 100);
    dirLight.castShadow = true;
    dirLight.shadow.mapSize.width = 1024;
    dirLight.shadow.mapSize.height = 1024;
    scene.add(dirLight);

    const hemiLight = new THREE.HemisphereLight(0x0284c7, 0x1e293b, 0.8);
    scene.add(hemiLight);

    // Ground Plane with Cyber Grid
    const groundGeo = new THREE.PlaneGeometry(1200, 1200, 40, 40);
    const groundMat = new THREE.MeshStandardMaterial({
      color: 0x090d16,
      roughness: 0.9,
      metalness: 0.1
    });
    const ground = new THREE.Mesh(groundGeo, groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = -0.1;
    ground.receiveShadow = true;
    scene.add(ground);

    const gridHelper = new THREE.GridHelper(1000, 50, 0x0284c7, 0x1e293b);
    gridHelper.position.y = 0;
    scene.add(gridHelper);

    // Player Avatar 3D Group
    const playerGroup = new THREE.Group();
    
    // Beacon / Capsule
    const bodyGeo = new THREE.CylinderGeometry(1.2, 1.5, 4, 8);
    const bodyMat = new THREE.MeshStandardMaterial({ color: 0x06b6d4, emissive: 0x0891b2, emissiveIntensity: 0.5 });
    const body = new THREE.Mesh(bodyGeo, bodyMat);
    body.position.y = 2;
    body.castShadow = true;
    playerGroup.add(body);

    // Heading Pointer
    const coneGeo = new THREE.ConeGeometry(1.5, 3, 4);
    const coneMat = new THREE.MeshStandardMaterial({ color: 0xf97316, emissive: 0xea580c });
    const cone = new THREE.Mesh(coneGeo, coneMat);
    cone.rotation.x = Math.PI / 2;
    cone.position.set(0, 2.5, -3);
    playerGroup.add(cone);

    // Range Radius Ring (20m Interaction Radius)
    const ringGeo = new THREE.RingGeometry(19.8, 20.2, 64);
    const ringMat = new THREE.MeshBasicMaterial({ color: 0x06b6d4, side: THREE.DoubleSide, transparent: true, opacity: 0.6 });
    const ring = new THREE.Mesh(ringGeo, ringMat);
    ring.rotation.x = -Math.PI / 2;
    ring.position.y = 0.2;
    playerGroup.add(ring);

    // Radar 50m Scanning Pulse Ring
    const radarRingGeo = new THREE.RingGeometry(49.5, 50.5, 64);
    const radarRingMat = new THREE.MeshBasicMaterial({ color: 0xf97316, side: THREE.DoubleSide, transparent: true, opacity: 0.3 });
    const radarRing = new THREE.Mesh(radarRingGeo, radarRingMat);
    radarRing.rotation.x = -Math.PI / 2;
    radarRing.position.y = 0.15;
    playerGroup.add(radarRing);

    scene.add(playerGroup);
    playerMeshRef.current = playerGroup;

    // Interactables container
    const interactablesGroup = new THREE.Group();
    scene.add(interactablesGroup);
    interactablesGroupRef.current = interactablesGroup;

    // Handle Resize
    const handleResize = () => {
      if (!containerRef.current || !rendererRef.current || !cameraRef.current) return;
      const w = containerRef.current.clientWidth;
      const h = containerRef.current.clientHeight;
      cameraRef.current.aspect = w / h;
      cameraRef.current.updateProjectionMatrix();
      rendererRef.current.setSize(w, h);
    };
    window.addEventListener('resize', handleResize);

    // Render loop
    let clock = new THREE.Clock();
    const animate = () => {
      animationFrameRef.current = requestAnimationFrame(animate);
      const delta = clock.getDelta();
      const elapsed = clock.getElapsedTime();

      // Pulse rings
      if (playerMeshRef.current) {
        ring.rotation.z = elapsed * 0.5;
        radarRing.rotation.z = -elapsed * 0.3;
      }

      // Bob floating loot & chimères
      if (interactablesGroupRef.current) {
        interactablesGroupRef.current.children.forEach((child) => {
          if (child.userData.type === 'loot') {
            child.position.y = 2.5 + Math.sin(elapsed * 3 + (child.userData.id ? child.userData.id.charCodeAt(5) || 0 : 0)) * 0.6;
            child.rotation.y += delta * 1.5;
          } else if (child.userData.type === 'chimere') {
            child.position.y = 2.0 + Math.sin(elapsed * 2 + 1) * 0.4;
            child.rotation.y += delta * 0.8;
          }
        });
      }

      // Update camera position
      if (cameraRef.current) {
        const { theta, phi, radius } = cameraAngle.current;
        const targetX = playerMeshRef.current ? playerMeshRef.current.position.x : 0;
        const targetZ = playerMeshRef.current ? playerMeshRef.current.position.z : 0;

        const cx = targetX + radius * Math.sin(phi) * Math.sin(theta);
        const cy = radius * Math.cos(phi);
        const cz = targetZ + radius * Math.sin(phi) * Math.cos(theta);

        cameraRef.current.position.set(cx, cy, cz);
        cameraRef.current.lookAt(targetX, 3, targetZ);
      }

      renderer.render(scene, camera);
    };

    animate();

    return () => {
      window.removeEventListener('resize', handleResize);
      if (animationFrameRef.current) cancelAnimationFrame(animationFrameRef.current);
      renderer.dispose();
    };
  }, []);

  // Update Player Position in Scene
  useEffect(() => {
    if (!playerMeshRef.current) return;
    const { x, z } = latLonToLocalMeters(player.lat, player.lon, centerLat, centerLon);
    playerMeshRef.current.position.set(x, 0, z);
    playerMeshRef.current.rotation.y = (player.heading * Math.PI) / 180;
  }, [player.lat, player.lon, player.heading, centerLat, centerLon]);

  // Build Buildings, Roads, Parks, H3 Grid Meshes
  useEffect(() => {
    const scene = sceneRef.current;
    if (!scene) return;

    // Remove existing static world groups
    const toRemove = scene.children.filter(c => c.userData.isWorldGeometry);
    toRemove.forEach(c => scene.remove(c));

    const worldGroup = new THREE.Group();
    worldGroup.userData.isWorldGeometry = true;

    // 1. Extrude Buildings with Voxel/Degraded styling
    const concreteMat = new THREE.MeshStandardMaterial({
      color: 0x334155,
      roughness: 0.85,
      metalness: 0.2
    });
    const industrialMat = new THREE.MeshStandardMaterial({
      color: 0x78350f,
      roughness: 0.7,
      metalness: 0.5
    });
    const commercialMat = new THREE.MeshStandardMaterial({
      color: 0x1e293b,
      roughness: 0.6,
      metalness: 0.3
    });
    const ruinsMat = new THREE.MeshStandardMaterial({
      color: 0x475569,
      roughness: 0.95,
      wireframe: false
    });

    buildings.forEach((b) => {
      if (b.polygon.length < 3) return;

      const shape = new THREE.Shape();
      const first = latLonToLocalMeters(b.polygon[0][0], b.polygon[0][1], centerLat, centerLon);
      shape.moveTo(first.x, -first.z);

      for (let i = 1; i < b.polygon.length; i++) {
        const p = latLonToLocalMeters(b.polygon[i][0], b.polygon[i][1], centerLat, centerLon);
        shape.lineTo(p.x, -p.z);
      }
      shape.closePath();

      const extrudeSettings = {
        depth: Math.max(3, b.height),
        bevelEnabled: true,
        bevelSegments: 1,
        steps: 1,
        bevelSize: 0.3,
        bevelThickness: 0.3
      };

      try {
        const geom = new THREE.ExtrudeGeometry(shape, extrudeSettings);
        geom.rotateX(-Math.PI / 2); // Make Y up

        let mat = concreteMat;
        if (b.type === 'industrial') mat = industrialMat;
        else if (b.type === 'commercial') mat = commercialMat;
        else if (b.type === 'ruins') mat = ruinsMat;

        const mesh = new THREE.Mesh(geom, mat);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        mesh.userData = { isBuilding: true, id: b.id, name: b.name, height: b.height, levels: b.levels };

        // Add subtle edge highlight for tactical wireframe feel
        const wireGeo = new THREE.EdgesGeometry(geom, 25);
        const wireMat = new THREE.LineBasicMaterial({ color: 0x64748b, transparent: true, opacity: 0.35 });
        const wire = new THREE.LineSegments(wireGeo, wireMat);
        mesh.add(wire);

        worldGroup.add(mesh);
      } catch {
        // Skip malformed polygon
      }
    });

    // 2. Roads
    const roadMat = new THREE.LineBasicMaterial({ color: 0x38bdf8, linewidth: 2, transparent: true, opacity: 0.6 });
    const crevassedMat = new THREE.LineBasicMaterial({ color: 0xf59e0b, linewidth: 2, transparent: true, opacity: 0.8 });

    roads.forEach((road) => {
      if (road.points.length < 2) return;
      const points: THREE.Vector3[] = [];
      road.points.forEach(([lat, lon]) => {
        const { x, z } = latLonToLocalMeters(lat, lon, centerLat, centerLon);
        points.push(new THREE.Vector3(x, 0.1, z));
      });
      const geom = new THREE.BufferGeometry().setFromPoints(points);
      const line = new THREE.Line(geom, road.isCrevassed ? crevassedMat : roadMat);
      worldGroup.add(line);
    });

    // 3. Parks (Lush Green / Bioluminescent zones)
    parks.forEach((park) => {
      if (park.polygon.length < 3) return;
      const shape = new THREE.Shape();
      const first = latLonToLocalMeters(park.polygon[0][0], park.polygon[0][1], centerLat, centerLon);
      shape.moveTo(first.x, -first.z);

      for (let i = 1; i < park.polygon.length; i++) {
        const p = latLonToLocalMeters(park.polygon[i][0], park.polygon[i][1], centerLat, centerLon);
        shape.lineTo(p.x, -p.z);
      }
      shape.closePath();

      try {
        const geom = new THREE.ShapeGeometry(shape);
        geom.rotateX(-Math.PI / 2);
        const parkMat = new THREE.MeshBasicMaterial({
          color: 0x064e3b,
          transparent: true,
          opacity: 0.5,
          side: THREE.DoubleSide
        });
        const mesh = new THREE.Mesh(geom, parkMat);
        mesh.position.y = 0.05;
        worldGroup.add(mesh);
      } catch {}
    });

    // 4. H3 Hexagonal Grid
    if (showH3) {
      const hexLineMat = new THREE.LineBasicMaterial({ color: 0x0284c7, transparent: true, opacity: 0.3 });
      const radHexLineMat = new THREE.LineBasicMaterial({ color: 0xd97706, transparent: true, opacity: 0.4 });

      h3Tiles.forEach((tile) => {
        if (tile.polygon.length < 6) return;
        const pts: THREE.Vector3[] = [];
        tile.polygon.forEach(([lat, lon]) => {
          const { x, z } = latLonToLocalMeters(lat, lon, centerLat, centerLon);
          pts.push(new THREE.Vector3(x, 0.08, z));
        });
        // Close hexagon loop
        pts.push(pts[0]);

        const geom = new THREE.BufferGeometry().setFromPoints(pts);
        const hexLine = new THREE.Line(geom, tile.radiationLevel > 40 ? radHexLineMat : hexLineMat);
        worldGroup.add(hexLine);
      });
    }

    scene.add(worldGroup);
  }, [buildings, roads, parks, h3Tiles, centerLat, centerLon, showH3]);

  // Update Interactable Loot & Chimère meshes
  useEffect(() => {
    const group = interactablesGroupRef.current;
    if (!group) return;

    // Clear previous children
    group.clear();

    // 1. Loot Crates (Floating Cube with beacon light)
    const lootGeo = new THREE.BoxGeometry(2.5, 2.5, 2.5);
    const lootMat = new THREE.MeshStandardMaterial({
      color: 0xf59e0b,
      emissive: 0xd97706,
      emissiveIntensity: 0.8,
      roughness: 0.3
    });
    const medicalMat = new THREE.MeshStandardMaterial({
      color: 0x10b981,
      emissive: 0x059669,
      emissiveIntensity: 0.8,
      roughness: 0.3
    });

    lootSpots.forEach((loot) => {
      if (loot.looted) return;
      const { x, z } = latLonToLocalMeters(loot.lat, loot.lon, centerLat, centerLon);

      const lootMesh = new THREE.Mesh(lootGeo, loot.category === 'medical' ? medicalMat : lootMat);
      lootMesh.position.set(x, 2.5, z);
      lootMesh.castShadow = true;
      lootMesh.userData = { type: 'loot', data: loot, id: loot.id };

      // Pillar beam of light
      const beamGeo = new THREE.CylinderGeometry(0.1, 0.1, 20, 8);
      const beamMat = new THREE.MeshBasicMaterial({
        color: loot.category === 'medical' ? 0x10b981 : 0xf59e0b,
        transparent: true,
        opacity: 0.4
      });
      const beam = new THREE.Mesh(beamGeo, beamMat);
      beam.position.y = 10;
      lootMesh.add(beam);

      group.add(lootMesh);
    });

    // 2. Chimères (Pyramid / Sphere with distinct aura)
    const mechGeo = new THREE.OctahedronGeometry(2.2);
    const mechMat = new THREE.MeshStandardMaterial({
      color: 0xef4444,
      emissive: 0xb91c1c,
      emissiveIntensity: 0.9,
      roughness: 0.2
    });

    const bioGeo = new THREE.IcosahedronGeometry(2.2);
    const bioMat = new THREE.MeshStandardMaterial({
      color: 0xa855f7,
      emissive: 0x7e22ce,
      emissiveIntensity: 0.9,
      roughness: 0.4
    });

    chimeres.forEach((chim) => {
      if (chim.captured || !chim.lat || !chim.lon) return;
      const { x, z } = latLonToLocalMeters(chim.lat, chim.lon, centerLat, centerLon);

      const chimMesh = new THREE.Mesh(chim.type === 'mechanical' ? mechGeo : bioGeo, chim.type === 'mechanical' ? mechMat : bioMat);
      chimMesh.position.set(x, 2.0, z);
      chimMesh.castShadow = true;
      chimMesh.userData = { type: 'chimere', data: chim, id: chim.id };

      // Hazard aura ring
      const auraGeo = new THREE.RingGeometry(3.5, 4.0, 16);
      const auraMat = new THREE.MeshBasicMaterial({
        color: chim.type === 'mechanical' ? 0xef4444 : 0xa855f7,
        side: THREE.DoubleSide,
        transparent: true,
        opacity: 0.7
      });
      const aura = new THREE.Mesh(auraGeo, auraMat);
      aura.rotation.x = -Math.PI / 2;
      aura.position.y = -1.8;
      chimMesh.add(aura);

      group.add(chimMesh);
    });
  }, [lootSpots, chimeres, centerLat, centerLon]);

  // Click & Drag Handlers for Three.js Viewport
  const handlePointerDown = (e: React.PointerEvent) => {
    isDraggingRef.current = true;
    prevMousePos.current = { x: e.clientX, y: e.clientY };
  };

  const handlePointerMove = (e: React.PointerEvent) => {
    if (!isDraggingRef.current) return;
    const dx = e.clientX - prevMousePos.current.x;
    const dy = e.clientY - prevMousePos.current.y;
    prevMousePos.current = { x: e.clientX, y: e.clientY };

    cameraAngle.current.theta -= dx * 0.008;
    cameraAngle.current.phi = Math.max(0.15, Math.min(Math.PI / 2.2, cameraAngle.current.phi + dy * 0.008));
  };

  const handlePointerUp = () => {
    isDraggingRef.current = false;
  };

  const handleWheel = (e: React.WheelEvent) => {
    cameraAngle.current.radius = Math.max(40, Math.min(450, cameraAngle.current.radius + e.deltaY * 0.15));
  };

  // Click on objects (Raycasting) or Click-to-Move simulation
  const handleClick = (e: React.MouseEvent) => {
    if (!containerRef.current || !cameraRef.current || !sceneRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const mouse = new THREE.Vector2(
      ((e.clientX - rect.left) / rect.width) * 2 - 1,
      -((e.clientY - rect.top) / rect.height) * 2 + 1
    );

    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(mouse, cameraRef.current);

    // Check interactables first
    if (interactablesGroupRef.current) {
      const intersects = raycaster.intersectObjects(interactablesGroupRef.current.children, true);
      if (intersects.length > 0) {
        let target: THREE.Object3D | null = intersects[0].object;
        while (target && !target.userData.type && target.parent) {
          target = target.parent;
        }

        if (target && target.userData.type === 'loot') {
          onLootClick(target.userData.data);
          setSelectedEntityInfo(`Coffre : ${target.userData.data.name}`);
          return;
        } else if (target && target.userData.type === 'chimere') {
          onChimereClick(target.userData.data);
          setSelectedEntityInfo(`Chimère : ${target.userData.data.name}`);
          return;
        }
      }
    }

    // Check ground plane for Click-To-Move simulation
    const groundIntersects = raycaster.intersectObjects(sceneRef.current.children);
    const groundHit = groundIntersects.find(hit => hit.object.type === 'Mesh' && (hit.point.y <= 0.5));
    if (groundHit) {
      const hitX = groundHit.point.x;
      const hitZ = groundHit.point.z;
      // Convert local meters back to Lat/Lon
      const newLat = centerLat - hitZ / 111320;
      const newLon = centerLon + hitX / (40075000 * Math.cos((centerLat * Math.PI) / 180) / 360);
      onPlayerMove(newLat, newLon);
    }
  };

  return (
    <div className="relative w-full h-full bg-slate-950 overflow-hidden select-none">
      {/* 3D Canvas Mount */}
      <div
        ref={containerRef}
        className="w-full h-full cursor-grab active:cursor-grabbing"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onWheel={handleWheel}
        onClick={handleClick}
      />

      {/* Retro Sci-Fi Scanlines overlay */}
      <div className="absolute inset-0 scanlines pointer-events-none" />

      {/* Top Floating Controls */}
      <div className="absolute top-4 left-4 right-4 flex items-center justify-between pointer-events-none z-10">
        <div className="flex items-center gap-2 bg-slate-900/90 backdrop-blur border border-cyan-500/40 rounded-xl px-3 py-1.5 shadow-lg pointer-events-auto">
          <Crosshair className="w-4 h-4 text-cyan-400 animate-spin" />
          <span className="text-xs font-mono text-cyan-300 font-semibold uppercase tracking-wider">
            Rendu Voxel OSM & H3
          </span>
          <span className="text-[10px] bg-cyan-950 text-cyan-400 px-1.5 py-0.5 rounded border border-cyan-800">
            {buildings.length} Bâtiments
          </span>
        </div>

        <div className="flex items-center gap-2 pointer-events-auto">
          <button
            onClick={() => setShowH3(!showH3)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium border flex items-center gap-1.5 transition-all shadow-md ${
              showH3
                ? 'bg-cyan-950/80 border-cyan-500 text-cyan-300'
                : 'bg-slate-900/80 border-slate-700 text-slate-400'
            }`}
            title="Basculer grille hexagonale Uber H3"
          >
            <ShieldAlert className="w-3.5 h-3.5" />
            Hex H3 {showH3 ? 'ON' : 'OFF'}
          </button>

          <button
            onClick={() => {
              cameraAngle.current = { theta: Math.PI / 4, phi: Math.PI / 3.5, radius: 180 };
            }}
            className="p-2 rounded-lg bg-slate-900/80 border border-slate-700 text-slate-300 hover:text-white hover:border-slate-500 shadow-md transition-all"
            title="Recentrer caméra sur le joueur"
          >
            <RotateCcw className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Bottom Hint */}
      <div className="absolute bottom-4 left-4 bg-slate-900/90 backdrop-blur-md border border-slate-800 rounded-lg px-3 py-1.5 text-[11px] text-slate-400 flex items-center gap-2 shadow-lg pointer-events-none">
        <Move className="w-3.5 h-3.5 text-cyan-400" />
        <span>Cliquez sur le sol pour déplacer le joueur (Simulateur GPS) · Glisser pour tourner · Molette pour zoomer</span>
      </div>
    </div>
  );
};
