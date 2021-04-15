using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public struct HexTileAStarData : IEquatable<HexTileAStarData>
    {
        public HexTile Tile;
        public double F => G + H;
        public double G;
        public double H;
        public HexTile Parent;

        public bool Equals(HexTileAStarData other) => Equals(Tile, other.Tile);

        public override bool Equals(object obj) => obj is HexTileAStarData other && Equals(other);

        public override int GetHashCode() => Tile?.GetHashCode() ?? 0;
    }

    private struct QueuedPathData
    {
        public HexTile to;
        public HexTile from;
        public System.Action<HexTile[], Dictionary<HexTile, HexTileAStarData>> onComplete;
    }

    [SerializeField]
    private HexBoardChunkHandler chunkHandler;

    public static Pathfinder Instance;
    private Queue<QueuedPathData> queuedData = new Queue<QueuedPathData>();

    private void Awake()
    {
        Instance = this;
    }

    public void FindPathToTile(HexTile to, HexTile from, System.Action<HexTile[], Dictionary<HexTile, HexTileAStarData>> onComplete)
    {
        queuedData.Enqueue(new QueuedPathData()
        {
            to = to,
            from = from,
            onComplete = onComplete
        });
    }

    private void Update()
    {
        if(queuedData.Count > 0)
        {
            var pathData = queuedData.Dequeue();
            var path = RunPathing(pathData.to, pathData.from, out var coordData);
            pathData.onComplete(path, coordData);
        }
    }
    public HexTile[] RunPathing(HexTile to, HexTile from, out Dictionary<HexTile, HexTileAStarData> coordsToData)
    {
        coordsToData = new Dictionary<HexTile, HexTileAStarData>();

        if(to == from)
        {
            return new HexTile[] { to };
        }

        if(to.CantWalkThrough)
        {
            List<HexTile> nearby = chunkHandler.GetTileNeighbors(to);
            nearby.Sort((x, y) => { return Vector3.Distance(x.Position, to.Position).CompareTo(Vector3.Distance(y.Position, to.Position)); });
            bool none = true;
            for (int i = 0; i < nearby.Count; i++)
            {
                if(!nearby[i].CantWalkThrough)
                {
                    to = nearby[i];
                    none = false;
                    break;
                }
            }

            if(none)
            {
                return new HexTile[] { };
            }
        }
        List<HexTile> resultingPath = new List<HexTile>();

        HashSet<HexTileAStarData> open = new HashSet<HexTileAStarData>();
        HashSet<HexTileAStarData> closed = new HashSet<HexTileAStarData>();

        open.Add(new HexTileAStarData()
        {
            Tile = from
        });

        int safety = 0;

        while (open.Count > 0)
        {
            safety++;
            if(safety > 10000)
            {
                Debug.Log("Destination was too far! Pathfinding was unable to find a path in over 10000 attempts from " + from.Coordinates + " to " + to.Coordinates);
                return new HexTile[] { };
            }
            HexTileAStarData current = new HexTileAStarData();
            double lowestF = double.MaxValue;
            bool found = false;
            foreach (var openData in open) {
                if (openData.F < lowestF) {
                    current = openData;
                    lowestF = openData.F;
                    found = true;
                }
            }

            if (!found)
            {
                string error = "Current couldnt be found. Uh uh. We've looked at " + closed.Count + " tiles. Gonna draw lines from start to end (green to blue).\n";
                foreach (var boardData in open) {
                    error += boardData.F + "\n";
                }
                Debug.LogError(error + StackTraceUtility.ExtractStackTrace());
                Debug.DrawLine(from.Position, from.Position + new Vector3(0, 20), Color.green, 60);
                Debug.DrawLine(to.Position, to.Position + new Vector3(0, 20), Color.blue, 60);
                Debug.Break();
                return new HexTile[] { };
            }

            coordsToData[current.Tile] = current;
            open.Remove(current);
            closed.Add(current);
            
            if (current.Tile == to)
            {
                break;
            }

            List<HexTileAStarData> neighbors = chunkHandler.GetTileNeighbors(current.Tile)
                .Select(tile => new HexTileAStarData { Tile = tile }).ToList();

            for (int i = 0; i < neighbors.Count; i++)
            {
                HexTileAStarData neighborData = neighbors[i];
                
                if (closed.Contains(neighborData)
                    || !IsNavigable(current.Tile, neighborData.Tile))
                {
                    continue;
                }

                bool openContains = open.Contains(neighborData);

                double newCost = current.G + Cost(current.Tile, neighborData.Tile);
                if (newCost < neighborData.G || !openContains)
                {
                    neighborData.G = newCost;
                    neighborData.H = Heuristic(neighborData.Tile, to);
                    neighborData.Parent = current.Tile;

                    if (!openContains)
                    {
                        open.Add(neighborData);
                    }
                }

                neighbors[i] = neighborData;
            }
        }

        HexTile currentCheck = to;
        while (currentCheck != from)
        {
            resultingPath.Add(currentCheck);
            currentCheck = coordsToData[currentCheck].Parent;
        }
        resultingPath.Add(from);
        resultingPath.Reverse();
        
        //LineRenderer line = new GameObject("New Line").AddComponent<LineRenderer>();
        //line.positionCount = resultingPath.Count;
        //for (int i = 0; i < resultingPath.Count; i++) {
        //    line.SetPosition(i, resultingPath[i].Position + new Vector3(0, (resultingPath[i].Height + 1) * HexTile.HEIGHT_STEP));
        //}

        return resultingPath.ToArray();
    }

    private bool IsNavigable(HexTile from, HexTile to) {
        if (to.CantWalkThrough || to.WorkArea)
            return false;

        if (to.Height > from.Height + 20) { //too sheer of a cliff
            return false;
        }

        if (to.BuildingOnTile != null || from.BuildingOnTile != null) {
            var wallType = to.BuildingOnTile?.GetWallBetweenTiles(from, to)
                        ?? from.BuildingOnTile.GetWallBetweenTiles(to, from);
            
            if (wallType == WallStructureType.Solid || wallType == WallStructureType.Window) {
                return false;
            }

            if(to.BuildingOnTile != null && to.BuildingOnTile.DoesTileHaveWorkStation(to))
            {
                return false;
            }
        }

        return true;
    }

    private double Cost(HexTile from, HexTile to) {
        double cost = 0;

        //TODO Tile Biome Data?
        if (from.Height > 0 && to.Height > 0) { //cost of land travel
            if (to.Height > from.Height) { //up hill
                cost += 1 + (to.Height - from.Height) * HexTile.HEIGHT_STEP;
            } else { //even land/downhill
                cost += 1;
            }
        } else if (from.Height < 0 && to.Height < 0) { //cost of water travel
            cost += 1;
        } else {
            if (from.Height >= 0 && to.Height < 0) { //cost of getting on a boat
                cost += 100;
            } else if (from.Height < 0 && to.Height >= 0) { //cost of getting off boat
                cost += 5;
            }
        }

        return cost;
    }
    
    private double Heuristic(HexTile one, HexTile two)
    {
        Vector3 onePos = one.Coordinates.ToPosition();
        Vector3 twoPos = two.Coordinates.ToPosition();
        float extra = 0;
        if(two.CantWalkThrough)
        {
            return double.MaxValue;
        }

        return Vector3.Distance(onePos, twoPos) * HexTile.OUTER_RADIUS + extra;
    }
}
