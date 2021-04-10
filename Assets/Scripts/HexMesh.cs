using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh hexMesh;

    private MeshCollider hexCollider;

    private List<Vector3> corners = new List<Vector3>();
    private MeshRenderer meshRenderer;
    private HexBoard parentBoard;
    private GenerateHexagonHandler generateHexagonHandler;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        hexMesh = GetComponent<MeshFilter>().mesh = new Mesh();
        hexCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
        parentBoard = GetComponentInParent<HexBoard>();
        generateHexagonHandler = gameObject.AddComponent<GenerateHexagonHandler>();
    }


    public void SetMaterials(params Material[] mat)
    {
        meshRenderer.sharedMaterials = mat;
    }

    public void SetMaterial(Material mat, int index)
    {
        meshRenderer.sharedMaterials[index] = mat;
    }

    public void Triangulate(List<HexTile> tiles)
    {
        hexMesh.Clear();
        corners.Clear();

        List<TriangulateTileJob> jobs = new List<TriangulateTileJob>();
        foreach (var tile in tiles) {
            List<HexTile> tileNeighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(tile);
            NativeArray<int> neighborHeights = new NativeArray<int>(tileNeighbors.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector3> neighborPositions = new NativeArray<Vector3>(tileNeighbors.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < neighborHeights.Length; j++)
            {
                neighborHeights[j] = tileNeighbors[j].Height;
                neighborPositions[j] = tileNeighbors[j].Position;
            }

            if(tile.MaterialIndex == -1)
            {
                tile.MaterialIndex = HexBoardChunkHandler.Instance.TextureRef.GetTextureIndex(tile.Height);
            }

            TriangulateTileJob job = new TriangulateTileJob()
            {
                position = new Vector3(tile.Position.x, tile.Height * HexTile.HEIGHT_STEP, tile.Position.z),
                vertices = new NativeList<Vector3>(Allocator.TempJob),
                triangles = new NativeList<int>(Allocator.TempJob),
                textureID = tile.MaterialIndex,
                uvs = new NativeList<Vector2>(Allocator.TempJob),
                uvData = new NativeArray<Rect>(HexagonTextureReference.ATLAS_UVs, Allocator.TempJob),
                height = tile.Height,
                scale = 1,
                neighborArrayCount = tileNeighbors.Count,
                neighborHeight = neighborHeights,
                neighborPositions = neighborPositions
            };
            jobs.Add(job);
        }

        generateHexagonHandler.GenerateHexagons(jobs, SetupMesh);
    }


    private void SetupMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        hexMesh.vertices = vertices;
        int[] tris = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            tris[i] = i;
        }
        hexMesh.triangles = tris;
        hexMesh.SetUVs(0, uvs);
        hexMesh.RecalculateNormals();
        hexCollider.sharedMesh = hexMesh;

        //meshRenderer.sharedMaterial.SetTexture("_BaseColorMap", HexBoardChunkHandler.TEXTURE_ATLAS);
    }
}