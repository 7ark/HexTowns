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
                if(animal.Movement.GetTileOn() == tiles[i])
                {
                    result.Add(animal);
                }
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

    public virtual void MarkToKill()
    {
        if(!Dead && !Sleeping)
        {
            toKillWorkable = new ResourceWorkable(Movement.GetTileOn(), 1, ResourceType.Food, 1);
            toKillWorkable.TilesAssociated = new List<HexTile>() { Movement.GetTileOn() };
            toKillWorkable.BeginWorking();
            toKillWorkable.OnWorkFinished += (success)=> { if(success) Kill(); };
            toKillWorkable.DisplayedSymbol.transform.SetParent(transform);
        }
    }

    public void Kill()
    {
        Dead = true;
        gameObject.SetActive(false);
        toKillWorkable.CancelWork();
        toKillWorkable = null;
    }

    public void Sleep()
    {
        Sleeping = true;
        gameObject.SetActive(false);
        toKillWorkable.CancelWork();
        toKillWorkable = null;
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
