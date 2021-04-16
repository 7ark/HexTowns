using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingHexagon
{
    [SerializeField]
    public WallStructureType[] walls;

    public bool HasWorkStation { get { return workStation != null; } }
    [SerializeField]
    private JobWorkableGO workStation;
    public JobWorkableGO WorkStation { get { return workStation; } }

    [SerializeField]
    private int rotated = 0;

    public void Rotate(int amount)
    {
        rotated += amount;
    }

    public void SetWorkStation(JobWorkableGO workStation)
    {
        this.workStation = workStation;
    }

    private int GetWallIndex(int xDiff, int yDiff)
    {
        if (yDiff == 0)
        {
            if (xDiff == 1)
            {
                return 0;
            }
            else
            {
                return 3;
            }
        }
        if (xDiff == 0)
        {
            if (yDiff == 1)
            {
                return 5;
            }
            else
            {
                return 2;
            }
        }

        if (xDiff == 1 && yDiff == -1)
        {
            return 1;
        }
        else
        {
            return 4;
        }
    }

    public WallStructureType GetWallInDirection(HexCoordinates from, HexCoordinates to)
    {
        int xDiff = from.X - to.X;
        int yDiff = from.Y - to.Y;

        int wallIndex = GetWallIndex(xDiff, yDiff);

        if(rotated != 0)
        {
            wallIndex += rotated;
            if(wallIndex >= 6)
            {
                wallIndex -= 6;
            }
            if(wallIndex < 0)
            {
                wallIndex += 6;
            }
        }

        return walls[wallIndex];
    }
}

public class Building
{
    private HashSet<BuildingHexagon> pieces = new HashSet<BuildingHexagon>();
    private HashSet<HexTile> tiles = new HashSet<HexTile>();
    private Dictionary<BuildingHexagon, HexTile> pieceToTile = new Dictionary<BuildingHexagon, HexTile>();
    private Dictionary<HexTile, BuildingHexagon> tileToPiece = new Dictionary<HexTile, BuildingHexagon>();

    public static List<Building> AllBuildings { get; private set; } = new List<Building>();

    public Building()
    {
        AllBuildings.Add(this);
    }

    public void AddPiece(HexTile tile, BuildingHexagon hex)
    {
        pieces.Add(hex);
        tiles.Add(tile);
        pieceToTile.Add(hex, tile);
        tileToPiece.Add(tile, hex);


        tile.BuildingOnTile = this;
    }

    public void SetupWorkStations()
    {
        foreach(var hex in pieces)
        {
            if (hex.HasWorkStation && hex.WorkStation.RequiresWork)
            {
                List<HexTile> valid = new List<HexTile>();
                List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(pieceToTile[hex]);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if(tiles.Contains(neighbors[i]) && !tileToPiece[neighbors[i]].HasWorkStation)
                    {
                        valid.Add(neighbors[i]);
                    }
                }

                hex.WorkStation.Get().TilesAssociated = new List<HexTile>() { pieceToTile[hex] };
                hex.WorkStation.Get().WorkableTiles = valid;
            }
        }
    }

    public bool DoesTileHaveWorkStation(HexTile tile)
    {
        if(tiles.Contains(tile))
        {
            return tileToPiece[tile].HasWorkStation;
        }

        return false;
    }

    public JobWorkableGO GetJobWorkStationOnTile(HexTile tile)
    {
        if (tiles.Contains(tile))
        {
            return tileToPiece[tile].WorkStation;
        }

        return null;
    }

    public WallStructureType GetWallBetweenTiles(HexTile from, HexTile to)
    {
        if(tiles.Contains(from) || tiles.Contains(to))
        {
            if(tiles.Contains(to))
            {
                return tileToPiece[to].GetWallInDirection(from.Coordinates, to.Coordinates);
            }
            else
            {
                return tileToPiece[from].GetWallInDirection(from.Coordinates, to.Coordinates);
            }
        }
        else
        {
            return WallStructureType.None;
        }
    }
}
