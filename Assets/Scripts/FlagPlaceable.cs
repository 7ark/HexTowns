using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagPlaceable : Placeable
{
    [SerializeField]
    private int peepleToSpawn;
    [SerializeField]
    private Peeple peeplePrefab;

    public override int ModifiedHeight { get => base.ModifiedHeight; set => base.ModifiedHeight = 0; }

    protected override void WorkCompleted()
    {
        base.WorkCompleted();

        HexTile[] tiles = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(homeTiles[0], 4);

        List<HexTile> tileOptions = new List<HexTile>();
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != homeTiles[0] && !tiles[i].CantWalkThrough)
            {
                tileOptions.Add(tiles[i]);
            }
        }

        int toSpawn = Mathf.Min(peepleToSpawn, tileOptions.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            int index = Random.Range(0, tileOptions.Count);

            Peeple peeple = Instantiate(peeplePrefab, HexBoardChunkHandler.Instance.transform);
            peeple.Movement.SetGoal(tileOptions[index], true);
            peeple.SetHome(tileOptions[index]);

            tileOptions.RemoveAt(index);
        }
    }
}
