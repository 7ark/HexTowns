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

public enum Biome { None, Mountains, Hills, Plains, Ocean, Desert, Forest }

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

public class HexBoard
{
    public Vector2Int tileSize;

    private HexTile[,] board2D;
    private Dictionary<HexCoordinates, HexTile> board = new System.Collections.Generic.Dictionary<HexCoordinates, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();
    private HexMesh hexMesh;
    private bool environmentalObjectsGenerated = false;

    public bool GeneratingTiles { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public Vector2Int Size { get { return tileSize; } }
    public BoardCorners CornerHeights { get; set; }
    public Vector3 WorldPosition { get; private set; }

    [SerializeField] private Biome biome;
    public Biome BiomeTerrain { get => biome;
        private set => biome = value;
    }
    public int HighestPoint { get; private set; } = -500;
    public HexTile[] AllTilesOnBoard { get { return allTiles.ToArray(); } }
    public static Vector2 FullSize { get; private set; } = Vector2.zero;

    private GameObject spawningObject;
    private GameObject[] treePrefabs;
    private GameObject[] rockPrefabs;

    public HexBoard()
    {
    }

    public void Init()
    {
        hexMesh = new HexMesh();
        board2D = new HexTile[Size.x, Size.y];
    }

    public void CreateAllTiles(Vector2Int[] voronoiPoints, Dictionary<Vector2Int, Biome> biomeLayout)
    {
        for (int y = 0; y < Size.y; y++)
        {
            for (int x = 0; x < Size.x; x++)
            {
                CreateTile(x, y, voronoiPoints, biomeLayout);
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
        hexMesh.Update();
    }

    public void RefreshBoard()
    {

        //foreach (var coord in board.Keys)
        //{
        //    board[coord].Refresh();
        //}
    }

    public void RunTerrainGeneration(BoardCorners cornerData, Biome biome, Vector2Int[] voronoiPoints, Dictionary<Vector2Int, Biome> biomeLayout)
    {
        BiomeTerrain = biome;
        CreateAllTiles(voronoiPoints, biomeLayout);

        GenerateTerrainAlgorithm(cornerData);


        //RefreshBoard();

    }

    public void GenerateEnvironmentalObjects()
    {
        if (!environmentalObjectsGenerated && spawningObject != null)
        {
            List<HexBoard> otherBoards = HexBoardChunkHandler.Instance.GetNearbyBoards(this);
            environmentalObjectsGenerated = true;
            for (int i = 0; i < allTiles.Count; i++)
            {
                int likelinessToHaveTree = 1;
                int likelinessToHaveRock = 1;
                switch (BiomeTerrain)
                {
                    case Biome.Mountains:
                        likelinessToHaveTree = Random.Range(0, 150);
                        likelinessToHaveRock = Random.Range(0, 10);
                        break;
                    case Biome.Hills:
                        likelinessToHaveTree = Random.Range(0, 150);
                        likelinessToHaveRock = Random.Range(0, 50);
                        break;
                    case Biome.Plains:
                        likelinessToHaveTree = Random.Range(0, 100);
                        likelinessToHaveRock = Random.Range(0, 100);
                        break;
                    case Biome.Forest:
                        likelinessToHaveRock = Random.Range(0, 100);
                        int otherForests = otherBoards.Count(b => b.BiomeTerrain == Biome.Forest);

                        if (otherForests >= 6)
                        {
                            otherForests += 8;
                        }

                        likelinessToHaveTree = Random.Range(0, 20 - (otherForests));
                        break;
                }
                if (allTiles[i].Height < 150 && allTiles[i].Height > 5 && likelinessToHaveTree == 0)
                {
                    GameObject tree = GameObject.Instantiate(treePrefabs[Random.Range(0, treePrefabs.Length)], spawningObject.transform);
                    tree.transform.Rotate(new Vector3(0, Random.Range(0, 361)));
                    tree.transform.position = allTiles[i].Position + new Vector3(0, allTiles[i].Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP);

                    allTiles[i].AddEnvironmentItem(tree);
                }
                if (allTiles[i].Height > -2 && likelinessToHaveRock == 0)
                {
                    GameObject rock = GameObject.Instantiate(rockPrefabs[Random.Range(0, rockPrefabs.Length)], spawningObject.transform);
                    rock.transform.Rotate(new Vector3(0, Random.Range(0, 361)));
                    rock.transform.position = allTiles[i].Position + new Vector3(0, allTiles[i].Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP) + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));

                    allTiles[i].AddEnvironmentItem(rock);
                }
            }
        }
    }

    public void GenerateMesh(GameObject spawningObject, Mesh meshBasis, Material materialInst,  Material materialSelectionInst, Camera drawCamera, Camera selectionCamera, HexagonTextureReference textureReference, GameObject[] treePrefabs, GameObject[] rockPrefabs)
    {
        hexMesh.SetupMeshGenerationData(meshBasis, allTiles, materialInst, materialSelectionInst, drawCamera, selectionCamera, textureReference);

        this.spawningObject = spawningObject;
        this.treePrefabs = treePrefabs;
        this.rockPrefabs = rockPrefabs;
    }

    private int GetCornerHeight(Biome biome)
    {
        Debug.Log("Corner is " + biome);
        switch (biome)
        {
            case Biome.Hills:
                const int minHeightHills = 25;
                const int maxHeightHills = 161;

                return Random.Range(minHeightHills, maxHeightHills);
            case Biome.Plains:
                const int minHeightPlains = -2;
                const int maxHeightPlains = 51;

                return Random.Range(minHeightPlains, maxHeightPlains);
            case Biome.Ocean:
                const int minHeightOcean = -150;
                const int maxHeightOcean = 21;

                return Random.Range(minHeightOcean, maxHeightOcean);
            case Biome.Mountains:
                const int minHeightMountains = 50;
                const int maxHeightMountains = 301;

                if (Random.Range(0, 4) == 0)
                {
                    return Random.Range(minHeightMountains, maxHeightMountains);
                }
                else
                {
                    return Random.Range(minHeightMountains, maxHeightMountains + 200);
                }

            case Biome.Desert:
                const int minHeightDesert = 20;
                const int maxHeightDesert = 61;

                return Random.Range(minHeightDesert, maxHeightDesert);
            case Biome.Forest:
                const int minHeightForest = -5;
                const int maxHeightForest = 101;

                return Random.Range(minHeightForest, maxHeightForest);
        }

        return 0;
    }

    private void GenerateTerrainAlgorithm(BoardCorners cornerData)
    {
        //Set 4 corners
        if(board2D[0, 0].Height == 0)
        {
            int cornerHeight = GetCornerHeight(board2D[0, 0].BiomeTerrain);
            board2D[0, 0].SetHeight(cornerHeight);// cornerData.lowerLeft);
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(board2D[0, 0], 2);
            for (int i = 0; i < neighbors.Count; i++)
            {
                neighbors[i].SetHeight(cornerHeight + SpecialRandom(neighbors[i].BiomeTerrain));
                Debug.DrawLine(neighbors[i].Position, neighbors[i].Position + new Vector3(0, 20), Color.red, 10);
            }
        }
        if (board2D[Size.x - 1, 0].Height == 0)
        {
            int cornerHeight = GetCornerHeight(board2D[Size.x - 1, 0].BiomeTerrain);
            board2D[Size.x - 1, 0].SetHeight(cornerHeight); //(cornerData.lowerRight);
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(board2D[Size.x - 1, 0], 2);
            for (int i = 0; i < neighbors.Count; i++)
            {
                neighbors[i].SetHeight(cornerHeight + SpecialRandom(neighbors[i].BiomeTerrain));
            }
        }
        if (board2D[0, Size.y - 1].Height == 0)
        {
            int cornerHeight = GetCornerHeight(board2D[0, Size.y - 1].BiomeTerrain);
            board2D[0, Size.y - 1].SetHeight(cornerHeight); //(cornerData.upperLeft);
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(board2D[0, Size.y - 1], 2);
            for (int i = 0; i < neighbors.Count; i++)
            {
                neighbors[i].SetHeight(cornerHeight + SpecialRandom(neighbors[i].BiomeTerrain));
            }
        }
        if (board2D[Size.x - 1, Size.y - 1].Height == 0)
        {
            int cornerHeight = GetCornerHeight(board2D[Size.x - 1, Size.y - 1].BiomeTerrain);
            board2D[Size.x - 1, Size.y - 1].SetHeight(cornerHeight); //(cornerData.upperRight);
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(board2D[Size.x - 1, Size.y - 1], 2);
            for (int i = 0; i < neighbors.Count; i++)
            {
                neighbors[i].SetHeight(cornerHeight + SpecialRandom(neighbors[i].BiomeTerrain));
            }
        }

        DiamondAlgStep(new RectInt(0, 0, Size.x - 1, Size.y - 1));

        HighestPoint = -500;
        for (int i = 0; i < allTiles.Count; i++)
        {
            if(HighestPoint < allTiles[i].Height)
            {
                HighestPoint = allTiles[i].Height;
            }
        }
    }

    private void DiamondAlgStep(RectInt diamond, int depth = 0)
    {
        if(diamond.width < 2 || diamond.height < 2)
        {
            return;
        }

        int halfX = diamond.xMin + (diamond.xMax - diamond.xMin) / 2;
        int halfY = diamond.yMin + (diamond.yMax - diamond.yMin) / 2;

        int averageHeight =
            ((board2D[diamond.xMin, diamond.yMin].Height +
            board2D[diamond.xMin, diamond.yMax].Height +
            board2D[diamond.xMax, diamond.yMin].Height +
            board2D[diamond.xMax, diamond.yMax].Height) / 4);// + (depth % 6 == 0 ? SpecialRandom(new Vector2Int(-30, 80)) : SpecialRandom(board2D[halfX, halfY].BiomeTerrain, true));// SpecialRandom(new Vector2Int(-5, 30));

        board2D[halfX, halfY].SetHeight(averageHeight);

        board2D[halfX, diamond.yMin].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMin].Height, board2D[diamond.xMax, diamond.yMin].Height, 0.5f)) + SpecialRandom(board2D[halfX, diamond.yMin].BiomeTerrain));
        board2D[halfX, diamond.yMax].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMax].Height, board2D[diamond.xMax, diamond.yMax].Height, 0.5f)) + SpecialRandom(board2D[halfX, diamond.yMax].BiomeTerrain));
        board2D[diamond.xMin, halfY].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMin].Height, board2D[diamond.xMin, diamond.yMax].Height, 0.5f)) + SpecialRandom(board2D[diamond.xMin, halfY].BiomeTerrain));
        board2D[diamond.xMax, halfY].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMax, diamond.yMin].Height, board2D[diamond.xMax, diamond.yMax].Height, 0.5f)) + SpecialRandom(board2D[diamond.xMax, halfY].BiomeTerrain));

        DiamondAlgStep(new RectInt(diamond.xMin, diamond.yMin, halfX - diamond.xMin, halfY - diamond.yMin), depth + 1);
        DiamondAlgStep(new RectInt(halfX, diamond.yMin, diamond.xMax - halfX, halfY - diamond.yMin), depth + 1);
        DiamondAlgStep(new RectInt(diamond.xMin, halfY, halfX - diamond.xMin, diamond.yMax - halfY), depth + 1);
        DiamondAlgStep(new RectInt(halfX, halfY, diamond.xMax - halfX, diamond.yMax - halfY), depth + 1);
    }

    private int SpecialRandom(Vector2Int range)
    {
        int val = Random.Range(0, 3);
        if(val < 2)
        {
            return 0;
        }
        else
        {
            return Random.Range(range.x, range.y + 1);
        }
    }

    private int SpecialRandom(Biome biome, bool center = false)
    {
        return 0;
        if(center)
        {
            switch (biome)
            {
                case Biome.Hills:
                    return Random.Range(-7, 11);
                case Biome.Plains:
                    return Random.Range(-6, 7);
                case Biome.Ocean:
                    return Random.Range(-13, 8);
                case Biome.Mountains:
                    return Random.Range(-20, 21);
                case Biome.Forest:
                    return Random.Range(-6, 9);
            }
        }
        else
        {
            switch (biome)
            {
                case Biome.Hills:
                    return Random.Range(-2, 4);
                case Biome.Plains:
                    return Random.Range(-2, 3);
                case Biome.Ocean:
                    return Random.Range(-8, 3);
                case Biome.Mountains:
                    return Random.Range(-2, 13);
                case Biome.Forest:
                    return Random.Range(-1, 4);
            }
        }

        return 0;
    }

    private void CreateTile(int x, int y, Vector2Int[] voronoiPoints, Dictionary<Vector2Int, Biome> biomeLayout)
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


        HexTile tile = new HexTile {
            Position = position,
            ParentBoard = this,
            Coordinates = HexCoordinates.FromOffsetCoordinates(positionX, positionY)
        };

        Vector2Int closest = new Vector2Int();
        float closestDist = float.MaxValue;
        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            float dist = Vector3.Distance(position, new Vector3(voronoiPoints[i].x * (HexTile.OUTER_RADIUS * 2), 0, voronoiPoints[i].y * (HexTile.OUTER_RADIUS * 2)));
            if(dist < closestDist)
            {
                closestDist = dist;
                closest = voronoiPoints[i];
            }

            Vector3 p = new Vector3(voronoiPoints[i].x, 0, voronoiPoints[i].y);
            //Debug.DrawLine(p, p + new Vector3(0, 50), Color.red, 10);
        }
        tile.BiomeTerrain = biomeLayout[closest];

        Color biomeColor = Color.white;
        switch (tile.BiomeTerrain)
        {
            case Biome.Mountains:
                biomeColor = Color.grey;
                break;
            case Biome.Hills:
                biomeColor = Color.yellow;
                break;
            case Biome.Plains:
                biomeColor = Color.green;
                break;
            case Biome.Ocean:
                biomeColor = Color.blue;
                break;
            case Biome.Forest:
                biomeColor = Color.cyan;
                break;
        }
        //Debug.DrawLine(tile.Position, tile.Position + new Vector3(0, 20), biomeColor, 10);

        board.Add(tile.Coordinates, tile);
        board2D[x, y] = tile;
        allTiles.Add(tile);
        tile.GlobalIndex = HexBoardChunkHandler.Instance.RegisterTile(tile);
    }

    public Material OnDisable()
    {
        return hexMesh.ReleaseData();
    }
}
