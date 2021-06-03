using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

public class InstancedGenericObject
{
    [System.Serializable]
    public struct GenericObjBufferData
    {
        public Matrix4x4 world2Obj;
        public Matrix4x4 obj2world;
    };

    private GenericObjBufferData[] renderData = null;
    private ComputeBuffer dataBuffer = null;

    private static readonly int DataBuffer = Shader.PropertyToID("dataBuffer");

    private Mesh meshBasis;
    private List<Material> matInstances = new List<Material>();
    private Dictionary<Guid, Matrix4x4> pointInformation = new Dictionary<Guid, Matrix4x4>();
    private Vector3 center = new Vector3(0, 0, 0);
    private Vector3 boundsSize = new Vector3(80, 100, 80);
    private int submeshCount = 0;

    public InstancedGenericObject(GameObject originalObject, bool biggerBounds = false)
    {
        meshBasis = originalObject.GetComponent<MeshFilter>().sharedMesh;
        submeshCount = meshBasis.subMeshCount;

        if(biggerBounds)
        {
            boundsSize = new Vector3(100000, 100000, 100000);
        }

        if (matInstances.Count <= 0)
        {
            MeshRenderer meshRender = originalObject.GetComponent<MeshRenderer>();
            for (int i = 0; i < meshRender.sharedMaterials.Length; i++)
            {
                matInstances.Add(new Material(meshRender.sharedMaterials[i]));
            }
        }
    }

    public void RemoveDataPoint(Guid pos)
    {
        pointInformation.Remove(pos);
        UpdateBuffer(pointInformation);
    }

    public Guid AddDataPoint(Matrix4x4 pos, bool updateBuffer = true)
    {
        Guid uniqueId = Guid.NewGuid();
        pointInformation.Add(uniqueId, pos);
        if (updateBuffer)
        {
            UpdateBuffer(pointInformation);
        }

        FindCenter();

        return uniqueId;
    }

    public void UpdateDataPoint(Guid unqiueInstance, Matrix4x4 pos)
    {
        pointInformation[unqiueInstance] = pos;
        UpdateBuffer(pointInformation);
    }

    public Matrix4x4 GetDataPoint(Guid uniqueInstance)
    {
        return pointInformation[uniqueInstance];
    }

    public void FindCenter()
    {
        center = Vector3.zero;
        if(pointInformation.Count > 0)
        {
            foreach (var key in pointInformation.Keys)
            {
                center += (Vector3)pointInformation[key].GetColumn(3);
            }
            center /= pointInformation.Count;
        }
    }

    private void UpdateBuffer(Dictionary<Guid, Matrix4x4> points)
    {
        if (dataBuffer != null)
        {
            dataBuffer.Release();
        }

        if (points.Count <= 0)
        {
            return;
        }

        dataBuffer = new ComputeBuffer(points.Count,
            UnsafeUtility.SizeOf<GenericObjBufferData>());

        if (renderData == null || renderData.Length != points.Count)
        {
            renderData = new GenericObjBufferData[points.Count];
        }

        int renderIndex = 0;
        foreach (var guid in points.Keys)
        {
            GenericObjBufferData data;
            data.world2Obj = points[guid].inverse;
            data.obj2world = points[guid];
            renderData[renderIndex] = data;

            renderIndex++;
        }

        dataBuffer.SetData(renderData);
        // from `    StructuredBuffer<Data> dataBuffer;` in the hlsl file
        for (int i = 0; i < matInstances.Count; i++)
        {
            matInstances[i].SetBuffer(DataBuffer, dataBuffer);
        }
    }
    public void Update()
    {
        if (meshBasis != null && pointInformation != null && pointInformation.Count > 0)
        {
            for (int i = 0; i < submeshCount; i++)
            {
                Bounds bound = new Bounds(center, boundsSize);
                Graphics.DrawMeshInstancedProcedural(meshBasis, i, matInstances[i], bound, pointInformation.Count, null,
                    ShadowCastingMode.On, true, 0);
            }

        }
    }
}