using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

public class HexMesh
{

    [System.Serializable]
    public struct HexBufferData
    {
        public Vector4 pos_s;
        public float index;
    };

    private HexBufferData[] renderData;
    private ComputeBuffer dataBuffer;

    private static readonly int DataBuffer = Shader.PropertyToID("dataBuffer");
    private Mesh meshBasis;
    [SerializeField]
    private Material matInstance;
    private HexTile[] allTiles;
    public Vector3 center;

    public void SetupMeshGenerationData(Mesh mesh, List<HexTile> tiles, Material materialInst, HexagonTextureReference textureReference)
    {
        Vector3 pos = Vector3.zero;
        for (int i = 0; i < tiles.Count; i++)
        {
            pos += tiles[i].Position;
        }
        pos /= tiles.Count;

        center = pos;
        allTiles = tiles.ToArray();
        matInstance = new Material(materialInst);
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
                data.index = textureReference.GetTextureIndex(cell.Height);
                renderData[i] = data;
            }
        }

        dataBuffer.SetData(renderData);
        // from `    StructuredBuffer<Data> dataBuffer;` in the hlsl file
        matInstance.SetBuffer(DataBuffer, dataBuffer);
    }

    public void Update()
    {
        if(meshBasis != null)
        {
            Graphics.DrawMeshInstancedProcedural(meshBasis, 0, matInstance, new Bounds(center, new Vector3(80, 100, 80)), allTiles.Length);
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