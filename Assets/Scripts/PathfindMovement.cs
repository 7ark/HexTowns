using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public System.Action<List<Vector3>, System.Action> OverrideMovementHandling;

    public float MovementSpeed { get { return movementDelay; } }

    Dictionary<HexTile, Pathfinder.HexTileAStarData> debugTileData;
    
    public HexTile GetTileOn()
    {
        return HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(transform.position));
    }

    public void SetGoal(HexTile goal, bool instant = false, System.Action<bool> arrivedComplete = null, bool alwaysDoActionEvent = false)
    {
        if(!gameObject.activeSelf || Time.timeScale == 0)
        {
            arrivedComplete?.Invoke(false);
            return;
        }
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
            HexTile current = GetTileOn();

            Debug.Log(name + " is trying to find a path to a tile");
            Pathfinder.Instance.FindPathToTile(goal, current, (pathTiles, coordData) => 
            {
                debugTileData = coordData;
                Debug.Log($"Coord Data Count: {coordData.Count}");
                iTween.Stop(gameObject);
                if (pathTiles.Length == 0)
                {
                    Debug.Log(name + " tried to find a path, but it wasn't valid");
                    currentArrivedAction?.Invoke(false);
                }
                else if(pathTiles.Length == 1)
                {
                    currentArrivedAction?.Invoke(true);
                }
                else
                {
                    Debug.Log(name + " found the path to the tile. Beginning movement.");
                    List<Vector3> positionPath = new List<Vector3> {transform.position};
                    positionPath.AddRange(pathTiles.Select(tile => 
                        tile.Position + new Vector3(0, Mathf.Max(tile.Height * HexTile.HEIGHT_STEP, HexTile.WATER_LEVEL))));

                    if(positionPath.Count <= 1)
                    {
                        Debug.LogError("Trying to move with one given position\n" + StackTraceUtility.ExtractStackTrace(), gameObject);
                    }
                    if(OverrideMovementHandling != null)
                    {
                        OverrideMovementHandling(positionPath, ArrivedAtLocation);
                    }
                    else
                    {
                        iTween.MoveTo(gameObject, iTween.Hash("path", positionPath.ToArray(), "time", positionPath.Count * movementDelay, "easetype", iTween.EaseType.linear, "orienttopath", true, "looktime", movementDelay, "delay", 0.1f, "oncomplete", "ArrivedAtLocation"));
                    }
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if (debugTileData == null || debugTileData.Count > 1000) 
            return;
        foreach (var tileData in debugTileData) {
            var tile = tileData.Key;
            Handles.Label(tile.Position + Vector3.up * tile.Height * .1f, $"{tileData.Value.F:F1}");
        }
    }
#endif
}
