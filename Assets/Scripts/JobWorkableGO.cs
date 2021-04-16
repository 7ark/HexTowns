using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum JobAction { ChopDownNearestTree, PlantTree }

public class JobWorkableGO : MonoBehaviour
{
    [SerializeField]
    private JobAction[] actions;
    public JobWorkable Get() { return workableObj; }

    private JobWorkable workableObj;
    private Dictionary<JobAction, Task> allJobActions = new Dictionary<JobAction, Task>();
    private Peeple activePeeple;

    public bool RequiresWork { get { return actions.Length > 0; } }

    private void Awake()
    {
        workableObj = new JobWorkable();
        workableObj.SetTotalWorkableSlots(1);

        allJobActions.Add(JobAction.ChopDownNearestTree,  new PrimitiveTask<Peeple.PeepleWS>("ChopDownNearestTree", null, null, ChopDownNearestTree));
        allJobActions.Add(JobAction.PlantTree, new PrimitiveTask<Peeple.PeepleWS>("PlantTreeNearby", null, null, PlantTreeNearby));

        workableObj.OnWorkTick += () =>
        {
            activePeeple = workableObj.GetWorker();
            for (int i = 0; i < actions.Length; i++)
            {
                activePeeple.AddToTasks(allJobActions[actions[i]]);
            }

            return false;
        };
    }

    private IEnumerator ChopDownNearestTree(System.Action<bool> onComplete)
    {
        activePeeple = workableObj.GetWorker();

        var tiles = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(workableObj.GetTilesAssociated()[0], 25);
        ResourceWorkable tree = null;
        for (int i = 0; i < tiles.Count; i++)
        {
            if(tiles[i].HasEnvironmentalItems)
            {
                Workable[] workables = tiles[i].GetEnvironmentalItemsAsWorkable();
                for (int j = 0; j < workables.Length; j++)
                {
                    ResourceWorkable resource = (ResourceWorkable)workables[j];
                    if(resource.ResourceReturn == ResourceType.Wood && resource.AbleToBeHarvested) //TODO: God improve this
                    {
                        tree = resource;
                        tiles[i].RemoveEnvironmentalItem(tree);
                        break;
                    }
                }
            }
            if(tree != null)
            {
                break;
            }
        }

        yield return activePeeple.MoveToJobSite(tree);
        tree.SetWorkLeft();
        while(true)
        {
            if (tree == null || tree.DoWork() || tree.WorkFinished)
            {
                break;
            }

            yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        }

        activePeeple.SetPeepleLocation(Peeple.PeepleLocation.Anywhere);
        onComplete(true);
    }

    private IEnumerator PlantTreeNearby(System.Action<bool> onComplete)
    {
        yield return new WaitForSeconds(Random.Range(4, 9));

        activePeeple = workableObj.GetWorker();

        HexTile tileToPlant = null;
        HexTile tileToMoveTo = null;
        var tiles = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(workableObj.GetTilesAssociated()[0], 10);
        Shuffle(ref tiles);
        for (int i = 0; i < tiles.Count; i++)
        {
            if(tiles[i].HeightLocked || tiles[i].HasWorkables || tiles[i].BuildingOnTile != null || tiles[i].WorkArea || tiles[i].CantWalkThrough)
            {
                continue;
            }

            bool neighborsClear = true;
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(tiles[i]);
            for (int j = 0; j < neighbors.Count; j++)
            {
                if(neighbors[j].BuildingOnTile != null && neighbors[j].BuildingOnTile.GetWallBetweenTiles(tiles[i], neighbors[j]) == WallStructureType.Door)
                {
                    neighborsClear = false;
                    break;
                }
                if (!neighbors[j].HeightLocked && !neighbors[j].HasWorkables && neighbors[j].BuildingOnTile == null && !neighbors[j].WorkArea && !neighbors[j].CantWalkThrough)
                {
                    tileToMoveTo = neighbors[j];
                }
            }

            if(!neighborsClear || tileToMoveTo == null)
            {
                continue;
            }

            tileToPlant = tiles[i];
            tileToPlant.WorkArea = true;
            break;
        }

        bool arrived = false;
        activePeeple.Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            arrived = true;
        });

        while(!arrived)
        {
            yield return null;
        }

        //TODO: Plant tree happen over time
        tileToPlant.ParentBoard.AddTree(tileToPlant);

        activePeeple.SetPeepleLocation(Peeple.PeepleLocation.Anywhere);
        onComplete(true);
    }
    public static void Shuffle<T>(ref List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
