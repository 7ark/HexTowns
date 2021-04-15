

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
        public Bounds bounds;
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
                Bounds bound = currDisplay.bounds;
                bool drawInGameView = true;
#if UNITY_EDITOR
                foreach (var svObj in UnityEditor.SceneView.sceneViews)
                {
                    UnityEditor.SceneView sv = svObj as UnityEditor.SceneView;
                    if (sv != null)
                    {
                        if (sv == UnityEditor.SceneView.currentDrawingSceneView)
                            drawInGameView = false;
                        Graphics.DrawMeshInstancedProcedural(meshBasis, 0, currDisplay.matInstance, bound, currDisplay.renderData.Length, null,
                            ShadowCastingMode.On, true, 6, sv.camera);
                    }
                }
#endif
                if(drawInGameView)
                {
                    Graphics.DrawMeshInstancedProcedural(meshBasis, 0, currDisplay.matInstance, bound, currDisplay.renderData.Length, null,
                        ShadowCastingMode.On, true, 6, drawCamera);
                }
            }

        }
    }

    public static PreviewRenderData DisplayArea(PreviewRenderData uniqueReference, List<HexTile> areaTiles, int height, bool useRedDisplay = false)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < areaTiles.Count; i++)
        {
            center += areaTiles[i].Position;
        }
        center /= areaTiles.Count;

        uniqueReference.dataBuffer = new ComputeBuffer(areaTiles.Count,
            UnsafeUtility.SizeOf<HexBufferData>());

        uniqueReference.renderData = new HexBufferData[areaTiles.Count];

        float left = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MaxValue;
        float top = float.MinValue;
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

                if(pos.x < left)
                {
                    left = pos.x;
                }
                if(pos.x > right)
                {
                    right = pos.x;
                }
                if(pos.z < bottom)
                {
                    bottom = pos.z;
                }
                if(pos.z > top)
                {
                    top = pos.z;
                }
            }
        }

        uniqueReference.bounds = new Bounds(center, new Vector3(right - left, 100, top - bottom));

        uniqueReference.dataBuffer.SetData(uniqueReference.renderData);
        uniqueReference.matInstance.SetBuffer(DataBuffer, uniqueReference.dataBuffer);

        return uniqueReference;
    }
}
