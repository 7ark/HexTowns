using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainModificationHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject terrainModPreviewPrefab;

    private GenerateHexagonHandler generateHexagonHandler;
    private Dictionary<GameObject, HexTile[]> objToAssociatedTiles = new Dictionary<GameObject, HexTile[]>();
    private Dictionary<GameObject, int> objToAssociatedHeight = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, System.Action> objToAssociatedCompleteAction = new Dictionary<GameObject, System.Action>();
    private List<GameObject> allWorkableObjects = new List<GameObject>();
    private Dictionary<Workable, HexTile[]> workablesWaitingToStart = new Dictionary<Workable, HexTile[]>();
    private float workablesWaitingTimer = 0;

    public static TerrainModificationHandler Instance;

    private void Awake()
    {
        Instance = this;
        generateHexagonHandler = gameObject.AddComponent<GenerateHexagonHandler>();
    }

    public void RequestTerrainModification(HexTile[] areaTiles, int height, GameObject existingModification = null, System.Action onComplete = null)
    {
        List<HexTile> areaTilesList = new List<HexTile>(areaTiles);

        for (int i = 0; i < allWorkableObjects.Count; i++)
        {
            if(allWorkableObjects[i] != existingModification)
            {
                HexTile[] otherTiles = objToAssociatedTiles[allWorkableObjects[i]];
                for (int j = 0; j < otherTiles.Length; j++)
                {
                    if (areaTilesList.Contains(otherTiles[j]))
                    {
                        areaTilesList.Remove(otherTiles[j]);
                    }
                }
            }
        }

        for (int i = areaTilesList.Count - 1; i >= 0; i--)
        {
            if(areaTilesList[i].HeightLocked)
            {
                areaTilesList.RemoveAt(i);
            }
        }

        GameObject newObj = existingModification == null ? Instantiate(terrainModPreviewPrefab) : existingModification;
        if(existingModification == null)
        {
            allWorkableObjects.Add(newObj);
            objToAssociatedTiles.Add(newObj, areaTilesList.ToArray());
            objToAssociatedHeight.Add(newObj, height);
            objToAssociatedCompleteAction.Add(newObj, onComplete);
        }

        HexagonPreviewArea.AddAreaToDisplay(areaTilesList.ToArray(), height, newObj, (GameObject modifiedObject, bool success) =>
        {
            if (!success)
            {
                onComplete?.Invoke();
                objToAssociatedHeight.Remove(modifiedObject);
                objToAssociatedTiles.Remove(modifiedObject);
                allWorkableObjects.Remove(modifiedObject);
                objToAssociatedCompleteAction.Remove(modifiedObject);
                return;
            }
            if (existingModification == null)
            {
                Workable workable = newObj.AddComponent<Workable>();
                workable.TilesAssociated = new List<HexTile>(areaTilesList);
                workable.OnWorkTick += () => { return DoWorkOnTerrain(newObj); };
                workable.OnBuilt += () =>
                {
                    for (int i = 0; i < areaTilesList.Count; i++)
                    {
                        areaTilesList[i].WorkArea = false;
                    }
                };
                workable.SetTotalWorkableSlots(Mathf.Max(1, areaTilesList.Count / 5));
                workablesWaitingToStart.Add(workable, areaTilesList.ToArray());
                for (int i = 0; i < areaTilesList.Count; i++)
                {
                    areaTilesList[i].AddWorkableToTile(workable, height);
                    Workable[] tileWorkables = areaTilesList[i].GetEnvironmentalItemsAsWorkable();
                    for (int j = 0; j < tileWorkables.Length; j++)
                    {
                        tileWorkables[j].BeginWorking();
                    }
                }
                //PeepleJobHandler.Instance.AddWorkable(workable);
            }

            newObj.transform.position = Vector3.zero;
        });

    }

    private void Update()
    {
        workablesWaitingTimer -= Time.deltaTime;    

        if(workablesWaitingToStart.Count > 0 && workablesWaitingTimer <= 0)
        {
            workablesWaitingTimer = 2;

            List<Workable> toRemove = new List<Workable>();
            foreach (var workable in workablesWaitingToStart.Keys)
            {
                HexTile[] tilesWaitingOn = workablesWaitingToStart[workable];
                bool goodToStart = true;
                for (int i = 0; i < tilesWaitingOn.Length; i++)
                {
                    if (tilesWaitingOn[i].HasEnvironmentalItems)
                    {
                        goodToStart = false;
                        break;
                    }
                }

                if (goodToStart)
                {
                    List<HexTile> tilesWaitingOnList = new List<HexTile>(tilesWaitingOn);
                    //Peeple[] peeps = PeepleHandler.Instance.GetPeepleOnTiles(tilesWaitingOn);
                    //if(peeps.Length > 0)
                    //{
                    //    int directionX = -1;
                    //    int peepIndex = 0;
                    //    Peeple farthest = null;
                    //    float farthestDist = 0;
                    //    for (int i = 0; i < peeps.Length; i++)
                    //    {
                    //        float dist = Vector3.Distance(peeps[i].transform.position, tilesWaitingOn[0].Position + new Vector3(0, tilesWaitingOn[0].Height * HexTile.HEIGHT_STEP));
                    //        if(dist > farthestDist)
                    //        {
                    //            farthest = peeps[i];
                    //            farthestDist = dist;
                    //        }
                    //    }
                    //    for (int i = 0; i < tilesWaitingOn.Length; i++)
                    //    {
                    //        tilesWaitingOn[i].WorkArea = true;
                    //    }
                    //    while (peepIndex < peeps.Length)
                    //    {
                    //        HexTile hex = HexBoardChunkHandler.Instance.GetTileInDirection(tilesWaitingOn[0], directionX, 0);
                    //        directionX--;
                    //        if (!hex.CantWalkThrough && !tilesWaitingOnList.Contains(hex))
                    //        {
                    //            if (peeps[peepIndex] == farthest)
                    //            {
                    //                peeps[peepIndex].Movement.SetGoal(hex, alwaysDoActionEvent: true, arrivedComplete: (valid) =>
                    //                {
                    //                    for (int i = 0; i < tilesWaitingOn.Length; i++)
                    //                    {
                    //                        tilesWaitingOn[i].HeightLocked = true;
                    //                        tilesWaitingOn[i].WorkArea = false;
                    //                    }
                    //                    workable.BeginWorking();
                    //                });
                    //            }
                    //            else
                    //            {
                    //                peeps[peepIndex].Movement.SetGoal(hex);
                    //            }
                    //            peepIndex++;
                    //        }
                    //    }
                    //}
                    //else
                    {
                        for (int i = 0; i < tilesWaitingOn.Length; i++)
                        {
                            tilesWaitingOn[i].WorkArea = true;
                        }
                        workable.BeginWorking();
                    }
                    toRemove.Add(workable);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                workablesWaitingToStart.Remove(toRemove[i]);
            }
            toRemove.Clear();
        }
    }

    private bool DoWorkOnTerrain(GameObject obj)
    {
        if(!allWorkableObjects.Contains(obj))
        {
            return false;
        }
        HexTile[] areaTiles = objToAssociatedTiles[obj];

        for (int i = 0; i < areaTiles.Length; i++)
        {
            areaTiles[i].HeightLocked = false;
        }
        if(!HexBoardChunkHandler.Instance.FlattenArea(areaTiles, objToAssociatedHeight[obj], true))
        {
            RequestTerrainModification(areaTiles, objToAssociatedHeight[obj], obj);
            for (int i = 0; i < areaTiles.Length; i++)
            {
                areaTiles[i].HeightLocked = true;
            }

            return false;
        }
        else
        {
            objToAssociatedCompleteAction[obj]?.Invoke();

            objToAssociatedHeight.Remove(obj);
            objToAssociatedTiles.Remove(obj);
            allWorkableObjects.Remove(obj);
            objToAssociatedCompleteAction.Remove(obj);
            Destroy(obj);

            return true;
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < allWorkableObjects.Count; i++)
        {
            HexagonPreviewArea.DisplayArea(allWorkableObjects[i], generateHexagonHandler);
        }
    }

    //float timer = 0;
    //private void Update()
    //{
    //    timer += Time.deltaTime;
    //    if(timer > 1)
    //    {
    //        for (int i = 0; i < allWorkableObjects.Count; i++)
    //        {
    //            DoWorkOnTerrain(allWorkableObjects[i]);
    //        }
    //
    //        timer = 0;
    //    }
    //}
}

public static class HexagonPreviewArea
{
    private static HexBufferData[] renderData;
    private static ComputeBuffer dataBuffer;
    private static readonly int DataBuffer = Shader.PropertyToID("dataBuffer");
    private struct HexPreviewData
    {
        public HexTile[] areatiles;
        public int height;
        public System.Action<GameObject, bool> onComplete;
    }
    private static Dictionary<GameObject, HexPreviewData> displaysToDo = new Dictionary<GameObject, HexPreviewData>();

    public static void AddAreaToDisplay(HexTile[] areaTiles, int height, GameObject meshToModify, System.Action<GameObject, bool> onComplete)
    {
        HexPreviewData previewData = new HexPreviewData()
        {
            areatiles = areaTiles,
            height = height,
            onComplete = onComplete
        };
        if (!displaysToDo.ContainsKey(meshToModify))
        {
            displaysToDo.Add(meshToModify, previewData);
        }
        displaysToDo[meshToModify] = previewData;
    }

    public static void DisplayArea(GameObject associatedObject, GenerateHexagonHandler generateHexagonHandler)
    {
        if(!displaysToDo.ContainsKey(associatedObject))
        {
            return;
        }
        DisplayArea(displaysToDo[associatedObject].areatiles, displaysToDo[associatedObject].height, associatedObject, generateHexagonHandler, displaysToDo[associatedObject].onComplete);
        displaysToDo.Remove(associatedObject);
    }

    private static void DisplayArea(HexTile[] areaTiles, int height, GameObject meshToModify, GenerateHexagonHandler generateHexagonHandler, System.Action<GameObject, bool> onComplete)
    {
        //if(generateHexagonHandler.DoingJob)
        //{
        //    return;
        //}
        List<HexTile> areaTilesList = new List<HexTile>(areaTiles);
        List<TriangulateTileJob> jobs = new List<TriangulateTileJob>();
        for (int i = 0; i < areaTilesList.Count; i++)
        {
            if (areaTilesList[i].Height == height)
            {
                continue;
            }
            HexTile[] tileNeighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(areaTilesList[i]).ToArray();
            NativeArray<int> neighborHeights = new NativeArray<int>(tileNeighbors.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector3> neighborPositions = new NativeArray<Vector3>(tileNeighbors.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int j = 0; j < neighborHeights.Length; j++)
            {
                neighborHeights[j] = areaTilesList.Contains(tileNeighbors[j]) || tileNeighbors[j].Height > height ? height : (Mathf.Max(areaTilesList[i].Height, tileNeighbors[j].Height));
                neighborPositions[j] = tileNeighbors[j].Position;
            }

            TriangulateTileJob job = new TriangulateTileJob()
            {
                position = new Vector3(areaTilesList[i].Position.x, height * HexTile.HEIGHT_STEP, areaTilesList[i].Position.z),
                vertices = new NativeList<Vector3>(Allocator.TempJob),
                triangles = new NativeList<int>(Allocator.TempJob),
                textureID = areaTilesList[i].MaterialIndex,
                uvs = new NativeList<Vector2>(Allocator.TempJob),
                uvData = new NativeArray<Rect>(0, Allocator.TempJob),
                height = height > areaTilesList[i].Height ? height : areaTilesList[i].Height,
                scale = height > areaTilesList[i].Height ? 1 : 1.05f,
                neighborArrayCount = tileNeighbors.Length,
                neighborHeight = neighborHeights,
                neighborPositions = neighborPositions
            };
            jobs.Add(job);
        }

        if (jobs.Count == 0)
        {
            onComplete?.Invoke(meshToModify, false);
            return;
        }

        generateHexagonHandler.GenerateHexagons(jobs, (vertices, triangles, uvs) =>
        {
            GameObject newObj = meshToModify;
            //Debug.Log(jobs.Count + " : " + vertices.Count);

            if(newObj == null)
            {
                return;
            }

            newObj.transform.position = Vector3.zero;

            Mesh hexMesh = newObj.GetComponent<MeshFilter>().mesh = new Mesh();
            hexMesh.vertices = vertices;
            int[] tris = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                tris[i] = i;
            }
            hexMesh.triangles = tris;
            hexMesh.RecalculateNormals();

            onComplete?.Invoke(newObj, true);
        });
    }
}