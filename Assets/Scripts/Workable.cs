using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Workable
{
    [SerializeField]
    private int workStepsRequired = 1;

    private int workLeft = 0;

    public bool CanWork { get; private set; } = false;
    public bool WorkFinished { get; private set; } = false;
    public List<HexTile> TilesAssociated { private get; set; } = new List<HexTile>(); //Weird scenario where we do need to set this outside sometimes, but we want to use GetTilesAssociated()
    public System.Action<bool> OnWorkFinished;
    public System.Action<Workable> OnDestroyed;
    public System.Func<bool> OnWorkTick;
    public bool Unreachable { get; private set; }

    private List<Peeple> currentWorkers = new List<Peeple>();
    private List<HexTile> currentTilesWorking = new List<HexTile>();
    protected int totalWorkSlots = 1;
    public int WorkSlotsAvailable
    {
        get
        {
            return totalWorkSlots - currentWorkers.Count;
        }
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

    public HexTile[] GetWorkableTiles()
    {
        List<HexTile> associatedTiles = GetTilesAssociated();
        List<HexTile> goodTiles = new List<HexTile>();
        for (int i = 0; i < associatedTiles.Count; i++)
        {
            HexTile[] neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(associatedTiles[i]).ToArray();
            for (int j = 0; j < neighbors.Length; j++)
            {
                if(!associatedTiles.Contains(neighbors[j]))
                {
                    goodTiles.Add(neighbors[j]);
                }
            }
        }

        for (int i = 0; i < currentTilesWorking.Count; i++)
        {
            goodTiles.Remove(currentTilesWorking[i]);
        }

        return goodTiles.ToArray();
    }

    public virtual void BeginWorking()
    {
        workLeft = workStepsRequired;
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
    public virtual List<HexTile> GetTilesAssociated()
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
    protected virtual void DestroySelf()
    {
        List<HexTile> tiles = GetTilesAssociated();
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].RemoveWorkableFromTile(this);
        }
        PeepleJobHandler.Instance.RemoveWorkable(this);
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }
        OnDestroyed?.Invoke(this);
    }
}
