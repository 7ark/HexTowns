using System;
using MeshCombineStudio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

[System.Serializable]
public struct HexCoordinates : System.IEquatable<HexCoordinates>
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get { return -X - Y; } }
    public int Height { get; set; }

    public HexCoordinates(int x, int y)
    {
        X = x;
        Y = y;
        Height = 0;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int y)
    {
        return new HexCoordinates(x - y / 2, y);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexTile.INNER_RADIUS * 2f);
        float y = -x;
        float offset = position.z / (HexTile.OUTER_RADIUS * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if(iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public Vector3 ToPosition()
    {
        const float sqrtValue = 1.73205080757f;
        const float yVal = 3f / 2f;
        float x = HexTile.OUTER_RADIUS * (sqrtValue * X + sqrtValue / 2f * Y); //X * (HexTile.INNER_RADIUS * 2f);
        float y = HexTile.OUTER_RADIUS * yVal * Y;// -X * (HexTile.OUTER_RADIUS * 3f);
        //float offset = X * (HexTile.OUTER_RADIUS * 3f);
        //x += offset;
        //y += offset;

        return new Vector3(x, 0, y);
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ")";
    }

    public string ToStringSeparate()
    {
        return X + "\n" + Y + "\n" + Z;
    }

    public bool Equals(HexCoordinates other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public static int HexDistance(HexCoordinates one, HexCoordinates two)
    {
        return (
            Mathf.Abs(one.X - two.X) +
            Mathf.Abs(one.X + one.Y - two.X - two.Y) +
            Mathf.Abs(one.Y - two.Y)) / 2;
    }

    public override bool Equals(object obj)
    {
        if(obj is HexCoordinates)
        {
            return this.Equals((HexCoordinates)obj);
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return this.X.GetHashCode() ^ this.Y.GetHashCode() ^ this.Z.GetHashCode();
    }

    public static bool operator==(HexCoordinates one, HexCoordinates two)
    {
        return one.X == two.X && one.Y == two.Y && one.Z == two.Z;
    }
    public static bool operator !=(HexCoordinates one, HexCoordinates two)
    {
        return !(one.X == two.X && one.Y == two.Y && one.Z == two.Z);
    }
}

public enum Biome
{
    None, 
    
    //Proper biomes
    Desert, Savannah, TropicalRainforest, Grassland, Shrubland, TemperateForest,
    TemperateRainforest, Taiga, Tundra,
    
    //Non-traditional
    MountainRange, River
}

public struct BoardCorners
{
    public int lowerLeft;
    public int lowerRight;
    public int upperLeft;
    public int upperRight;

    public BoardCorners(int lowerLeft, int lowerRight, int upperLeft, int upperRight)
    {
        this.lowerLeft = lowerLeft;
        this.lowerRight = lowerRight;
        this.upperLeft = upperLeft;
        this.upperRight = upperRight;
    }
}

public enum BoardInstancedType { Tree, Rock, Plant }
public enum RockType { Stone }
public enum TreeType { Oak, Pine, Pine_Snow, Jungle }

public enum PlantType { Bush, Fern }

public class HexBoard
{
    public Vector2Int tileSize;

    public  HexTile[,] board2D;
    public List<HexTile> board
    {
        get { return allTiles; }
    }
    private List<HexTile> allTiles = new List<HexTile>();
    private HexMesh hexMesh;
    private bool environmentalObjectsGenerated = false;
    private HashSet<Animal> allAnimalsOnBoard = new HashSet<Animal>();

    private Dictionary<BoardInstancedType, InstancedGenericObject[]> boardInstancedObjects = new Dictionary<BoardInstancedType, InstancedGenericObject[]>();

    public bool GeneratingTiles { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public Vector2Int Size { get { return tileSize; } }
    public BoardCorners CornerHeights { get; set; }
    public Vector3 WorldPosition { get; private set; }
    public int FarthestDistanceFromCenter { get; set; }

    [SerializeField] private Biome biome;
    public Biome BiomeTerrain { get => biome;
        set => biome = value;
    }
    public int HighestPoint { get; set; } = -500;
    public static Vector2 FullSize { get; private set; } = Vector2.zero;

    private GameObject spawningObject;
    private Dictionary<BoardInstancedType, GameObject[]> allPrefabs;

    public HexBoard()
    {
    }

    public void Init()
    {
        hexMesh = new HexMesh();
        board2D = new HexTile[Size.x, Size.y];
        CreateAllTiles();

        GameTime.Instance.OnDawnBreaks += RespawnDeadAnimals;
        GameTime.Instance.OnDawnBreaks += WakeUpAnimals;
        GameTime.Instance.OnDuskBreaks += HaveAnimalsSleep;
    }

    public void CreateAllTiles()
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                CreateTile(x, y);
            }
        }

        if(FullSize == Vector2.zero)
        {
            float left = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MaxValue;
            float top = float.MinValue;

            for (int i = 0; i < allTiles.Count; i++)
            {
                if(allTiles[i].Position.x < left)
                {
                    left = allTiles[i].Position.x;
                }
                if (allTiles[i].Position.x > right)
                {
                    right = allTiles[i].Position.x;
                }
                if (allTiles[i].Position.z < bottom)
                {
                    bottom = allTiles[i].Position.z;
                }
                if (allTiles[i].Position.z > top)
                {
                    top = allTiles[i].Position.z;
                }
            }

            FullSize = new Vector2(
                right - left,
                top - bottom
                );
        }


        //StaticBatchingUtility.Combine(gameObject);
    }

    public void Update()
    {
        GenerateEnvironmentalObjects();

        foreach (var animal in allAnimalsOnBoard)
        {
            if(!animal.Dead && !animal.Sleeping && !animal.gameObject.activeSelf)
            {
                animal.gameObject.SetActive(true);
            }
        }

        hexMesh.Update();

        foreach(var instancedObjects in boardInstancedObjects.Values)
        {
            for (int i = 0; i < instancedObjects.Length; i++)
            {
                instancedObjects[i].Update();
            }
        }
    }

    public void StoppedDisplaying()
    {
        foreach(var animal in allAnimalsOnBoard)
        {
            animal.gameObject.SetActive(false);
        }
    }

    public void HaveAnimalsSleep()
    {
        foreach (var animal in allAnimalsOnBoard)
        {
            animal.Sleep();
        }
    }

    public void WakeUpAnimals()
    {
        foreach (var animal in allAnimalsOnBoard)
        {
            animal.WakeUp();
            AnimalHandler.Instance.GiveAnimalNewPosition(animal, allTiles[Random.Range(0, allTiles.Count)]);
        }
    }

    public void RespawnDeadAnimals()
    {
        foreach (var animal in allAnimalsOnBoard)
        {
            animal.Respawn();
        }
    }

    public void RefreshBoard()
    {

        //foreach (var coord in board.Keys)
        //{
        //    board[coord].Refresh();
        //}
    }

    public struct EnvObj
    {
        public BoardInstancedType type;
        public int subType;
        public HexTile tile;
    }

    public void GenerateEnvironmentalObjects()
    {
        if (!environmentalObjectsGenerated && spawningObject != null)
        {
            environmentalObjectsGenerated = true;
            for (int i = 0; i < envObjsToAdd.Count; i++)
            {
                float yPosMod = 0;
                if (envObjsToAdd[i].type == BoardInstancedType.Tree)
                {
                    if (envObjsToAdd[i].subType == (int) TreeType.Pine || envObjsToAdd[i].subType == (int) TreeType.Pine_Snow)
                    {
                        yPosMod = Random.Range(0f, 3f);
                    }
                    else if (envObjsToAdd[i].subType == (int) TreeType.Jungle)
                    {
                        yPosMod = Random.Range(-1f, 4f);
                    }
                }
                
                Vector3 posAddition = Vector3.zero;
                if (envObjsToAdd[i].type == BoardInstancedType.Rock || envObjsToAdd[i].type == BoardInstancedType.Plant)
                {
                    posAddition = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                }
                
                Vector3 pos = envObjsToAdd[i].tile.Position + new Vector3(0, envObjsToAdd[i].tile.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP + yPosMod) + posAddition;
                Quaternion rotate = Quaternion.Euler(new Vector3(0, Random.Range(0, 361)));
                Matrix4x4 matrix = Matrix4x4.TRS(pos, rotate, allPrefabs[envObjsToAdd[i].type][envObjsToAdd[i].subType].transform.localScale);

                Guid id = boardInstancedObjects[envObjsToAdd[i].type][envObjsToAdd[i].subType].AddDataPoint(matrix, false, false);

                ResourceWorkable workable = null;
                bool used = false;
                
                int index = i;
                int work = 0;
                int worth = 0;
                ResourceType resource = ResourceType.Food;
                if (envObjsToAdd[i].type == BoardInstancedType.Tree)
                {
                    used = true;
                    resource = ResourceType.Wood;
                    switch ((TreeType)envObjsToAdd[i].subType)
                    {
                        case TreeType.Oak:
                            worth = 2;
                            work = 3;
                            break;
                        case TreeType.Pine:
                        case TreeType.Pine_Snow:
                            worth = 3;
                            work = 5;
                            break;
                        case TreeType.Jungle:
                            worth = 5;
                            work = 10;
                            break;
                    }
                }
                else if (envObjsToAdd[i].type == BoardInstancedType.Rock)
                {
                    used = true;
                    resource = ResourceType.Stone;
                }

                if (used)
                {
                    workable = new ResourceWorkable(envObjsToAdd[i].tile, work, resource,   worth); //FIX
                    workable.OnDestroyed += (w) =>
                    {
                        boardInstancedObjects[envObjsToAdd[index].type][envObjsToAdd[index].subType].RemoveDataPoint(id);
                    };
                
                    
                    envObjsToAdd[i].tile.AddEnvironmentItem(workable);
                }
            }

            foreach (var type in boardInstancedObjects.Keys)
            {
                for (int i = 0; i < boardInstancedObjects[type].Length; i++)
                {
                    boardInstancedObjects[type][i].FindCenter();
                    boardInstancedObjects[type][i].UpdateBufferManual();
                }
            }

            //Animals
            int amountOfBunies = 0;//Random.Range(0, 4);
            // if(BiomeTerrain == Biome.Forest)
            // {
            //     amountOfBunies = Random.Range(4, 8);
            // }
            // else if(BiomeTerrain == Biome.Plains)
            // {
            //     amountOfBunies = Random.Range(0, 4);
            // }

            for (int i = 0; i < amountOfBunies; i++)
            {
                allAnimalsOnBoard.Add(AnimalHandler.Instance.SpawnAnimal(AnimalType.Buny, allTiles[Random.Range(0, allTiles.Count)]));
            }
        }
    }

    private List<EnvObj> envObjsToAdd = new List<EnvObj>();
    public void AddEnvironmental(HexTile tile, BoardInstancedType type, int subType)
    {
        envObjsToAdd.Add(new EnvObj()
        {
            type = type,
            subType = subType,
            tile = tile
        });
        // Vector3 pos = tile.Position + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP);
        // Quaternion rotate = Quaternion.Euler(new Vector3(0, Random.Range(0, 361)));
        // Matrix4x4 matrix = Matrix4x4.TRS(pos, rotate, allPrefabs[BoardInstancedType.Tree][(int)TreeType.Oak].transform.localScale);
        //
        // Guid id = boardInstancedObjects[BoardInstancedType.Tree][(int)TreeType.Oak].AddDataPoint(matrix);
        //
        // ResourceWorkable treeWorkable = new ResourceWorkable(tile, 3, ResourceType.Wood, 2); //FIX
        // treeWorkable.OnDestroyed += (w) =>
        // {
        //     boardInstancedObjects[BoardInstancedType.Tree][(int)TreeType.Oak].RemoveDataPoint(id);
        // };
        //             
        // tile.AddEnvironmentItem(treeWorkable);
    }

    public Guid AddInstancedType(BoardInstancedType instancedType, HexTile tile, int subType = 0, bool instant = true, Quaternion? rotation = null, Vector3? posAdjustment = null)
    {
        Vector3 pos = tile.Position + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP) + (posAdjustment == null ? Vector3.zero : posAdjustment.Value);
        Quaternion rotate = rotation == null ? Quaternion.Euler(new Vector3(0, Random.Range(0, 361))) : rotation.Value;
        if(!allPrefabs.ContainsKey(instancedType))
        {
            Debug.LogError("Fuck");
        }
        Matrix4x4 matrix = Matrix4x4.TRS(pos, rotate, allPrefabs[instancedType][subType].transform.localScale);
        Guid referenceGuid = boardInstancedObjects[instancedType][subType].AddDataPoint(matrix);

        if(instancedType == BoardInstancedType.Tree)
        {
            if(!instant)
            {
                ResourceWorkable treeWorkable = new ResourceWorkable(tile, 3, ResourceType.Wood, 2, false);
                treeWorkable.OnDestroyed += (w) =>
                {
                    boardInstancedObjects[BoardInstancedType.Tree][(int)subType].RemoveDataPoint(referenceGuid);
                };

                tile.AddEnvironmentItem(treeWorkable);

                int minMinuteGrowthTime = 3;
                int maxMinuteGrowthTime = 6;

                HexBoardChunkHandler.Instance.StartCoroutine(GrowTree(referenceGuid, subType, pos, rotate, allPrefabs[BoardInstancedType.Tree][(int)subType].transform.localScale, Random.Range(minMinuteGrowthTime * 60, maxMinuteGrowthTime * 60), treeWorkable));
            }
        }

        return referenceGuid;
    }

    public void RemoveType(BoardInstancedType instancedType, Guid referenceType, int subType = 0)
    {
        boardInstancedObjects[instancedType][subType].RemoveDataPoint(referenceType);
    }

    public void ModifyType(BoardInstancedType instancedType, HexTile tileAssociated, Guid referenceType, Vector3 position, Quaternion rotation, int subType = 0)
    {
        boardInstancedObjects[instancedType][subType].UpdateDataPoint(referenceType, Matrix4x4.TRS(tileAssociated.Position + new Vector3(0, tileAssociated.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP) + position, rotation, allPrefabs[instancedType][subType].transform.localScale));
    }

    private IEnumerator GrowTree(Guid id, int subType, Vector3 position, Quaternion rotation, Vector3 finalScale, float timeToGrowFinal, ResourceWorkable treeWorkable)
    {
        float timePassed = 0;
        while(timePassed < timeToGrowFinal)
        {
            timePassed += Time.deltaTime;
            boardInstancedObjects[BoardInstancedType.Tree][subType].UpdateDataPoint(id, Matrix4x4.TRS(position, rotation, Vector3.Lerp(new Vector3(0.05f, 0.05f, 0.05f), finalScale, timePassed / timeToGrowFinal)));

            yield return null;
        }

        treeWorkable.AbleToBeHarvested = true;
    }

    public void GenerateMesh(GameObject spawningObject, Mesh meshBasis, Material materialInst,  Material materialSelectionInst, Camera drawCamera, Camera selectionCamera, HexagonTextureReference textureReference, Dictionary<BoardInstancedType, GameObject[]> allPrefabs)
    {
        hexMesh.SetupMeshGenerationData(meshBasis, allTiles, materialInst, materialSelectionInst, drawCamera, selectionCamera, textureReference);

        this.spawningObject = spawningObject;
        this.allPrefabs = allPrefabs;

        if(boardInstancedObjects.Count <= 0)
        {
            foreach (var type in allPrefabs.Keys)
            {
                InstancedGenericObject[] objs = new InstancedGenericObject[allPrefabs[type].Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = new InstancedGenericObject(allPrefabs[type][i]);
                }
                boardInstancedObjects.Add(type, objs);
            }
        }

        //boardInstancedObjects.Add(InstancedType.Tree, new InstancedGenericObject[]
        //{
        //    new InstancedGenericObject(treePrefabs[0])
        //});
        //
        //boardInstancedObjects.Add(InstancedType.Rock, new InstancedGenericObject[]
        //{
        //    new InstancedGenericObject(rockPrefabs[0])
        //});

    }

    private void CreateTile(int x, int y)
    {
        int positionX = x + (GridPosition.x * Size.x);
        int positionY = y + (GridPosition.y * Size.y);

        Vector3 position = new Vector3(
            (positionX + positionY * 0.5f - positionY / 2) * (HexTile.INNER_RADIUS * 2f),
            0,
            positionY * (HexTile.OUTER_RADIUS * 1.5f));

        if(x == Size.x / 2 && y == Size.y / 2)
        {
            WorldPosition = position;
        }

        var coords = HexCoordinates.FromOffsetCoordinates(positionX, positionY);

        HexTile tile = HexBoardChunkHandler.Instance.FetchTile(coords);
        tile.ParentBoard = this;
        
        board2D[x, y] = tile;
        allTiles.Add(tile);
    }

    public Material OnDisable()
    {
        return hexMesh.ReleaseData();
    }
}


