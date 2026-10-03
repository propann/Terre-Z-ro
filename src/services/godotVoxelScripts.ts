export const GODOT4_VOXEL_CHUNK_CS = `// VoxelChunk.cs - Micro-Voxel 16-Bit Chunk Container (Godot 4 .NET / C#)
// Spécification V1.1 : Résolution 20cm (1 voxel), Chunk 32x32x32 = 6.4m, 16 bits par voxel

using System;
using System.Runtime.CompilerServices;
using Godot;

public enum VoxelMaterial : byte
{
    Air = 0,
    Concrete = 1,        // Béton armé
    Brick = 2,           // Brique rouge
    Asphalt = 3,         // Asphalte craquelé
    Sidewalk = 4,        // Bordure de trottoir
    ReinforcedGlass = 5, // Verre blindé
    CopperWiring = 6,    // Câblage cuivre interne (Loot)
    MedCache = 7,        // Réserve médicale cachée
    SteelBarricade = 8,  // Blindage joueur
    TurretBase = 9,      // Base tourelle
    GrassOrganic = 10    // Mousse mutée
}

public struct Voxel16Bit
{
    public ushort Raw; // [0..7]: Material (8 bits), [8..11]: Durability (4 bits), [12..15]: Light/State (4 bits)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Voxel16Bit(VoxelMaterial mat, byte durability = 15, byte light = 0)
    {
        Raw = (ushort)(((byte)mat & 0xFF) | ((durability & 0x0F) << 8) | ((light & 0x0F) << 12));
    }

    public VoxelMaterial Material => (VoxelMaterial)(Raw & 0xFF);
    public byte Durability => (byte)((Raw >> 8) & 0x0F);
    public byte Light => (byte)((Raw >> 12) & 0x0F);
    public bool IsAir => (Raw & 0xFF) == 0;
}

public partial class VoxelChunk : Node3D
{
    public const int Size = 32; // 32x32x32 voxels
    public const float VoxelScale = 0.2f; // 20 cm par voxel (Taille du chunk = 6.4 m)

    private readonly ushort[] _voxels = new ushort[Size * Size * Size];
    public bool IsDirty { get; set; } = true;
    public MeshInstance3D MeshInstance { get; private set; }
    public StaticBody3D CollisionBody { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(int x, int y, int z) => x + y * Size + z * Size * Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VoxelMaterial GetVoxel(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size) return VoxelMaterial.Air;
        return (VoxelMaterial)(_voxels[GetIndex(x, y, z)] & 0xFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVoxel(int x, int y, int z, VoxelMaterial mat, byte durability = 15)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size) return;
        _voxels[GetIndex(x, y, z)] = (ushort)(((byte)mat & 0xFF) | ((durability & 0x0F) << 8));
        IsDirty = true;
    }

    // Minage chirurgical / Forage de trou au laser (Sprint 3.1)
    public int CarveSphere(Vector3 localPos, float radiusMeters)
    {
        int cx = Mathf.RoundToInt(localPos.X / VoxelScale);
        int cy = Mathf.RoundToInt(localPos.Y / VoxelScale);
        int cz = Mathf.RoundToInt(localPos.Z / VoxelScale);
        int r = Mathf.CeilToInt(radiusMeters / VoxelScale);
        int rSq = r * r;
        int destroyed = 0;

        for (int x = Math.Max(0, cx - r); x <= Math.Min(Size - 1, cx + r); x++)
        for (int y = Math.Max(0, cy - r); y <= Math.Min(Size - 1, cy + r); y++)
        for (int z = Math.Max(0, cz - r); z <= Math.Min(Size - 1, cz + r); z++)
        {
            int dSq = (x - cx) * (x - cx) + (y - cy) * (y - cy) + (z - cz) * (z - cz);
            if (dSq <= rSq)
            {
                if (GetVoxel(x, y, z) != VoxelMaterial.Air)
                {
                    SetVoxel(x, y, z, VoxelMaterial.Air);
                    destroyed++;
                }
            }
        }

        if (destroyed > 0)
        {
            RebuildMeshGreedy();
        }

        return destroyed;
    }

    public void RebuildMeshGreedy()
    {
        // Appelle le GreedyMesher C# optimisé (voir GreedyMesher.cs)
        GreedyMesher.GenerateMesh(this);
        IsDirty = false;
    }
}
`;

export const GODOT4_GREEDY_MESHER_CS = `// GreedyMesher.cs - Algorithme de Greedy Meshing pour Godot 4
// Fusionne les faces coplanaires adjacentes et supprime les faces internes cachées.
// Réduction drastique du nombre de polygones (>95% de triangles en moins).

using System;
using System.Collections.Generic;
using Godot;

public static class GreedyMesher
{
    private const int Size = VoxelChunk.Size;
    private const float Scale = VoxelChunk.VoxelScale;

    public static void GenerateMesh(VoxelChunk chunk)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        // Sweep sur les 3 axes dimensionnels (X = 0, Y = 1, Z = 2)
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;

            int[] x = new int[3];
            int[] q = new int[3];
            q[d] = 1;

            int[] mask = new int[Size * Size];

            for (x[d] = -1; x[d] < Size; )
            {
                int n = 0;
                for (x[v] = 0; x[v] < Size; x[v]++)
                for (x[u] = 0; x[u] < Size; x[u]++)
                {
                    var a = x[d] >= 0 ? chunk.GetVoxel(x[0], x[1], x[2]) : VoxelMaterial.Air;
                    var b = x[d] < Size - 1 ? chunk.GetVoxel(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : VoxelMaterial.Air;

                    if (a != VoxelMaterial.Air && b == VoxelMaterial.Air)
                        mask[n++] = (int)a; // Face positive
                    else if (a == VoxelMaterial.Air && b != VoxelMaterial.Air)
                        mask[n++] = -(int)b; // Face négative
                    else
                        mask[n++] = 0; // Face interne masquée (Culled !)
                }

                x[d]++;
                n = 0;

                // Fusion gloutonne (Greedy merge)
                for (int j = 0; j < Size; j++)
                for (int i = 0; i < Size; )
                {
                    int val = mask[n];
                    if (val != 0)
                    {
                        int width = 1;
                        while (i + width < Size && mask[n + width] == val) width++;

                        int height = 1;
                        bool canExtend = true;
                        while (j + height < Size && canExtend)
                        {
                            for (int k = 0; k < width; k++)
                            {
                                if (mask[n + k + height * Size] != val)
                                {
                                    canExtend = false;
                                    break;
                                }
                            }
                            if (canExtend) height++;
                        }

                        // Créer le quad fusionné
                        int[] pos = new int[3];
                        pos[d] = x[d];
                        pos[u] = i;
                        pos[v] = j;

                        int[] du = new int[3]; du[u] = width;
                        int[] dv = new int[3]; dv[v] = height;

                        AddQuad(surfaceTool, pos, du, dv, val > 0, (VoxelMaterial)Math.Abs(val), d);

                        // Vider la région du masque
                        for (int l = 0; l < height; l++)
                        for (int k = 0; k < width; k++)
                            mask[n + k + l * Size] = 0;

                        i += width;
                        n += width;
                    }
                    else
                    {
                        i++;
                        n++;
                    }
                }
            }
        }

        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();
        var arrayMesh = surfaceTool.Commit();

        if (chunk.MeshInstance == null)
        {
            var mi = new MeshInstance3D();
            chunk.AddChild(mi);
        }
        chunk.MeshInstance.Mesh = arrayMesh;
    }

    private static void AddQuad(SurfaceTool st, int[] pos, int[] du, int[] dv, bool forward, VoxelMaterial mat, int axis)
    {
        Vector3 v0 = new Vector3(pos[0], pos[1], pos[2]) * Scale;
        Vector3 v1 = new Vector3(pos[0] + du[0], pos[1] + du[1], pos[2] + du[2]) * Scale;
        Vector3 v2 = new Vector3(pos[0] + du[0] + dv[0], pos[1] + du[1] + dv[1], pos[2] + du[2] + dv[2]) * Scale;
        Vector3 v3 = new Vector3(pos[0] + dv[0], pos[1] + dv[1], pos[2] + dv[2]) * Scale;

        Color color = GetMaterialColor(mat);
        st.SetColor(color);

        if (forward)
        {
            st.AddVertex(v0); st.AddVertex(v1); st.AddVertex(v2);
            st.AddVertex(v0); st.AddVertex(v2); st.AddVertex(v3);
        }
        else
        {
            st.AddVertex(v0); st.AddVertex(v2); st.AddVertex(v1);
            st.AddVertex(v0); st.AddVertex(v3); st.AddVertex(v2);
        }
    }

    public static Color GetMaterialColor(VoxelMaterial mat) => mat switch
    {
        VoxelMaterial.Concrete => new Color(0.28f, 0.33f, 0.41f),
        VoxelMaterial.Brick => new Color(0.70f, 0.32f, 0.04f),
        VoxelMaterial.Asphalt => new Color(0.12f, 0.16f, 0.23f),
        VoxelMaterial.Sidewalk => new Color(0.39f, 0.45f, 0.55f),
        VoxelMaterial.ReinforcedGlass => new Color(0.22f, 0.74f, 0.97f, 0.8f),
        VoxelMaterial.CopperWiring => new Color(0.97f, 0.45f, 0.09f),
        VoxelMaterial.MedCache => new Color(0.06f, 0.72f, 0.51f),
        VoxelMaterial.SteelBarricade => new Color(0.01f, 0.52f, 0.78f),
        _ => Colors.White
    };
}
`;

export const GODOT4_OSM_VOXELIZER_CS = `// OSMVoxelizer.cs - Voxelisation Procédurale 2.5D des polygones OSM (Godot 4)
// Spécification V1.2 : Rasterisation des footprints + hauteur, dénivelé trottoir 2 voxels, caches internes

using System;
using System.Collections.Generic;
using Godot;

public static class OSMVoxelizer
{
    public static void VoxelizeBuilding(VoxelChunk chunk, Rect2 buildingFootprint, float heightMeters, string amenityTag)
    {
        int minX = Mathf.Clamp(Mathf.RoundToInt(buildingFootprint.Position.X / VoxelChunk.VoxelScale), 0, VoxelChunk.Size - 1);
        int maxX = Mathf.Clamp(Mathf.RoundToInt(buildingFootprint.End.X / VoxelChunk.VoxelScale), 0, VoxelChunk.Size - 1);
        int minZ = Mathf.Clamp(Mathf.RoundToInt(buildingFootprint.Position.Y / VoxelChunk.VoxelScale), 0, VoxelChunk.Size - 1);
        int maxZ = Mathf.Clamp(Mathf.RoundToInt(buildingFootprint.End.Y / VoxelChunk.VoxelScale), 0, VoxelChunk.Size - 1);
        int topY = Mathf.Clamp(Mathf.RoundToInt(heightMeters / VoxelChunk.VoxelScale), 3, VoxelChunk.Size - 1);

        VoxelMaterial wallMat = amenityTag == "pharmacy" ? VoxelMaterial.Brick : VoxelMaterial.Concrete;

        for (int y = 3; y <= topY; y++)
        {
            bool isFloor = (y == 3 || y == 15 || y == topY);
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                bool isPerimeter = (x == minX || x == maxX || z == minZ || z == maxZ);
                if (isFloor)
                {
                    chunk.SetVoxel(x, y, z, VoxelMaterial.Concrete);
                }
                else if (isPerimeter)
                {
                    bool isWindow = (y >= 8 && y <= 12) && (x % 3 == 0);
                    chunk.SetVoxel(x, y, z, isWindow ? VoxelMaterial.ReinforcedGlass : wallMat);
                }
            }
        }

        // Dissimuler du câblage en cuivre à l'intérieur des parois (Sprint 3.2)
        for (int y = 4; y < topY - 2; y++)
        {
            chunk.SetVoxel(minX + 1, y, minZ + 3, VoxelMaterial.CopperWiring);
        }

        // Si Pharmacie, dissimuler une trousse de secours derrière la cloison arrière
        if (amenityTag == "pharmacy")
        {
            chunk.SetVoxel((minX + maxX) / 2, 5, maxZ - 1, VoxelMaterial.MedCache);
        }
    }
}
`;
