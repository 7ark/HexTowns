using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : HTN_Agent<int>
{
    public PathfindMovement Movement { get; private set; }
    protected HexBoard homeBoard;

    public bool Dead { get; protected set; }
    public bool Sleeping { get; protected set; }

    protected virtual void Awake()
    {
        Movement = GetComponent<PathfindMovement>();

    }

    public void SetHomeBoard(HexBoard homeBoard)
    {
        this.homeBoard = homeBoard;
    }

    protected override int GetCurrentWorldState()
    {
        return 0;
    }

    public void Kill()
    {
        Dead = true;
        gameObject.SetActive(false);
    }

    public void Sleep()
    {
        Sleeping = true;
        gameObject.SetActive(false);
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
