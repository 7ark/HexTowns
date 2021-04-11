using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindMovement : MonoBehaviour
{
    [SerializeField]
    private float movementDelay = 0.5f;

    private HexTile currentTile;
    private float timer = 0;

    public bool IsMoving { get; private set; } = false;
    public bool OnBoard { get; private set; } = false;
    public System.Action<bool> currentArrivedAction;
    public bool shouldDoArrivedActionEventIfCancelled = false;

    public HexTile GetTileOn()
    {
        return HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(transform.position));
    }

    public void SetGoal(HexTile goal, bool instant = false, System.Action<bool> arrivedComplete = null, bool alwaysDoActionEvent = false)
    {
        if(!OnBoard)
        {
            instant = true;
        }

        if(instant)
        {
            transform.position = goal.Position + new Vector3(0, goal.Height * HexTile.HEIGHT_STEP);
            OnBoard = true;
            arrivedComplete?.Invoke(true);
        }
        else
        {
            if (currentArrivedAction != null && shouldDoArrivedActionEventIfCancelled)
            {
                currentArrivedAction?.Invoke(false);
            }
            shouldDoArrivedActionEventIfCancelled = alwaysDoActionEvent;
            IsMoving = true;
            currentArrivedAction = arrivedComplete;
            HexTile currentTile = GetTileOn();

            Debug.Log(name + " is trying to find a path to a tile");
            Pathfinder.Instance.FindPathToTile(goal, currentTile, (pathTiles) =>
            {
                iTween.Stop(gameObject);
                if (pathTiles.Length == 0)
                {
                    Debug.Log(name + " tried to find a path, but it wasn't valid");
                    currentArrivedAction?.Invoke(false);
                }
                else
                {
                    Debug.Log(name + " found the path to the tile. Beginning movement.");
                    List<Vector3> positionPath = new List<Vector3>();
                    positionPath.Add(transform.position);
                    for (int i = 0; i < pathTiles.Length; i++)
                    {
                        HexTile tile = pathTiles[i];
                        positionPath.Add(tile.Position + new Vector3(0, Mathf.Max(tile.Height * HexTile.HEIGHT_STEP, HexTile.WATER_LEVEL)));
                    }

                    if(positionPath.Count <= 1)
                    {
                        Debug.LogError("Trying to move with one given position\n" + StackTraceUtility.ExtractStackTrace(), gameObject);
                    }
                    iTween.MoveTo(gameObject, iTween.Hash("path", positionPath.ToArray(), "time", positionPath.Count * movementDelay, "easetype", iTween.EaseType.linear, "orienttopath", true, "looktime", movementDelay, "delay", 0.1f, "oncomplete", "ArrivedAtLocation"));
                }
            });
        }

        currentTile = goal;
    }

    private void ArrivedAtLocation()
    {
        IsMoving = false;
        Debug.Log(name + " arrived at its location!");
        currentArrivedAction?.Invoke(true);
        currentArrivedAction = null;
    }

    private void Update()
    {
        //if(path != null)
        //{
        //    timer -= Time.deltaTime;
        //    if(timer <= 0)
        //    {
        //        HexTile curr = path[0];
        //        path.RemoveAt(0);
        //        transform.position = curr.Position + new Vector3(0, curr.Height * HexTile.HEIGHT_STEP);
        //
        //        timer = movementDelay;
        //
        //        if(path.Count <= 0)
        //        {
        //            path = null;
        //        }
        //    }
        //}
    }
}
