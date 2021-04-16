using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum JobAction { ChopDownNearestTree }

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
                    if(resource.ResourceReturn == ResourceType.Wood) //TODO: God improve this
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
}
