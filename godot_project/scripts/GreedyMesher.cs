using System;
using Godot;

namespace Chimeres.Voxel
{
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

            // Recalcul Collision Trimesh
            if (chunk.CollisionShape != null)
            {
                chunk.CollisionShape.Shape = arrayMesh.CreateTrimeshShape();
            }
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
            VoxelMaterial.TurretBase => new Color(0.85f, 0.47f, 0.02f),
            VoxelMaterial.GrassOrganic => new Color(0.02f, 0.47f, 0.34f),
            _ => Colors.White
        };
    }
}
