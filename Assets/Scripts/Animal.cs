using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : HTN_Agent<int>
{
    public PathfindMovement Movement { get; private set; }
    protected HexBoard homeBoard;

    public bool Dead { get; protected set; }
    public bool Sleeping { get; protected set; }

    protected ResourceWorkable toKillWorkable;

    private static HashSet<Animal> allAnimals = new HashSet<Animal>();

    public static HashSet<Animal> GetAnimalsOnTiles(List<HexTile> tiles)
    {
        HashSet<Animal> result = new HashSet<Animal>();

        for (int i = 0; i < tiles.Count; i++)
        {
            foreach(var animal in allAnimals)
            {
                if(!animal.Dead && !animal.Sleeping && animal.Movement.GetTileOn() == tiles[i])
                {
                    result.Add(animal);
                }
            }
        }

        return result;
    }

    public static HashSet<Animal> GetAnimalsWithinRange(HexTile tile, float distance)
    {
        HashSet<Animal> result = new HashSet<Animal>();

        foreach (var animal in allAnimals)
        {
            float dist = Vector3.Distance(animal.transform.position, tile.Position + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP));
            if (dist < distance && !animal.Dead && !animal.Sleeping)
            {
                result.Add(animal);
            }
        }

        return result;
    }

    protected virtual void Awake()
    {
        Movement = GetComponent<PathfindMovement>();

        allAnimals.Add(this);
    }

    public void SetHomeBoard(HexBoard homeBoard)
    {
        this.homeBoard = homeBoard;
    }

    protected override int GetCurrentWorldState()
    {
        return 0;
    }

    public virtual ResourceWorkable MarkToKill(bool startWork = true)
    {
        if(!Dead && !Sleeping)
        {
            toKillWorkable = new ResourceWorkable(Movement.GetTileOn(), 2, ResourceType.Food, 15);
            toKillWorkable.TilesAssociated = new List<HexTile>() { Movement.GetTileOn() };
            if(startWork)
            {
                toKillWorkable.BeginWorking();
                toKillWorkable.DisplayedSymbol.transform.SetParent(transform);
            }
            toKillWorkable.OnWorkFinished += (success)=> { if(success) Kill(); };

            return toKillWorkable;
        }

        return null;
    }

    public void Kill()
    {
        Dead = true;
        gameObject.SetActive(false);
        if(toKillWorkable != null)
        {
            toKillWorkable.CancelWork();
            toKillWorkable = null;
        }
    }

    public void Sleep()
    {
        Sleeping = true;
        gameObject.SetActive(false);
        if (toKillWorkable != null)
        {
            toKillWorkable.CancelWork();
            toKillWorkable = null;
        }
    }

    public void WakeUp()
    {
        Sleeping = false;
    }

    public void Respawn()
    {
        Dead = false;
    }
}
