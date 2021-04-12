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
    private struct HexTileAStarData : IEquatable<HexTileAStarData>
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
        public System.Action<HexTile[]> onComplete;
    }

    [SerializeField]
    private HexBoardChunkHandler chunkHandler;

    public static Pathfinder Instance;
    private Queue<QueuedPathData> queuedData = new Queue<QueuedPathData>();

    private void Awake()
    {
        Instance = this;
    }

    public void FindPathToTile(HexTile to, HexTile from, System.Action<HexTile[]> onComplete)
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
            pathData.onComplete(RunPathing(pathData.to, pathData.from));
        }
    }
    public HexTile[] RunPathing(HexTile to, HexTile from)
    {
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
        Dictionary<HexTile, HexTileAStarData> coordsToData = new Dictionary<HexTile, HexTileAStarData>();

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

            coordsToData.Add(current.Tile, current);
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
                
                if (closed.Contains(neighborData))
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

    private double Cost(HexTile from, HexTile to) {
        double cost = 0;
        if (to.CantWalkThrough) {
            return double.MaxValue;
        }
        if(to.WorkArea)
        {
            cost += 10000;
        }
        
        //TODO Tile Biome Data?
        if (from.Height > 0 && to.Height > 0) {
            //cost of land travel
            if (to.Height > from.Height) {
                //uphill scalar
                cost += (to.Height - from.Height) * 10;
            } else {
                //even land/downhill
                cost += 10;
            }
        } else if(from.Height < 0 && to.Height < 0) {
            //cost of water travel
            cost += 10;
        } else {
            if (from.Height >= 0 && to.Height < 0) {
                //cost of getting on a boat
                cost += 1000;
            } else if (from.Height < 0 && to.Height >= 0) {
                //cost of getting off boat
                cost += 500;
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
        if(two.WorkArea)
        {
            extra += 10000;
        }

        return Vector3.Distance(onePos, twoPos) * HexTile.OUTER_RADIUS * 10 + extra;
    }
}
