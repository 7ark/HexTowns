using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workable : MonoBehaviour
{
    [SerializeField]
    private int workStepsRequired = 1;

    private int workLeft = 0;

    public bool CanBuild { get; private set; } = false;
    public bool WorkFinished { get; private set; } = false;
    public List<HexTile> TilesAssociated { private get; set; } = new List<HexTile>(); //Weird scenario where we do need to set this outside sometimes, but we want to use GetTilesAssociated()
    public System.Action OnBuilt;
    public System.Action<GameObject> OnDestroyed;
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

    public void SetWorkSteps(int worksteps)
    {
        workStepsRequired = worksteps;
        workLeft = workStepsRequired;
    }

    protected virtual void Awake()
    {
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
                WorkCompleted();
                return true;
            }

            return false;
        }

        bool done = OnWorkTick();

        if(done)
        {
            WorkCompleted();
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
        List<HexTile> additionalSurroundings = new List<HexTile>();
        List<HexTile> goodTiles = new List<HexTile>(GetTilesAssociated());
        for (int i = 0; i < goodTiles.Count; i++)
        {
            HexTile[] neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(goodTiles[i]).ToArray();
            for (int j = 0; j < neighbors.Length; j++)
            {
                if(!goodTiles.Contains(neighbors[j]))
                {
                    additionalSurroundings.Add(neighbors[j]);
                }
            }
        }

        goodTiles.AddRange(additionalSurroundings);

        for (int i = 0; i < currentTilesWorking.Count; i++)
        {
            goodTiles.Remove(currentTilesWorking[i]);
        }

        return goodTiles.ToArray();
    }

    public virtual void BeginWorking()
    {
        if(workStepsRequired == 0)
        {
            WorkCompleted();
            return;
        }

        PeepleJobHandler.Instance.AddWorkable(this);
        CanBuild = true;
    }

    public virtual void CancelWork()
    {
        if(!WorkFinished)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void WorkCompleted()
    {
        OnBuilt?.Invoke();

        WorkFinished = true;
        PeepleJobHandler.Instance.RemoveWorkable(this);
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }
    }
    public virtual HexTile[] GetTilesAssociated()
    {
        return TilesAssociated.ToArray();
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
    protected virtual void OnDestroy()
    {
        HexTile[] tiles = GetTilesAssociated();
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].RemoveWorkableFromTile(this);
        }
        PeepleJobHandler.Instance.RemoveWorkable(this);
        for (int i = 0; i < currentWorkers.Count; i++)
        {
            LeaveWork(currentWorkers[i]);
        }
        OnDestroyed?.Invoke(gameObject);
    }
}
