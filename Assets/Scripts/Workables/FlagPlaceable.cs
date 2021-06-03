using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        var tiles = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(homeTiles.First(), 4);

        List<HexTile> tileOptions = new List<HexTile>();
        foreach (var tile in tiles) {
            if (tile != homeTiles.First() && !tile.CantWalkThrough)
            {
                tileOptions.Add(tile);
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

        //Spawn resources
        int resourceIndex = Random.Range(0, tileOptions.Count);
        StorageTracker.AddStorageLocation(tileOptions[resourceIndex], ResourceType.Wood);
        ResourceHandler.Instance.SpawnNewResource(ResourceType.Wood, 50, tileOptions[resourceIndex]);
        tileOptions.RemoveAt(resourceIndex);

        resourceIndex = Random.Range(0, tileOptions.Count);
        StorageTracker.AddStorageLocation(tileOptions[resourceIndex], ResourceType.Stone);
        ResourceHandler.Instance.SpawnNewResource(ResourceType.Stone, 25, tileOptions[resourceIndex]);
        tileOptions.RemoveAt(resourceIndex);

        resourceIndex = Random.Range(0, tileOptions.Count);
        StorageTracker.AddStorageLocation(tileOptions[resourceIndex], ResourceType.Food);
        ResourceHandler.Instance.SpawnNewResource(ResourceType.Food, 50, tileOptions[resourceIndex]);
        tileOptions.RemoveAt(resourceIndex);
    }
}
