using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshCombineStudio;
using TMPro;
using System.IO;
using Random = UnityEngine.Random;

public class HexBoardChunkHandler : MonoBehaviour
{
    [SerializeField]
    private Vector2Int boardSize;
    
    [SerializeField]
    private Vector2Int tileSize = new Vector2Int(17, 17);

    [SerializeField]
    private HexBoard hexBoardPrefab;
    [SerializeField]
    private CodeRoadOne.CRO_Camera cameraController;
    [SerializeField]
    private bool generateOverTime = false;
    [SerializeField]
    private TextMeshProUGUI generationDisplay;
    [SerializeField]
    private HexagonTextureReference textureReference;
    [SerializeField]
    private Material boardMaterial;

    private HexTile[] globalTiles;
    private Vector2Int cachedLengths;
    private List<HexBoard> boardsQueuedToRender = new List<HexBoard>();
    private Dictionary<Vector2Int, HexBoard> globalBoards = new Dictionary<Vector2Int, HexBoard>();
    private Dictionary<Vector2Int, HexBoard> queuedBoards = new Dictionary<Vector2Int, HexBoard>();
    private Dictionary<Vector2Int, Biome> biomeLayout = new Dictionary<Vector2Int, Biome>();
    bool waitOne = false;
    private int seed;
    private Vector2Int[] voronoiPoints;

    public static HexBoardChunkHandler Instance;
    private static Texture2D TEXTURE_ATLAS;

    public HexagonTextureReference TextureRef { get { return textureReference; } }
    
    private void OnValidate() {
        int newBoardSizeScalar= 1;
        int newBoardSize = 1;
        while(newBoardSize < tileSize.x)
        {
            newBoardSizeScalar++;
            int currSize = (int)(Mathf.Pow(2, newBoardSizeScalar) + 1);
            if(currSize > tileSize.x)
            {
                break;
            }
            newBoardSize = currSize;
        }

        tileSize = new Vector2Int(newBoardSize, newBoardSize);
    }

    private void Awake()
    {
        Instance = this;
    }

    public void StartGeneratingWorld(int worldSize)
    {
        TEXTURE_ATLAS = textureReference.CreateAtlas();
        boardMaterial.SetTexture("_MainTex", HexBoardChunkHandler.TEXTURE_ATLAS);
        //File.WriteAllBytes(@"C:\Users\funny\Documents\ShareX\Screenshots\2021-04\test.png", TEXTURE_ATLAS.EncodeToPNG());

        seed = System.DateTime.Now.Millisecond;
        if (seed.ToString().Length < 3)
        {
            seed += 100;
        }

        Random.InitState(seed);

        StartGeneratingWorld(new Vector2Int(worldSize, worldSize));
    }

    public void StartGeneratingWorld(Vector2Int worldSize)
    {
        boardSize = worldSize;

        cachedLengths = new Vector2Int(worldSize.x * tileSize.x, worldSize.y * tileSize.y);
        globalTiles = new HexTile[cachedLengths.x * cachedLengths.y * 4];

        GenerateBiomesData();
        FillBoards(0, 0);
    }

    public bool FlattenArea(HexTile[] areaTiles, int height, bool step = false)
    {
        bool allFlattened = true;
        HashSet<HexBoard> boardsInvolvedWithTiles = new HashSet<HexBoard>();
        for (int i = 0; i < areaTiles.Length; i++)
        {
            if(areaTiles[i].HeightLocked)
            {
                continue;
            }
            if(step)
            {
                int currentHeight = areaTiles[i].Height;
                if(currentHeight == height)
                {
                    continue;
                }
                if(currentHeight < height)
                {
                    currentHeight++;
                }
                else
                {
                    currentHeight--;
                }
                if(currentHeight != height)
                {
                    allFlattened = false;
                }
                areaTiles[i].SetHeight(currentHeight);
            }
            else
            {
                areaTiles[i].SetHeight(height);
            }

            boardsInvolvedWithTiles.Add(areaTiles[i].ParentBoard);
            
            foreach (var neighborBoard in GetTileNeighborBoards(areaTiles[i])) {
                boardsInvolvedWithTiles.Add(neighborBoard);
            }
        }

        foreach (var boardToRender in boardsInvolvedWithTiles) {
            AddBoardToRender(boardToRender);
        }
        
        return allFlattened;
    }


    private void Update()
    {

        //timer -= Time.deltaTime;
        //if(timer <= 0)
        //{
        //    CheckForFillMap();
        //
        //    //Debug.Log("Render Status Update\nGlobal Boards: " + globalBoards.Count + "\nBoards Waiting to Render: " + boardsQueuedToRender.Count + "\nQueuedBoards: " + queuedBoards.Count);
        //
        //    timer = 2;
        //}

        if(waitOne)
        {
            waitOne = false;
        }
        else if(boardsQueuedToRender.Count > 0)
        {
            if(!generateOverTime)
            {
                for (int i = 0; i < boardsQueuedToRender.Count; i++)
                {
                    boardsQueuedToRender[i].GenerateMesh();
                    if (!globalBoards.ContainsKey(boardsQueuedToRender[i].GridPosition))
                    {
                        globalBoards.Add(boardsQueuedToRender[i].GridPosition, boardsQueuedToRender[0]);
                    }
                    queuedBoards.Remove(boardsQueuedToRender[i].GridPosition);
                }
                boardsQueuedToRender.Clear();
            }
            else
            {
                boardsQueuedToRender[0].GenerateMesh();
                
                if (!globalBoards.ContainsKey(boardsQueuedToRender[0].GridPosition))
                {
                    globalBoards.Add(boardsQueuedToRender[0].GridPosition, boardsQueuedToRender[0]);
                }
                
                queuedBoards.Remove(boardsQueuedToRender[0].GridPosition);
                boardsQueuedToRender.RemoveAt(0);
            }

            if(boardsQueuedToRender.Count > 0)
            {
                int totalCount = globalBoards.Count + boardsQueuedToRender.Count;
                float percentage = 1f - ((float)boardsQueuedToRender.Count / (float)totalCount);
                generationDisplay.text = percentage.ToString("00.00%") + " (" + boardsQueuedToRender.Count.ToString("000000") + " Left)";
            }
            else
            {
                generationDisplay.text = string.Empty;
                //if(!tempMover.OnBoard)
                //{
                //    tempMover.SetGoal(GetTileFromCoordinate(new HexCoordinates(0, 0)), true);
                //}
            }

            waitOne = true;
        }
    }

    private void GenerateBiomesData()
    {
        Dictionary<Vector2Int, List<Vector2Int>> voronoiChunks = new Dictionary<Vector2Int, List<Vector2Int>>();
        Dictionary<Vector2Int, Vector2Int> chunkToVoronoiPoint = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, Biome> voronoiToBiome = new Dictionary<Vector2Int, Biome>();

        voronoiPoints = new Vector2Int[boardSize.x * boardSize.y];
        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            voronoiPoints[i] = new Vector2Int(Random.Range(-boardSize.x, boardSize.x + 1), Random.Range(-boardSize.y, boardSize.y + 1));
            if(!voronoiChunks.ContainsKey(voronoiPoints[i]))
            {
                voronoiChunks.Add(voronoiPoints[i], new List<Vector2Int>());
                voronoiToBiome.Add(voronoiPoints[i], GetRandomBiome());
            }
        }


        for (int x = -boardSize.x; x <= boardSize.x; x++)
        {
            for (int y = -boardSize.y; y <= boardSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector2Int voronoiPointClosestTo = new Vector2Int(0, 0);
                float voronoiDistance = float.MaxValue;
                for (int i = 0; i < voronoiPoints.Length; i++)
                {
                    float dist = (voronoiPoints[i] - pos).magnitude;

                    if(dist < voronoiDistance)
                    {
                        voronoiDistance = dist;
                        voronoiPointClosestTo = voronoiPoints[i];
                    }
                }

                voronoiChunks[voronoiPointClosestTo].Add(pos);
                chunkToVoronoiPoint.Add(pos, voronoiPointClosestTo);
            }
        }

        for (int x = -boardSize.x; x <= boardSize.x; x++)
        {
            for (int y = -boardSize.y; y <= boardSize.y; y++)
            {
                Vector2Int voronoiPoint = chunkToVoronoiPoint[new Vector2Int(x, y)];
                biomeLayout.Add(new Vector2Int(x, y), voronoiToBiome[voronoiPoint]);
            }
        }
    }

    private void CheckForFillMap()
    {
        int xCameraReference = Mathf.RoundToInt(cameraController.transform.position.x / 100f);
        int yCameraReference = Mathf.RoundToInt(cameraController.transform.position.z / 100f);

        bool updateall = false;
        for (int x = xCameraReference - boardSize.x; x <= xCameraReference + boardSize.x; x++)
        {
            for (int y = yCameraReference - boardSize.y; y <= yCameraReference + boardSize.y; y++)
            {
                if (!globalBoards.ContainsKey(new Vector2Int(x, y)) && !queuedBoards.ContainsKey(new Vector2Int(x, y)))
                {
                    updateall = true;
                    break;
                }
            }
            if(updateall)
            {
                break;
            }
        }

        if(updateall)
        {
            int increasedDist = 2;
            FillBoards(xCameraReference, yCameraReference, increasedDist);
        }

    }

    private void FillBoards(int offsetX, int offsetY, int increasedDistance = 1)
    {
        List<HexBoard> boardsToUpdate = new List<HexBoard>();
        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                if (!globalBoards.ContainsKey(new Vector2Int(x, y)) && !queuedBoards.ContainsKey(new Vector2Int(x, y)))
                {
                    boardsToUpdate.Add(CreateNewBoard(x, y));
                }
            }
        }

        //Debug.Log("Boards to update step 1: " + boardsToUpdate.Count);

        List<HexBoard> boardsToAdd = new List<HexBoard>();
        for (int i = 0; i < boardsToUpdate.Count; i++)
        {
            List<HexBoard> neighborBoards = GetNearbyBoards(boardsToUpdate[i]);
            for (int j = 0; j < neighborBoards.Count; j++)
            {
                if (!boardsToUpdate.Contains(neighborBoards[j]))
                {
                    boardsToAdd.Add(neighborBoards[j]);
                }
            }
        }

        for (int i = 0; i < boardsToAdd.Count; i++)
        {
            boardsToUpdate.Add(boardsToAdd[i]);
        }

        //Debug.Log("Boards to update step 2: " + boardsToUpdate.Count);

        Vector2Int cameraRefPos = new Vector2Int(offsetX + boardSize.x/2, offsetY + boardSize.y/2);
        boardsToUpdate.Sort((x, y) => { return (cameraRefPos - x.GridPosition).magnitude.CompareTo((cameraRefPos - y.GridPosition).magnitude); });
        cameraController.SetTargetedPosition(new Vector3(boardSize.x / 2 * boardsToUpdate[0].Size.x * HexTile.OUTER_RADIUS * 1.5f, 0, boardSize.y / 2 * boardsToUpdate[0].Size.y * HexTile.INNER_RADIUS * 1.5f));

        for (int i = 0; i < boardsToUpdate.Count; i++)
        {
            AddBoardToRender(boardsToUpdate[i]);
        }
    }

    private void AddBoardToRender(HexBoard board)
    {
        boardsQueuedToRender.Add(board);
        if (!queuedBoards.ContainsKey(board.GridPosition))
        {
            queuedBoards.Add(board.GridPosition, board);
        }
    }

    public HexBoard CreateNewBoard(int x, int y)
    {
        const int minHeight = -50;
        const int maxHeight = 181;

        Random.State s = Random.state;

        Biome biome = Biome.None;

        HexBoard board = Instantiate(hexBoardPrefab, transform);
        board.GridPosition = new Vector2Int(x, y);
        board.tileSize = tileSize;
        board.transform.localPosition = Vector3.zero;

        //Setup corners
        int sX = x;
        int sY = y;
        BoardCorners cornerRect = new BoardCorners();
        string seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
        //Debug.Log(seedStringed);
        Random.InitState(System.Convert.ToInt32(seedStringed));

        if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
        {
            biome = GetRandomBiome();
            biomeLayout.Add(new Vector2Int(sX, sY), biome);
        }
        Random.InitState(System.Convert.ToInt32(seedStringed));
        cornerRect.lowerLeft = GetCornerHeight(biome);

        sX = x + 1;
        sY = y;
        seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
        //Debug.Log(seedStringed);
        Random.InitState(System.Convert.ToInt32(seedStringed));
        if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
        {
            biome = GetRandomBiome();
            biomeLayout.Add(new Vector2Int(sX, sY), biome);
        }
        Random.InitState(System.Convert.ToInt32(seedStringed));
        cornerRect.lowerRight = GetCornerHeight(biome);

        sX = x;
        sY = y + 1;
        seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
        //Debug.Log(seedStringed);
        Random.InitState(System.Convert.ToInt32(seedStringed));
        if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
        {
            biome = GetRandomBiome();
            biomeLayout.Add(new Vector2Int(sX, sY), biome);
        }
        Random.InitState(System.Convert.ToInt32(seedStringed));
        cornerRect.upperLeft = GetCornerHeight(biome);

        sX = x + 1;
        sY = y + 1;
        seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
        //Debug.Log(seedStringed);
        Random.InitState(System.Convert.ToInt32(seedStringed));
        if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
        {
            biome = GetRandomBiome();
            biomeLayout.Add(new Vector2Int(sX, sY), biome);
        }
        Random.InitState(System.Convert.ToInt32(seedStringed));
        cornerRect.upperRight = GetCornerHeight(biome);

        Random.state = s;

        board.CornerHeights = cornerRect;

        if (!biomeLayout.TryGetValue(new Vector2Int(x, y), out biome))
        {
            biome = GetRandomBiome();
            biomeLayout.Add(new Vector2Int(x, y), biome);
        }

        board.RunTerrainGeneration(cornerRect, biome);

        return board;
    }

    private int GetCornerHeight(Biome biome)
    {
        switch (biome)
        {
            case Biome.Hills:
                const int minHeightHills = -50;
                const int maxHeightHills = 181;

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

                if(Random.Range(0, 4) == 0)
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

    private Biome GetRandomBiome()
    {
        int val = Random.Range(0, 15);

        if(val < 2)
        {
            return Biome.Plains;
        }
        else if(val < 5)
        {
            return Biome.Hills;
        }
        else if(val < 9)
        {
            return Biome.Ocean;
        }
        else if(val < 12)
        {
            return Biome.Mountains;
        }
        else if(val < 15)
        {
            return Biome.Forest;
        }
        else if(val < 16)
        {
            return Biome.Desert;
        }
        else
        {
            return Biome.None;
        }
    }


    public List<HexBoard> GetNearbyBoards(HexBoard board)
    {
        List<HexBoard> otherBoards = new List<HexBoard>();
        for (int x = board.GridPosition.x - 1; x < board.GridPosition.x + 2; x++)
        {
            for (int y = board.GridPosition.y - 1; y < board.GridPosition.y + 2; y++)
            {
                if(board.GridPosition != new Vector2Int(x, y))
                {
                    if(globalBoards.ContainsKey(new Vector2Int(x, y)))
                    {
                        HexBoard otherBoard = globalBoards[new Vector2Int(x, y)];
                        if (otherBoard != null && otherBoard != board)
                        {
                            otherBoards.Add(otherBoard);
                        }
                    }
                    else if(queuedBoards.ContainsKey(new Vector2Int(x, y)))
                    {
                        HexBoard otherBoard = queuedBoards[new Vector2Int(x, y)];
                        if (otherBoard != null && otherBoard != board)
                        {
                            otherBoards.Add(otherBoard);
                        }
                    }
                }
            }
        }

        return otherBoards;
    }

    public int RegisterTile(HexTile tile) {
        int idx = HexCoordToGlobalIndex(tile.Coordinates);
        if (globalTiles[idx] != null) {
            Debug.LogError($"Duplicate IDX {idx}");
        }
        globalTiles[idx] = tile;
        return idx;
    }

    public int HexCoordToGlobalIndex(HexCoordinates coords) {
        return cachedLengths.x * coords.Y// Y offset
               + coords.Y / 2 //radial offset
               + coords.X;
    }

    public HexTile GetTileFromWorldPosition(Vector3 position)
    {
        return GetTileFromCoordinate(HexCoordinates.FromPosition(position));
    }

    public HexTile GetTileFromCoordinate(HexCoordinates coords)
    {
        int index = HexCoordToGlobalIndex(coords);
        return globalTiles[index];
    }

    public List<HexTile> GetTileNeighbors(HexTile tile) 
    {
        var index = tile.GlobalIndex;
        var offset = tile.Coordinates.Y % 2 == 0 ? -1 : 1;
        var neighbors = new List<HexTile>();

        CheckAndAdd(index - 1);
        CheckAndAdd(index + 1);
        CheckAndAdd(index + cachedLengths.x);
        CheckAndAdd(index + cachedLengths.x + offset);
        CheckAndAdd(index - cachedLengths.x);
        CheckAndAdd(index - cachedLengths.x + offset);

        return neighbors;

        void CheckAndAdd(int lookup) {
            if (lookup >= 0 && lookup < globalTiles.Length) {
                HexTile lookupTile = globalTiles[lookup];
                //if(lookupTile == null)
                //{
                //    Debug.LogError("TILE IS NULL IDIOT " + lookup + " (0 - " + globalTiles.Length + ")");
                //}
                if(lookupTile != null)
                {
                    neighbors.Add(lookupTile);
                }
            }
        }
    }

    public HexTile[] GetTileNeighborsInDistance(HexTile tile, int distance)
    {
        List<HexTile> allTiles = new List<HexTile>()
        {
            tile
        };

        List<HexTile> newLayer = new List<HexTile>()
        {
            tile
        };
        List<HexTile> delayedLayer = new List<HexTile>();
        for (int i = 0; i < distance; i++)
        {
            for (int j = 0; j < newLayer.Count; j++)
            {
                List<HexTile> neighbors = GetTileNeighbors(newLayer[j]);
                for (int k = 0; k < neighbors.Count; k++)
                {
                    if (!allTiles.Contains(neighbors[k]))
                    {
                        allTiles.Add(neighbors[k]);
                        delayedLayer.Add(neighbors[k]);
                    }
                }
            }
            newLayer.Clear();
            newLayer.AddRange(delayedLayer);
            delayedLayer.Clear();
        }

        return allTiles.ToArray();
    }

    public HexTile GetTileInDirection(HexTile tile, int directionX, int directionY)
    {
        HexCoordinates changedCoords = new HexCoordinates(tile.Coordinates.X + directionX, tile.Coordinates.Y + directionY);
        return GetTileFromCoordinate(changedCoords);
    }
    public IEnumerable<HexBoard> GetTileNeighborBoards(HexTile tile)
    {
        List<HexBoard> neighborBoards = new List<HexBoard>();
        var otherTiles = GetTileNeighbors(tile);

        List<HexBoard> otherBoards = GetNearbyBoards(tile.ParentBoard);
        for (int i = 0; i < otherBoards.Count; i++)
        {
            if (otherTiles.Count > 0)
            {
                neighborBoards.Add(otherBoards[i]);
            }
        }

        return neighborBoards.ToArray();
    }

}
