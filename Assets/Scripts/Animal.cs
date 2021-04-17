using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : HTN_Agent<int>
{
    public PathfindMovement Movement { get; private set; }
    protected HexBoard homeBoard;

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
}
