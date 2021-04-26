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
    public bool Unreachable { get; private set; }

    private List<Peeple> currentWorkers = new List<Peeple>();
    private HashSet<HexTile> currentTilesWorking = new HashSet<HexTile>();
    protected int totalWorkSlots = 1;
    public int WorkSlotsAvailable
    {
        get
        {
            return totalWorkSlots - currentWorkers.Count;
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

    public void SetWorkSteps(int worksteps)
    {
        workStepsRequired = worksteps;
        workLeft = workStepsRequired;
    }

    public void MarkAsUnreachable()
    {
        Unreachable = true;

        Timing.RunCoroutine(DelayedTryAgain());
    }

    private IEnumerator<float> DelayedTryAgain()
    {
        yield return Timing.WaitForSeconds(2);

        Unreachable = false;
    }

    public virtual bool DoWork()
    {
        if(OnWorkTick == null)
        {
            workLeft--;
            if (workLeft <= 0)
            {
                WorkCompleted(true);
                return true;
            }

            return false;
        }

        bool done = OnWorkTick();

        if(done)
        {
            WorkCompleted(true);
        }

        return done;
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
