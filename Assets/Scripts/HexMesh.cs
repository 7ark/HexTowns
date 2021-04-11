using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

public class HexMesh
{

    [System.Serializable]
    public struct HexBufferData
    {
        public Vector4 pos_s;
        public float index;
        public int hexCoordX;
        public int hexCoordZ;
    };

    private HexBufferData[] renderData;
    private ComputeBuffer dataBuffer;

    private static readonly int DataBuffer = Shader.PropertyToID("dataBuffer");
    private static readonly int OffsetX = Shader.PropertyToID("_OffsetX");
    private static readonly int OffsetZ = Shader.PropertyToID("_OffsetZ");
    private Mesh meshBasis;
    private Material matInstance;
    private Material matSelectionInstance;
    private HexTile[] allTiles;
    private Camera drawCamera;
    private Camera selectionCamera;
    private Vector3 center;

    public void SetupMeshGenerationData(Mesh mesh, List<HexTile> tiles, Material materialInst, Material materialSelectionInst, Camera drawCamera, Camera selectionCamera, HexagonTextureReference textureReference)
    {
        this.drawCamera = drawCamera;
        this.selectionCamera = selectionCamera;
        Vector3 pos = Vector3.zero;
        for (int i = 0; i < tiles.Count; i++)
        {
            pos += tiles[i].Position;
        }
        pos /= tiles.Count;

        center = pos;
        allTiles = tiles.ToArray();
        matInstance = new Material(materialInst);
        matSelectionInstance = new Material(materialSelectionInst);
        meshBasis = mesh;

        UpdateBuffer(tiles, textureReference);
    }
    private void UpdateBuffer(List<HexTile> tiles, HexagonTextureReference textureReference)
    {
        if (dataBuffer != null)
        {
            dataBuffer.Release();
        }

        dataBuffer = new ComputeBuffer(tiles.Count,
            UnsafeUtility.SizeOf<HexBufferData>());

        renderData = new HexBufferData[tiles.Count];
        for (var i = 0; i < renderData.Length; i++)
        {
            HexTile cell = tiles[i];
            if (cell != null)
            {
                int lowestHeight = int.MaxValue;
                List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(cell);
                for (int j = 0; j < neighbors.Count; j++)
                {
                    if(neighbors[j].Height < lowestHeight)
                    {
                        lowestHeight = neighbors[j].Height;
                    }
                }

                Vector3 pos = cell.Position;
                pos.y = (lowestHeight - 1) * HexTile.HEIGHT_STEP;
                HexBufferData data;
                data.pos_s = pos;
                int height = Mathf.Max(1, cell.Height - lowestHeight);
                data.pos_s.w = height * HexTile.HEIGHT_STEP;
                if(cell.MaterialIndex == -1)
                {
                    cell.MaterialIndex = textureReference.GetTextureIndex(cell.Height);
                }
                data.index = cell.MaterialIndex;
                data.hexCoordX = cell.Coordinates.X;
                data.hexCoordZ = cell.Coordinates.Y;
                renderData[i] = data;
            }
        }

        dataBuffer.SetData(renderData);
        // from `    StructuredBuffer<Data> dataBuffer;` in the hlsl file
        matInstance.SetBuffer(DataBuffer, dataBuffer);

        // chunk's offset in units of hexagons
        matSelectionInstance.SetFloat(OffsetX, 0);
        matSelectionInstance.SetFloat(OffsetZ, 0);
        matSelectionInstance.SetBuffer(DataBuffer, dataBuffer);
    }

    public void Update()
    {
        if(meshBasis != null)
        {
            Bounds bound = new Bounds(center, new Vector3(80, 100, 80));
#if UNITY_EDITOR
            foreach (var svObj in UnityEditor.SceneView.sceneViews)
            {
                UnityEditor.SceneView sv = svObj as UnityEditor.SceneView;
                if (sv != null)
                {
                    Graphics.DrawMeshInstancedProcedural(meshBasis, 0, matInstance, bound, allTiles.Length, null,
                        ShadowCastingMode.On, true, 6, sv.camera);
                }
            }
#endif

            Graphics.DrawMeshInstancedProcedural(meshBasis, 0, matInstance, bound, allTiles.Length, null,
                ShadowCastingMode.On, true, 6, drawCamera);

            Graphics.DrawMeshInstancedProcedural(meshBasis, 0, matSelectionInstance, bound, allTiles.Length, null,
                ShadowCastingMode.Off, false, 6, selectionCamera);
        }
    }

    public Material ReleaseData()
    {
        if(dataBuffer != null)
        {
            dataBuffer.Release();
        }
        return matInstance;
    }
}