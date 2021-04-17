using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimalType { Buny }
public class AnimalHandler : MonoBehaviour
{
    [System.Serializable]
    private struct AnimalData
    {
        public AnimalType animalType;
        public Animal prefab;
    }

    [SerializeField]
    private AnimalData[] animalDatas;

    private Dictionary<AnimalType, Animal> animalPrefabs = new Dictionary<AnimalType, Animal>();

    public static AnimalHandler Instance;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < animalDatas.Length; i++)
        {
            animalPrefabs.Add(animalDatas[i].animalType, animalDatas[i].prefab);
        }
    }

    public HexTile GiveAnimalNewPosition(Animal animal, HexTile tile)
    {
        HexTile tileToSpawnAt = tile;
        if (tile.CantWalkThrough)
        {
            List<HexTile> others = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tile, 3);
            for (int i = 0; i < others.Count; i++)
            {
                if (!others[i].CantWalkThrough && others[i].ParentBoard == tile.ParentBoard)
                {
                    tileToSpawnAt = others[i];
                    break;
                }
            }
        }

        return tileToSpawnAt;
    }

    public Animal SpawnAnimal(AnimalType type, HexTile tile)
    {
        Animal animal = Instantiate(animalPrefabs[type], transform);
        HexTile tileToSpawnAt = GiveAnimalNewPosition(animal, tile);

        animal.SetHomeBoard(tile.ParentBoard);
        animal.Movement.SetGoal(tileToSpawnAt, true);

        return animal;
    }
}
