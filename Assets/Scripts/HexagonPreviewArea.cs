

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

public static class HexagonPreviewArea
{
    private static readonly int DataBuffer = Shader.PropertyToID("dataBuffer");
    private static Mesh meshBasis;
    private static Camera drawCamera;
    private static Material baseMaterial;

    public class PreviewRenderData
    {
        public HexBufferData[] renderData;
        public ComputeBuffer dataBuffer;
        public Material matInstance;
        public Vector3 center;
    }
    private static HashSet<PreviewRenderData> displays = new HashSet<PreviewRenderData>();

    public static void Initialize(Mesh meshBasisValue, Material baseMaterialValue, Camera drawCameraValue)
    {
        meshBasis = meshBasisValue;
        baseMaterial = baseMaterialValue;
        drawCamera = drawCameraValue;
    }

    public static PreviewRenderData CreateUniqueReference()
    {
        PreviewRenderData previewRenderData = new PreviewRenderData()
        {
            matInstance = new Material(baseMaterial)
        };
        displays.Add(previewRenderData);

        return previewRenderData;
    }

    public static void StopDisplay(PreviewRenderData uniqueReference)
    {
        GameObject.Destroy(uniqueReference.matInstance);
        displays.Remove(uniqueReference);
    }

    public static void Update()
    {
        foreach(var currDisplay in displays)
        {
            if(currDisplay.renderData != null && currDisplay.renderData.Length > 0)
            {
                //Draw that shit
                Bounds bound = new Bounds(currDisplay.center, new Vector3(500, 100, 500));
#if UNITY_EDITOR
                foreach (var svObj in UnityEditor.SceneView.sceneViews)
                {
                    UnityEditor.SceneView sv = svObj as UnityEditor.SceneView;
                    if (sv != null)
                    {
                        Graphics.DrawMeshInstancedProcedural(meshBasis, 0, currDisplay.matInstance, bound, currDisplay.renderData.Length, null,
                            ShadowCastingMode.On, true, 6, sv.camera);
                    }
                }
#endif

                Graphics.DrawMeshInstancedProcedural(meshBasis, 0, currDisplay.matInstance, bound, currDisplay.renderData.Length, null,
                    ShadowCastingMode.On, true, 6, drawCamera);
            }

        }
    }

    public static PreviewRenderData DisplayArea(PreviewRenderData uniqueReference, List<HexTile> areaTiles, int height, bool useRedDisplay = false)
    {
        uniqueReference.center = Vector3.zero;
        for (int i = 0; i < areaTiles.Count; i++)
        {
            uniqueReference.center += areaTiles[i].Position;
        }
        uniqueReference.center /= areaTiles.Count;

        uniqueReference.dataBuffer = new ComputeBuffer(areaTiles.Count,
            UnsafeUtility.SizeOf<HexBufferData>());

        uniqueReference.renderData = new HexBufferData[areaTiles.Count];

        for (int i = 0; i < uniqueReference.renderData.Length; i++)
        {
            HexTile cell = areaTiles[i];
            if (cell != null)
            {
                int lowestHeight = height;
                List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(cell);
                for (int j = 0; j < neighbors.Count; j++)
                {
                    if (neighbors[j].Height < lowestHeight && !areaTiles.Contains(neighbors[j]))
                    {
                        lowestHeight = neighbors[j].Height;
                    }
                }

                Vector3 pos = cell.Position;
                pos.y = (lowestHeight - 1) * HexTile.HEIGHT_STEP;
                HexBufferData data;
                data.pos_s = pos;
                int currHeight = Mathf.Max(1, Mathf.Max(height, cell.Height) - lowestHeight);
                data.pos_s.w = currHeight * HexTile.HEIGHT_STEP;
                data.index = useRedDisplay ? 1 : 0;
                data.hexCoordX = cell.Coordinates.X;
                data.hexCoordZ = cell.Coordinates.Y;
                uniqueReference.renderData[i] = data;
            }
        }

        uniqueReference.dataBuffer.SetData(uniqueReference.renderData);
        uniqueReference.matInstance.SetBuffer(DataBuffer, uniqueReference.dataBuffer);

        return uniqueReference;
    }
}
