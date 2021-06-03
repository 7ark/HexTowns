using MEC;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Workable
{
    [SerializeField]
    private int workStepsRequired = 1;

    private int workLeft = 0;

    public bool CanWork { get; private set; } = false;
    public bool WorkFinished { get; private set; } = false;
    public HashSet<HexTile> TilesAssociated { private get; set; } = new HashSet<HexTile>(); //Weird scenario where we do need to set this outside sometimes, but we want to use GetTilesAssociated()
    public HashSet<HexTile> WorkableTiles { private get; set; } = null; //Only set if you want to override what tiles can be worked
    public System.Action<bool> OnWorkFinished;
    public System.Action<Workable> OnDestroyed;
    public System.Func<bool> OnWorkTick;
    public bool Unworkable { get; private set; }

    protected Dictionary<ResourceType, int> resourcesNeededToStart = new Dictionary<ResourceType, int>();
    protected Dictionary<ResourceType, int> resourcesToUse = new Dictionary<ResourceType, int>();
    protected Dictionary<ResourceType, HexTile> resourcePiles = new Dictionary<ResourceType, HexTile>();
    protected List<Peeple> currentWorkers = new List<Peeple>();
    private HashSet<HexTile> currentTilesWorking = new HashSet<HexTile>();
    [SerializeField]
    protected int totalWorkSlots = 1;
    protected bool waitingOnResources = false;
    public bool RequiresResources
    {
        get
        {
            //foreach(var key in resourcesNeededToStart.Keys)
            //{
            //    if(resourcesNeededToStart[key] > 0)
            //    {
            //        return true;
            //    }
            //}
            //
            //return false;
            return waitingOnResources;
        }
    }
    public Dictionary<ResourceType, int> ResourcesNeeded
    {
        get
        {
            return resourcesNeededToStart;
        }
    }
    public Dictionary<ResourceType, HexTile> ResourcePiles
    {
        get
        {
            return resourcePiles;
        }
    }
    public int WorkSlotsAvailable
    {
        get
        {
            return totalWorkSlots - currentWorkers.Count;// waitingOnResources ? 0 : totalWorkSlots - currentWorkers.Count;
        }
    }

    public Peeple GetWorker(int index = 0)
    {
        return currentWorkers[index];
    }

    public Workable()
    {

    }

    public Workable(int workStepsRequired)
    {
        this.workStepsRequired = workStepsRequired;
    }

    public void AddResource(ResourceType type, int amount)
    {
        resourcesNeededToStart[type] -= amount;

        UpdateResourceStatus();
    }

    public void UpdateResourceStatus()
    {
        foreach (var key in resourcesNeededToStart.Keys)
        {
            if (resourcesNeededToStart[key] > 0)
            {
                return;
            }
        }

        waitingOnResources = false;
    }

    public void SetResourceRequirement(params ResourceCount[] resources)
    {
        var workableTiles = GetWorkableTiles().ToArray();
        for (int i = 0; i < resources.Length; i++)
        {
            if(resources[i].Amount > 0)
            {
                resourcesNeededToStart.Add(resources[i].ResourceType, resources[i].Amount);
                resourcesToUse.Add(resources[i].ResourceType, resources[i].Amount);
                resourcePiles.Add(resources[i].ResourceType, workableTiles[i]);
            }
        }

        waitingOnResources = true;
    }

    public void SetWorkSteps(int worksteps)
    {
        workStepsRequired = worksteps;
        workLeft = workStepsRequired;
    }

    public void MarkAsUnreachable()
    {
        Unworkable = true;

        Timing.RunCoroutine(DelayedTryAgain());
    }
    public void MarkAsOutOfResources()
    {
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }
        Unworkable = true;

        Timing.RunCoroutine(DelayedTryAgain(60));
    }

    private IEnumerator<float> DelayedTryAgain(float time = 2)
    {
        yield return Timing.WaitForSeconds(time);

        Unworkable = false;
    }

    public virtual IEnumerator<float> DoWork(Peeple specificPeepleWorking = null)
    {
        if(OnWorkTick == null)
        {
            workLeft--;
            if (workLeft <= 0)
            {
                WorkCompleted(true);
                yield break;
            }

            yield break;
        }

        bool done = OnWorkTick();

        if(done)
        {
            WorkCompleted(true);
        }

        yield break;
    }

    public void SetTotalWorkableSlots(int value)
    {
        totalWorkSlots = value;
    }

    public void SetTileWorking(HexTile tile)
    {
        currentTilesWorking.Add(tile);
    }

    public void StopWorkingTile(HexTile tile)
    {
        currentTilesWorking.Remove(tile);
    }
    public HexTile GetAssociatedTileNextToTile(HexTile tile)
    {
        var associated = GetTilesAssociated();
        foreach (var associatedTile in associated) {
            if (HexCoordinates.HexDistance(associatedTile.Coordinates, tile.Coordinates) <= 1)
            {
                return associatedTile;
            }
        }

        return null;
    }

    public IEnumerable<HexTile> GetWorkableTiles()
    {
        if(WorkableTiles != null)
        {
            return WorkableTiles;
        }

        var associatedTiles = GetTilesAssociated();
        var goodTiles = new HashSet<HexTile>();
        foreach (var neighbor in associatedTiles
            .Select(associatedTile => associatedTile.Neighbors)
            .SelectMany(neighbors => neighbors
                .Where(neighbor => 
                    !associatedTiles.Contains(neighbor) 
                    && !currentTilesWorking.Contains(neighbor)))) 
        {
            goodTiles.Add(neighbor);
        }

        return goodTiles;
    }

    public void SetWorkLeft()
    {
        workLeft = workStepsRequired;
    }

    public virtual void BeginWorking()
    {
        SetWorkLeft();
        if (workStepsRequired == 0)
        {
            WorkCompleted(true);
            return;
        }

        PeepleJobHandler.Instance.AddWorkable(this);
        CanWork = true;
    }

    public virtual void CancelWork()
    {
        if(!WorkFinished)
        {
            WorkCompleted(false);
        }
    }

    protected virtual void WorkCompleted(bool completedSuccessfully)
    {
        OnWorkFinished?.Invoke(completedSuccessfully);

        WorkFinished = true;
        PeepleJobHandler.Instance.RemoveWorkable(this);
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }

        //if(completedSuccessfully)
        //{
        //    foreach(var key in resourcePiles.Keys)
        //    {
        //        ResourceHandler.Instance.PickupResources(key, ResourceHandler.Instance.ResourcesAtLocation(resourcePiles[key]), resourcePiles[key]);
        //    }
        //}
        //else
        //{
        //    Timing.RunCoroutine(SetResourcesToReturn());
        //}
    }

    public virtual HashSet<HexTile> GetTilesAssociated()
    {
        return TilesAssociated;
    }

    public virtual HexTile GetClosestAssociatedTile(Vector3 pos)
    {
        List<HexTile> tiles = new List<HexTile>(GetWorkableTiles());
        tiles.Sort((x, y) => { return Vector3.Distance(x.Position, pos).CompareTo(Vector3.Distance(y.Position, pos)); });

        return tiles[0];
    }

    public virtual void AssignWorker(Peeple peeple)
    {
        currentWorkers.Add(peeple);
        peeple.SetCurrentJob(this);
    }

    public virtual void LeaveWork(Peeple peeple)
    {
        currentWorkers.Remove(peeple);
        peeple.SetCurrentJob(null);
    }
    public virtual void DestroySelf()
    {
        var tiles = GetTilesAssociated();
        foreach (var t in tiles) {
            t.RemoveWorkableFromTile(this);
        }
        PeepleJobHandler.Instance.RemoveWorkable(this);
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }
        OnDestroyed?.Invoke(this);
    }
}
