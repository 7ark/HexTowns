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

    private Dictionary<HexagonPreviewArea.PreviewRenderData, HexTile[]> objToAssociatedTiles = new Dictionary<HexagonPreviewArea.PreviewRenderData, HexTile[]>();
    private Dictionary<HexagonPreviewArea.PreviewRenderData, int> objToAssociatedHeight = new Dictionary<HexagonPreviewArea.PreviewRenderData, int>();
    private Dictionary<HexagonPreviewArea.PreviewRenderData, System.Action> objToAssociatedCompleteAction = new Dictionary<HexagonPreviewArea.PreviewRenderData, System.Action>();
    private List<HexagonPreviewArea.PreviewRenderData> allWorkableObjects = new List<HexagonPreviewArea.PreviewRenderData>();
    private Dictionary<Workable, HexTile[]> workablesWaitingToStart = new Dictionary<Workable, HexTile[]>();
    private float workablesWaitingTimer = 0;

    public static TerrainModificationHandler Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void RequestTerrainModification(HexTile[] areaTiles, int height, HexagonPreviewArea.PreviewRenderData existingModification = null, System.Action onComplete = null)
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
            if(areaTilesList[i].Height == height)
            {
                areaTilesList.RemoveAt(i);
            }
        }

        if(areaTilesList.Count <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        HexagonPreviewArea.PreviewRenderData newObj = existingModification == null ? HexagonPreviewArea.CreateUniqueReference() : existingModification;
        if(existingModification == null)
        {
            allWorkableObjects.Add(newObj);
            objToAssociatedTiles.Add(newObj, areaTilesList.ToArray());
            objToAssociatedHeight.Add(newObj, height);
            objToAssociatedCompleteAction.Add(newObj, onComplete);


            Workable workable = new Workable();// newObj.AddComponent<Workable>();
            workable.TilesAssociated = new List<HexTile>(areaTilesList);
            workable.OnWorkTick += () => { return DoWorkOnTerrain(newObj); };
            workable.OnWorkFinished += () =>
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
        }

        HexagonPreviewArea.DisplayArea(newObj, areaTilesList, height);

        //HexagonPreviewArea.AddAreaToDisplay(areaTilesList.ToArray(), height, newObj, (GameObject modifiedObject, bool success) =>
        //{
        //    if (!success)
        //    {
        //        onComplete?.Invoke();
        //        objToAssociatedHeight.Remove(modifiedObject);
        //        objToAssociatedTiles.Remove(modifiedObject);
        //        allWorkableObjects.Remove(modifiedObject);
        //        objToAssociatedCompleteAction.Remove(modifiedObject);
        //        return;
        //    }
        //    if (existingModification == null)
        //    {
        //        Workable workable = new Workable();// newObj.AddComponent<Workable>();
        //        workable.TilesAssociated = new List<HexTile>(areaTilesList);
        //        workable.OnWorkTick += () => { return DoWorkOnTerrain(newObj); };
        //        workable.OnWorkFinished += () =>
        //        {
        //            for (int i = 0; i < areaTilesList.Count; i++)
        //            {
        //                areaTilesList[i].WorkArea = false;
        //            }
        //        };
        //        workable.SetTotalWorkableSlots(Mathf.Max(1, areaTilesList.Count / 5));
        //        workablesWaitingToStart.Add(workable, areaTilesList.ToArray());
        //        for (int i = 0; i < areaTilesList.Count; i++)
        //        {
        //            areaTilesList[i].AddWorkableToTile(workable, height);
        //            Workable[] tileWorkables = areaTilesList[i].GetEnvironmentalItemsAsWorkable();
        //            for (int j = 0; j < tileWorkables.Length; j++)
        //            {
        //                tileWorkables[j].BeginWorking();
        //            }
        //        }
        //        //PeepleJobHandler.Instance.AddWorkable(workable);
        //    }
        //
        //    newObj.transform.position = Vector3.zero;
        //});

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

    private bool DoWorkOnTerrain(HexagonPreviewArea.PreviewRenderData obj)
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

            HexagonPreviewArea.StopDisplay(obj);

            return true;
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

