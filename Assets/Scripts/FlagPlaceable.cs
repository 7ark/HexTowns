using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FlagPlaceable : Placeable
{
    [SerializeField]
    private int peepleToSpawn;
    [SerializeField]
    private Peeple peeplePrefab;

    public override int ModifiedHeight { get => base.ModifiedHeight; set => base.ModifiedHeight = 0; }

    protected override void WorkCompleted(bool completedSuccessfully)
    {
        base.WorkCompleted(completedSuccessfully);

        if(!completedSuccessfully)
        {
            return;
        }

        List<HexTile> tiles = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(homeTiles[0], 4);

        List<HexTile> tileOptions = new List<HexTile>();
        for (int i = 0; i < tiles.Count; i++)
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

            Peeple peeple = GameObject.Instantiate(peeplePrefab, HexBoardChunkHandler.Instance.transform);
            peeple.Movement.SetGoal(tileOptions[index], true);
            peeple.SetHome(tileOptions[index], -1);

            tileOptions.RemoveAt(index);
        }
    }
}
