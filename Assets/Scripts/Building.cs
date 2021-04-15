using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingHexagon
{
    [SerializeField]
    public GameObject gameObjectReference;
    [SerializeField]
    public WallStructureType[] walls;

    public WallStructureType GetWallInDirection(HexCoordinates from, HexCoordinates to)
    {
        int xDiff = from.X - to.X;
        int yDiff = from.Y - to.Y;

        if(yDiff == 0)
        {
            if(xDiff == 1)
            {
                return walls[0];
            }
            else
            {
                return walls[3];
            }
        }
        if(xDiff == 0)
        {
            if(yDiff == 1)
            {
                return walls[5];
            }
            else
            {
                return walls[2];
            }
        }

        if(xDiff == 1 && yDiff == -1)
        {
            return walls[1];
        }
        else
        {
            return walls[4];
        }
    }
}
public class BuildingWall
{

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
