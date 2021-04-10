using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class GenerateHexagonHandler : MonoBehaviour
{
    public delegate void TriangulateResult(Vector3[] Verts, int[] Tris, Vector2[] UVs);
    
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> currentUvs = new List<Vector2>();
    private List<int> triangles = new List<int>();
    public bool DoingJob { get; private set; }

    private List<JobHandle> meshGenerationJobHandles = new List<JobHandle>();
    private List<TriangulateTileJob> meshGenerationJobs = new List<TriangulateTileJob>();

    private TriangulateResult _onComplete;

    public void GenerateHexagon(TriangulateTileJob job, TriangulateResult onComplete)
    {
        GenerateHexagons(new List<TriangulateTileJob> { job }, onComplete);
    }

    public void GenerateHexagons(List<TriangulateTileJob> hexJobs, TriangulateResult onComplete)
    {
        if(DoingJob)
        {
            for (int i = 0; i < meshGenerationJobs.Count; i++)
            {
                meshGenerationJobHandles[i].Complete();
                meshGenerationJobs[i].vertices.Dispose();
                meshGenerationJobs[i].triangles.Dispose();
                meshGenerationJobs[i].neighborPositions.Dispose();
                meshGenerationJobs[i].neighborHeight.Dispose();
                meshGenerationJobs[i].uvs.Dispose();
                meshGenerationJobs[i].uvData.Dispose();
            }
            meshGenerationJobs.Clear();
            meshGenerationJobHandles.Clear();
        }
        this._onComplete = onComplete;

        this.enabled = true;
        DoingJob = true;
        for (int i = 0; i < hexJobs.Count; i++)
        {
            JobHandle jobHandler = hexJobs[i].Schedule();

            meshGenerationJobs.Add(hexJobs[i]);
            meshGenerationJobHandles.Add(jobHandler);
        }
    }

    private void LateUpdate()
    {
        if (DoingJob)
        {
            bool allComplete = true;
            for (int i = 0; i < meshGenerationJobHandles.Count; i++)
            {
                meshGenerationJobHandles[i].Complete();
                if (!meshGenerationJobHandles[i].IsCompleted)
                {
                    allComplete = false;
                }
            }

            if (allComplete)
            {
                vertices.Clear();
                triangles.Clear();
                currentUvs.Clear();
                for (int i = 0; i < meshGenerationJobs.Count; i++)
                {
                    // vertices.AddRange(meshGenerationJobs[i].vertices.ToArray());
                    for (int j = 0, jLen = meshGenerationJobs[i].vertices.Length; j < jLen; j++) {
                        vertices.Add(meshGenerationJobs[i].vertices[j]);
                    }

                    // triangles.AddRange(meshGenerationJobs[i].triangles.ToArray());
                    for (int j = 0, jLen = meshGenerationJobs[i].triangles.Length; j < jLen; j++) {
                        triangles.Add(meshGenerationJobs[i].triangles[j]);
                    }
                    
                    // currentUvs.AddRange(meshGenerationJobs[i].uvs.ToArray());
                    for (int j = 0, jLen = meshGenerationJobs[i].uvs.Length; j < jLen; j++) {
                        currentUvs.Add(meshGenerationJobs[i].uvs[j]);
                    }
                    
                    meshGenerationJobs[i].vertices.Dispose();
                    meshGenerationJobs[i].triangles.Dispose();
                    meshGenerationJobs[i].neighborPositions.Dispose();
                    meshGenerationJobs[i].neighborHeight.Dispose();
                    meshGenerationJobs[i].uvs.Dispose();
                    meshGenerationJobs[i].uvData.Dispose();
                }

                meshGenerationJobs.Clear();
                meshGenerationJobHandles.Clear();

                _onComplete?.Invoke(vertices.ToArray(), triangles.ToArray(), currentUvs.ToArray());

                DoingJob = false;
                this.enabled = false;
            }
        }
    }
}

[BurstCompile]
public struct TriangulateTileJob : IJob
{
    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector2> uvs;
    [ReadOnly]
    public Vector3 position;
    [ReadOnly]
    public int height;
    [ReadOnly]
    public int textureID;
    [ReadOnly]
    public float scale;

    [ReadOnly]
    public int neighborArrayCount;
    [ReadOnly]
    public NativeArray<int> neighborHeight;
    [ReadOnly]
    public NativeArray<Vector3> neighborPositions;
    [ReadOnly]
    public NativeArray<Rect> uvData;

    private NativeArray<Vector3> GetNeighborCorner(int i)
    {
        NativeArray<Vector3> corners = new NativeArray<Vector3>(6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        for (int j = 0; j < corners.Length; j++)
        {
            corners[j] = neighborPositions[i] + HexTile.CORNERS[j];
        }

        return corners;
    }

    public void Execute()
    {
        Rect rect = new Rect();
        if(uvData.Length > 0)
        {
            rect = uvData[textureID];
        }

        const float OUTER_RADIUS = 0.5f;
        const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;
        NativeArray<Vector2> CORNERS = new NativeArray<Vector2>(7, Allocator.Temp);
        CORNERS[0] = new Vector3(0f, OUTER_RADIUS);
        CORNERS[1] = new Vector3(INNER_RADIUS, 0.5f * OUTER_RADIUS);
        CORNERS[2] = new Vector3(INNER_RADIUS, -0.5f * OUTER_RADIUS);
        CORNERS[3] = new Vector3(0f, -OUTER_RADIUS);
        CORNERS[4] = new Vector3(-INNER_RADIUS, -0.5f * OUTER_RADIUS);
        CORNERS[5] = new Vector3(-INNER_RADIUS, 0.5f * OUTER_RADIUS);
        CORNERS[6] = new Vector3(0f, OUTER_RADIUS);

        Vector2 centerUV = rect.center;
        Vector2 size = rect.size;

        Vector3 center = new Vector3(position.x, height * HexTile.HEIGHT_STEP + (scale - 1), position.z);
        for (int i = 0; i < HexTile.CORNERS.Length - 1; i++)
        {
            AddTriangle(
                center,
                center + HexTile.CORNERS[i],
                center + HexTile.CORNERS[i + 1]);

            if (uvData.Length > 0)
            {
                Vector2 uv = CORNERS[i] * size + centerUV;
                Vector2 uv2 = CORNERS[i + 1] * size + centerUV;
                uvs.Add(centerUV);
                uvs.Add(uv);
                uvs.Add(uv2);
            }
        }

        for (int i = 0; i < neighborArrayCount; i++)
        {
            if (height != neighborHeight[i] && height > neighborHeight[i])
            {
                float neighborYPos = neighborHeight[i] * HexTile.HEIGHT_STEP;

                NativeArray<Vector3> tileCorners = new NativeArray<Vector3>(6, Allocator.Temp);
                for (int j = 0; j < tileCorners.Length; j++)
                {
                    tileCorners[j] = center + HexTile.CORNERS[j];
                }
                //var neighborCornerInstance = GetNeighborCorner(i);
                NativeArray<Vector3> neighborCorners = GetNeighborCorner(i);
                //for (int j = 0; j < neighborCorners.Length; j++)
                //{
                //    neighborCorners[j] = neighborCornerInstance[j];
                //}
                NativeArray<Vector3> sharedCorners = GetSharedCorners(tileCorners, neighborCorners);

                if (sharedCorners.Length > 0)
                {
                    Vector3 cornerOne = sharedCorners[0];
                    Vector3 cornerTwo = sharedCorners[1];

                    Vector3 cross = Vector3.Cross(
                        new Vector3(cornerTwo.x, center.y, cornerTwo.z) - new Vector3(cornerOne.x, center.y, cornerOne.z),
                        new Vector3(cornerTwo.x, neighborYPos, cornerTwo.z) - new Vector3(cornerTwo.x, center.y, cornerTwo.z));

                    float dot = Vector3.Dot(cross, Vector3.Lerp(cornerOne, cornerTwo, 0.5f) - position);

                    if (dot > 0)
                    {
                        Vector3 temp = cornerOne;
                        cornerOne = cornerTwo;
                        cornerTwo = temp;
                    }


                    AddTriangle(
                        new Vector3(cornerOne.x, height * HexTile.HEIGHT_STEP, cornerOne.z),
                        new Vector3(cornerOne.x, neighborYPos, cornerOne.z),
                        new Vector3(cornerTwo.x, neighborYPos, cornerTwo.z));
                    AddTriangle(
                        new Vector3(cornerTwo.x, neighborYPos, cornerTwo.z),
                        new Vector3(cornerTwo.x, height * HexTile.HEIGHT_STEP, cornerTwo.z),
                        new Vector3(cornerOne.x, height * HexTile.HEIGHT_STEP, cornerOne.z));

                    if (uvData.Length > 0)
                    {
                        Vector2 lowerLeftUV = rect.min;
                        Vector2 upperLeftUV = new Vector2(rect.xMin, rect.yMax);
                        Vector2 upperRightUV = rect.max;
                        Vector2 lowerRightUV = new Vector2(rect.xMax, rect.yMin);

                        uvs.Add(upperLeftUV);
                        uvs.Add(lowerLeftUV);
                        uvs.Add(lowerRightUV);

                        uvs.Add(lowerRightUV);
                        uvs.Add(upperRightUV);
                        uvs.Add(upperLeftUV);
                    }

                }
            }
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 diff = (vertices[i] - center).normalized * (scale - 1);
            vertices[i] += diff;
        }
    }

    private void AddTriangle(Vector3 vert1, Vector3 vert2, Vector3 vert3)
    {
        int vertexIndex = vertices.Length;
        vertices.Add(vert1);
        vertices.Add(vert2);
        vertices.Add(vert3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }


    private NativeArray<Vector3> GetSharedCorners(NativeArray<Vector3> oneCorners, NativeArray<Vector3> twoCorners)
    {
        const float threshold = 0.01f;
        //List<Vector3[]> cornerData = new List<Vector3[]>() { new Vector3[oneCorners.Length], new Vector3[twoCorners.Length] };
        NativeArray<Vector3> shared = new NativeArray<Vector3>(2, Allocator.Temp);
        int sharedIndex = 0;
        for (int i = 0; i < oneCorners.Length; i++)
        {
            for (int j = 0; j < twoCorners.Length; j++)
            {
                Vector3 onePos = oneCorners[i];
                Vector3 twoPos = twoCorners[j];
                onePos.y = 0;
                twoPos.y = 0;

                //cornerData[0][i] = onePos;
                //cornerData[1][j] = twoPos;

                if (math.abs(onePos.x - twoPos.x) < threshold && math.abs(onePos.z - twoPos.z) < threshold && !shared.Contains(oneCorners[i]))
                {
                    shared[sharedIndex++] = (oneCorners[i]);
                }
            }
        }

        return shared;
    }
}