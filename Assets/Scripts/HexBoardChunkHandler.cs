using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshCombineStudio;
using TMPro;
using System.IO;
using System.Linq;
using Random = UnityEngine.Random;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

public class HexBoardChunkHandler : MonoBehaviour
{
    [SerializeField]
    private Vector2Int boardSize;
    
    [SerializeField]
    private Vector2Int tileSize = new Vector2Int(17, 17);

    [SerializeField]
    private CodeRoadOne.CRO_Camera cameraController;
    [SerializeField]
    private bool generateOverTime = false;
    [SerializeField]
    private TextMeshProUGUI generationDisplay;
    [SerializeField]
    private HexagonTextureReference textureReference;
    [SerializeField]
    private Vector2Int cameraViewRange = new Vector2Int(1, 10);
    [SerializeField]
    private GameObject[] treePrefabs;
    [SerializeField]
    private GameObject[] rockPrefabs;

    [SerializeField]
    private Material baseMaterial;
    [SerializeField]
    private Material baseMaterialSelection;
    [SerializeField]
    private Camera drawingCamera;
    [SerializeField]
    private Camera selectionCamera;

    [SerializeField]
    private Material hexagonPreviewMaterial;
    [SerializeField]
    private int debugSeed;


    [System.Serializable]
    public struct HexBufferData
    {
        public Vector4 pos_s;
        public float index;
    };

    private Material materialInst;
    [SerializeField]
    private Mesh mesh;
    private Texture2D[] textures;

    [SerializeField]
    private Texture2DArray textureArray;
    
    //Master container
    private HexTile[] globalTiles;
    private Vector2Int cachedLengths;
    private List<HexBoard> boardsQueuedToRender = new List<HexBoard>();
    private Dictionary<Vector2Int, HexBoard> globalBoards = new Dictionary<Vector2Int, HexBoard>();
    private Dictionary<Vector2Int, HexBoard> queuedBoards = new Dictionary<Vector2Int, HexBoard>();
    private Dictionary<Vector2Int, Biome> biomeLayout = new Dictionary<Vector2Int, Biome>();
    private HexBoard[,] allBoards;
    bool waitOne = false;
    private int seed;
    private HashSet<Vector2Int> voronoiPoints;
    private HashSet<HexBoard> lastRendered = new HashSet<HexBoard>();

    public static HexBoardChunkHandler Instance;

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

    private void CreateBaseMesh()
    {
        GenerateHexagonHandler generateHexagonHandler = gameObject.AddComponent<GenerateHexagonHandler>();
        NativeArray<int> neighborHeights = new NativeArray<int>(6, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<Vector3> neighborPositions = new NativeArray<Vector3>(6, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        HexCoordinates[] neighborPos = new HexCoordinates[]
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(0, 1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(0, -1),
            new HexCoordinates(1, -1),
            new HexCoordinates(-1, 1),

        };
        for (int j = 0; j < neighborHeights.Length; j++)
        {
            neighborHeights[j] = 0;
            neighborPositions[j] = neighborPos[j].ToPosition();
        }
        NativeArray<Rect> test = new NativeArray<Rect>(new Rect[] { new Rect()
        {
            min = new Vector2(0, 0),
            max = new Vector2(1, 1)
        }}, Allocator.TempJob);
        generateHexagonHandler.GenerateHexagon(new TriangulateTileJob()
        {
            position = new Vector3(0, 1, 0),
            vertices = new NativeList<Vector3>(Allocator.TempJob),
            triangles = new NativeList<int>(Allocator.TempJob),
            textureID = 0,
            uvs = new NativeList<Vector2>(Allocator.TempJob),
            uvData = test,
            height = 10,
            scale = 1,
            neighborArrayCount = 6,
            neighborHeight = neighborHeights,
            neighborPositions = neighborPositions
        }, (Vector3[] vertices, int[] triangles, Vector2[] uvs) =>
        {
            mesh = new Mesh();

            mesh.vertices = vertices;
            int[] tris = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                tris[i] = i;
            }
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
        });
    }

    private void Awake()
    {
        Instance = this;


        materialInst = new Material(baseMaterial);

        CreateBaseMesh();
        textures = textureReference.OrganizeAllTextures().ToArray();

        // copied from https://catlikecoding.com/unity/tutorials/hex-map/part-14/
        Texture2D t = textures[0];
        textureArray = new Texture2DArray(
            t.width, t.height, textures.Length, t.format, false
        );

        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        for (int i = 0; i < textures.Length; i++)
        {
            for (int m = 0; m < t.mipmapCount; m++)
            {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        materialInst.mainTexture = textureArray;
    }

    private void Start()
    {
        GameTime.Instance.SetTimeSpeed(0);
        HexagonPreviewArea.Initialize(mesh, hexagonPreviewMaterial, drawingCamera);
    }

    public void StartGeneratingWorld(int worldSize)
    {
        //boardMaterial.SetTexture("_MainTex", HexBoardChunkHandler.TEXTURE_ATLAS);
        //File.WriteAllBytes(@"C:\Users\funny\Documents\ShareX\Screenshots\2021-04\test.png", TEXTURE_ATLAS.EncodeToPNG());

        seed = System.DateTime.Now.Millisecond;
        if (seed.ToString().Length < 3)
        {
            seed += 100;
        }

        Random.InitState(seed);
        Random.InitState(debugSeed);

        StartGeneratingWorld(new Vector2Int(worldSize, worldSize));

        GameTime.Instance.SetTimeSpeed(1);
        GameTime.Instance.SetTime(8);
    }

    public void StartGeneratingWorld(Vector2Int worldSize)
    {
        boardSize = worldSize;

        cachedLengths = new Vector2Int(worldSize.x * tileSize.x, worldSize.y * tileSize.y);
        globalTiles = new HexTile[cachedLengths.x * cachedLengths.y];
        FillTiles();

        FillBoards(0, 0);
    }

    private void FillTiles() {
        for (var i = 0; i < cachedLengths.x; i++)
        for (var j = 0; j < cachedLengths.y; j++) {
            var index = cachedLengths.x * j + i;
            globalTiles[index] = new HexTile(i, j, index);
        }
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
        }

        foreach (var boardToRender in boardsInvolvedWithTiles) {
            AddBoardToRender(boardToRender);
        }
        
        return allFlattened;
    }

    

    private void Update()
    {
        HexagonPreviewArea.Update();

        float currentZoom = cameraController.GetTargetedZoomLevel();
        Vector3 cameraPosition = cameraController.GetTargetedPosition();
        int camX = (int)(cameraPosition.x / HexBoard.FullSize.x);
        int camY = (int)(cameraPosition.z / HexBoard.FullSize.y);

        HashSet<HexBoard> renderedThisFrame = new HashSet<HexBoard>();

        int range = (int)Mathf.Lerp(cameraViewRange.x, cameraViewRange.y, currentZoom);
        if(allBoards != null && allBoards.Length > 0)
        {
            for (int x = camX - range; x <= camX + range; x++)
            {
                for (int y = camY - range; y <= camY + range; y++)
                {
                    bool valid =
                        x >= 0 &&
                        x < allBoards.GetLength(0) &&
                        y >= 0 &&
                        y < allBoards.GetLength(1) &&
                        allBoards[x, y] != null;

                    if (valid)
                    {
                        renderedThisFrame.Add(allBoards[x, y]);
                        lastRendered.Add(allBoards[x, y]);
                        allBoards[x, y].Update();
                    }
                }
            }
        }

        foreach (var board in lastRendered)
        {
            if(!renderedThisFrame.Contains(board))
            {
                board.StoppedDisplaying();
            }
        }
        lastRendered.Clear();
        lastRendered = renderedThisFrame;

        //for (int x = 0; x < allBoards.GetLength(0); x++)
        //{
        //    for (int y = 0; y < allBoards.GetLength(1); y++)
        //    {
        //        allBoards[x, y].Update();
        //    }
        //}

        if (waitOne)
        {
            waitOne = false;
        }
        else if(boardsQueuedToRender.Count > 0)
        {
            if (!generateOverTime)
            {
                for (int i = 0; i < boardsQueuedToRender.Count; i++)
                {
                    boardsQueuedToRender[i].GenerateMesh(gameObject, mesh, materialInst, baseMaterialSelection, drawingCamera, selectionCamera, textureReference, treePrefabs, rockPrefabs);
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
                const int chunksToRenderPerFrame = 10;
                for (int i = 0; i < Mathf.Min(boardsQueuedToRender.Count, chunksToRenderPerFrame); i++)
                {
                    boardsQueuedToRender[0].GenerateMesh(gameObject, mesh, materialInst, baseMaterialSelection, drawingCamera, selectionCamera, textureReference, treePrefabs, rockPrefabs);

                    if (!globalBoards.ContainsKey(boardsQueuedToRender[0].GridPosition))
                    {
                        globalBoards.Add(boardsQueuedToRender[0].GridPosition, boardsQueuedToRender[0]);
                    }

                    queuedBoards.Remove(boardsQueuedToRender[0].GridPosition);
                    boardsQueuedToRender.RemoveAt(0);
                }
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

            //waitOne = true;
        }
    }

    private void GenerateBiomesData()
    {
        Dictionary<Vector2Int, List<Vector2Int>> voronoiChunks = new Dictionary<Vector2Int, List<Vector2Int>>();
        Dictionary<Vector2Int, Vector2Int> chunkToVoronoiPoint = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, Biome> voronoiToBiome = new Dictionary<Vector2Int, Biome>();

        voronoiPoints = new HashSet<Vector2Int>();
        int length = boardSize.x * boardSize.y / 4;
        for (int i = 0; i < length; i++)
        {

            HexTile tile = null;
            while(tile == null)
            {
                tile = globalTiles[Random.Range(0, globalTiles.Length)];
            }
            Vector3 randomTile = tile.Position;
            Vector2Int point = new Vector2Int((int)randomTile.x, (int)randomTile.z);// new Vector2Int(Random.Range(0, boardSize.x + 1), Random.Range(0, boardSize.y + 1));
            if(!voronoiPoints.Contains(point))
            {
                voronoiPoints.Add(point);
            }
            //voronoiPoints[i] = point;
            //if(!voronoiChunks.ContainsKey(voronoiPoints[i]))
            //{
            //    voronoiChunks.Add(voronoiPoints[i], new List<Vector2Int>());
            //    voronoiToBiome.Add(voronoiPoints[i], GetRandomBiome());
            //}
        }

        
        //for (int x = -boardSize.x; x <= boardSize.x; x++)
        //{
        //    for (int y = -boardSize.y; y <= boardSize.y; y++)
        //    {
        //        Vector2Int pos = new Vector2Int(x, y);
        //        Vector2Int voronoiPointClosestTo = new Vector2Int(0, 0);
        //        float voronoiDistance = float.MaxValue;
        //        for (int i = 0; i < voronoiPoints.Length; i++)
        //        {
        //            float dist = (voronoiPoints[i] - pos).magnitude;
        //
        //            if(dist < voronoiDistance)
        //            {
        //                voronoiDistance = dist;
        //                voronoiPointClosestTo = voronoiPoints[i];
        //            }
        //        }
        //
        //        voronoiChunks[voronoiPointClosestTo].Add(pos);
        //        chunkToVoronoiPoint.Add(pos, voronoiPointClosestTo);
        //    }
        //}
        //
        //for (int x = -boardSize.x; x <= boardSize.x; x++)
        //{
        //    for (int y = -boardSize.y; y <= boardSize.y; y++)
        //    {
        //        Vector2Int voronoiPoint = chunkToVoronoiPoint[new Vector2Int(x, y)];
        //        biomeLayout.Add(new Vector2Int(x, y), voronoiToBiome[voronoiPoint]);
        //    }
        //}
    }

    private void FillBoards(int offsetX, int offsetY, int increasedDistance = 1)
    {
        allBoards = new HexBoard[boardSize.x, boardSize.y];
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


        GenerateBiomesData();

        //GenerateTerrain();
        StartCoroutine(GenerateTerrain());

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

    public enum TectonicPlateType { Continental, Oceanic }
    public class TectonicPlate
    {
        public HashSet<HexTile> tilesInPlate = new HashSet<HexTile>();
        public HashSet<HexTile> tileBorders = new HashSet<HexTile>();
        public HexCoordinates movementDirection;

        public int density = 0;
        public TectonicPlateType plateType;
    }

    private struct ExtraTerrainMod
    {
        public HexTile tile;
        public Vector2Int generalHeightRange;
        public Vector2Int heightFalloutRange;
        public bool heightIncrease;
    }

    private struct Droplet
    {
        public HexTile position;
        public float speed;
        public float water;
        public float sediment;
        public HexCoordinates direction;
    }

    private IEnumerator GenerateTerrain()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        //Dictionary<HexTile, TectonicPlate> tilePlateParent = new Dictionary<HexTile, TectonicPlate>();
        Dictionary<Vector2Int, TectonicPlate> plates = new Dictionary<Vector2Int, TectonicPlate>();
        Dictionary<HexTile, int> tempTileHeights = new Dictionary<HexTile, int>();
        HexCoordinates[] directions = new HexCoordinates[]
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(0, 1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(0, -1),
            new HexCoordinates(1, -1),
            new HexCoordinates(-1, 1),
        };
        foreach(var point in voronoiPoints)
        {
            plates.Add(point, new TectonicPlate()
            {
                movementDirection = directions[Random.Range(0, directions.Length)],
                density = Random.Range(0, 3),
                plateType = Random.Range(0, 3) == 0 ? TectonicPlateType.Oceanic : TectonicPlateType.Continental
            });
        }
        Debug.Log("Time to create plates " + stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();
        for (int i = 0; i < globalTiles.Length; i++)
        {
            if(globalTiles[i] == null)
            {
                continue;
            }
            float closestDist = float.MaxValue;
            Vector2Int closestVoronoi = new Vector2Int(-1, -1);

            foreach (var point in voronoiPoints)
            {
                float dist = Vector2.Distance(new Vector2(globalTiles[i].Position.x, globalTiles[i].Position.z), point);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestVoronoi = point;
                }
            }

            plates[closestVoronoi].tilesInPlate.Add(globalTiles[i]);
            globalTiles[i].Plate = plates[closestVoronoi];
            //tilePlateParent.Add(globalTiles[i], plates[closestVoronoi]);
            tempTileHeights.Add(globalTiles[i], 0);
        }
        Debug.Log("Time to assign tiles to plates " + stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        foreach (var plate in plates.Values)
        {
            foreach (var tile in plate.tilesInPlate)
            {
                var tileNeighbors = tile.Neighbors;
                bool isNeighbor = true;
                for (int i = 0; i < tileNeighbors.Count; i++)
                {
                    if(tileNeighbors[i].Plate != plate)
                    {
                        isNeighbor = false;
                        break;
                    }
                }
                if(!isNeighbor)
                {
                    plate.tileBorders.Add(tile);
                }
            }
        }

        foreach (var plate in plates.Values)
        {
            foreach (var tile in plate.tilesInPlate)
            {
                if(plate.plateType == TectonicPlateType.Oceanic)
                {
                    //Debug.DrawLine(tile.Position, tile.Position + new Vector3(0, 20), Color.blue, 60);

                    tile.SetHeight(-75);
                }
                else
                {

                    tile.SetHeight(10);
                    //Debug.DrawLine(tile.Position, tile.Position + new Vector3(0, 20), Color.green, 60);
                }
            }
        }
        for (int smoothness = 0; smoothness < 10; smoothness++)
        {
            for (int i = 0; i < globalTiles.Length; i++)
            {
                if (globalTiles[i] == null)
                {
                    continue;
                }

                var neighbors = globalTiles[i].Neighbors;
                int avgHeight = 0;
                for (int j = 0; j < neighbors.Count; j++)
                {
                    avgHeight += neighbors[j].Height;
                }
                avgHeight /= neighbors.Count;
                if (avgHeight != globalTiles[i].Height)
                {
                    int newHeight = avgHeight + Random.Range(-2, 3);
                    if (globalTiles[i].Height >= 0 && newHeight < 0)
                    {
                        newHeight = 0;
                    }
                    globalTiles[i].SetHeight(newHeight);
                }
            }
        }

        for (int i = 0; i < globalTiles.Length; i++)
        {
            if (globalTiles[i] == null)
            {
                continue;
            }

            tempTileHeights[globalTiles[i]] = globalTiles[i].Height;
        }


        int primaryDetailIterations = 50;
        Vector2Int volcanoIterationRange = new Vector2Int(5, 15);
        int tileDistanceEffect = 5; //Was 15
        int tileLargeDistanceEffect = 15; //Was 30
        int tileDistanceIncrease = 4;
        int exponentialIncrease = 3;
        int mountainIterations = 5;

        for (int tectonicIterations = 0; tectonicIterations < primaryDetailIterations; tectonicIterations++)
        {
            foreach (var plate in plates.Values)
            {
                foreach (var tile in plate.tileBorders)
                {
                    HexCoordinates forwardMovedDirection = tile.Coordinates + plate.movementDirection;
                    HexTile tileInSpaceForward = GetTileFromCoordinate(forwardMovedDirection);

                    if (tileInSpaceForward != null)
                    {
                        //The movement direction is running into another plate
                        if (tileInSpaceForward.Plate != plate)
                        {
                            if (HexCoordinates.AreOpposites(plate.movementDirection, tileInSpaceForward.Plate.movementDirection))
                            {
                                if (plate.plateType == TectonicPlateType.Continental && tileInSpaceForward.Plate.plateType == TectonicPlateType.Continental)
                                {
                                    for (int i = 0; i < mountainIterations; i++)
                                    {
                                        var nearby = tile.GetTileNeighborsInDistance(tileDistanceEffect + 20);
                                        HexTile heightTile = nearby[Random.Range(0, nearby.Count)];
                                        bool extra = false;// tectonicIterations > primaryDetailIterations - 2;
                                        ChangeTileHeight(heightTile, extra ? 1 : Random.Range(1, 6), true, extra ? new Vector2Int(0, 2) : new Vector2Int(1, 2), extra ? 5 : 100);
                                    }
                                }
                                else if (plate.plateType == TectonicPlateType.Continental && tileInSpaceForward.Plate.plateType == TectonicPlateType.Oceanic)
                                {
                                    var nearby = tile.GetTileNeighborsInDistance(tileDistanceEffect);
                                    HexTile heightTileOcean = nearby[Random.Range(0, nearby.Count)];
                                    HexTile heightTile = nearby[Random.Range(0, nearby.Count)];
                                    ChangeTileHeight(heightTileOcean, Random.Range(1, 5), false, new Vector2Int(5, 10), 100, TectonicPlateType.Oceanic);
                                    ChangeTileHeight(heightTile, Random.Range(1, 5), true, new Vector2Int(1, 2));
                                }
                                else if (plate.plateType == TectonicPlateType.Oceanic && tileInSpaceForward.Plate.plateType == TectonicPlateType.Oceanic)
                                {
                                    var nearby = tile.GetTileNeighborsInDistance(tileLargeDistanceEffect);
                                    HexTile heightTile = nearby[Random.Range(0, nearby.Count)];
                                    ChangeTileHeight(heightTile, Random.Range(1, 5), false, new Vector2Int(1, 2), 100, TectonicPlateType.Oceanic);

                                    var nearbyVolcano = tile.GetTileNeighborsInDistance(tileLargeDistanceEffect);
                                    HexTile heightVolcanoTile = nearbyVolcano[Random.Range(0, nearbyVolcano.Count)];
                                    int volcanoIterations = Random.Range(volcanoIterationRange.x, volcanoIterationRange.y);
                                    for (int i = 0; i < volcanoIterations; i++)
                                    {
                                        ChangeTileHeight(heightVolcanoTile, Random.Range(1, 4), true, new Vector2Int(1, 2));
                                    }
                                    //if (Random.Range(0, 100) == 0)
                                    //{
                                    //    List<HexTile> nearby = GetTileNeighborsInDistance(tile, 50);
                                    //    HexTile newVolcano = nearby[Random.Range(0, nearby.Count)];
                                    //    if (!extraTerrainMods.ContainsKey(newVolcano) && !claimed.Contains(newVolcano))
                                    //    {
                                    //        extraTerrainMods.Add(newVolcano, new ExtraTerrainMod()
                                    //        {
                                    //            tile = newVolcano,
                                    //            generalHeightRange = new Vector2Int(1, 5),
                                    //            heightFalloutRange = new Vector2Int(2, 12),
                                    //            heightIncrease = true
                                    //        });
                                    //    }
                                    //}
                                }
                            }
                            else
                            {
                                //if(plate.plateType == TectonicPlateType.Continental && tileInSpaceForward.Plate.plateType == TectonicPlateType.Continental) //if (Random.Range(0, 50) == 0)
                                {
                                    var nearby = tile.GetTileNeighborsInDistance(tileLargeDistanceEffect);
                                    HexTile newVolcano = nearby[Random.Range(0, nearby.Count)];
                                    for (int i = 0; i < 1; i++)
                                    {
                                        ChangeTileHeight(newVolcano, Random.Range(1, 3), true, tectonicIterations < 5 ? new Vector2Int(0, 2) : new Vector2Int(1, 2), 5);
                                    }
                                }
                            }
                        }
                    }

                    HexCoordinates backwardMovedDirection = tile.Coordinates - plate.movementDirection;
                    HexTile tileInSpaceBackward = GetTileFromCoordinate(backwardMovedDirection);

                    if (tileInSpaceBackward != null)
                    {
                        //The behind movement direction is running into another plate
                        if (tileInSpaceBackward.Plate != plate)
                        {
                            if (HexCoordinates.AreOpposites(plate.movementDirection, tileInSpaceBackward.Plate.movementDirection))
                            {
                                var nearby = tile.GetTileNeighborsInDistance(tileDistanceEffect);
                                HexTile heightTile = nearby[Random.Range(0, nearby.Count)];
                                ChangeTileHeight(heightTile, Random.Range(1, 5), false, new Vector2Int(1, 2));
                            }
                        }
                    }
                }

            }

            if(tectonicIterations != 0 && tectonicIterations % 10 == 0)
            {
                tileDistanceEffect += tileDistanceIncrease;
                tileLargeDistanceEffect += tileDistanceIncrease;
                tileDistanceIncrease += exponentialIncrease;
            }

            foreach (var tile in tempTileHeights.Keys)
            {
                tile.SetHeight(tempTileHeights[tile]);
            }
            yield return RefreshForTest(0.01f);
        }



        yield return RefreshForTest(5f);

        #region Erosion
        //Erosion
        int erosionIterations = 10;
        for (int erosion = 0; erosion < erosionIterations; erosion++)
        {
            int rainDroplets = Random.Range(globalTiles.Length / 5000, globalTiles.Length / 1000);
            for (int rain = 0; rain < rainDroplets; rain++)
            {
                var options = GetTileFromCoordinate(new HexCoordinates(139, 204)).GetTileNeighborsInDistance(20);
                HexTile tileRainHits = null;// GetTileFromCoordinate(new HexCoordinates(139, 204));//null;
                while(tileRainHits == null)
                {
                    tileRainHits = options[Random.Range(0, options.Count)];
                }

                Droplet currentDroplet = new Droplet()
                {
                    position = tileRainHits,
                    speed = 100f,
                    sediment = 0,
                    water = 50f,
                    direction = new HexCoordinates(0, 0)
                };

                HexTile startingTileDirection = null;
                {
                    int lowestHeight = int.MaxValue;
                    var neighboringTiles = currentDroplet.position.Neighbors;
                    for (int i = 0; i < neighboringTiles.Count; i++)
                    {
                        if (neighboringTiles[i].Height < lowestHeight)
                        {
                            lowestHeight = neighboringTiles[i].Height;
                            startingTileDirection = neighboringTiles[i];
                        }
                    }
                }
                currentDroplet.direction = startingTileDirection.Coordinates - tileRainHits.Coordinates;

                List<HexTile> path = new List<HexTile>();

                for (int dropletLifetime = 0; dropletLifetime < 30; dropletLifetime++)
                {
                    path.Add(currentDroplet.position);

                    //Calculate where droplet is moving
                    HexTile goodTileToMove = GetTileFromCoordinate(currentDroplet.position.Coordinates + currentDroplet.direction);

                    HexTile lowestNear = null;
                    int lowestHeight = goodTileToMove.Height;
                    var neighboringTiles = currentDroplet.position.Neighbors;
                    for (int i = 0; i < neighboringTiles.Count; i++)
                    {
                        if (neighboringTiles[i].Height < lowestHeight)
                        {
                            lowestHeight = neighboringTiles[i].Height;
                            lowestNear = neighboringTiles[i];
                        }
                    }

                    if(lowestNear != null)
                    {
                        if (goodTileToMove.Height - currentDroplet.position.Height > currentDroplet.speed)
                        {
                            goodTileToMove = lowestNear;
                        }
                    }

                    currentDroplet.direction = goodTileToMove.Coordinates - currentDroplet.position.Coordinates;

                    //Update droplet position
                    int heightDiff = currentDroplet.position.Height - goodTileToMove.Height;
                    currentDroplet.position = goodTileToMove;

                    if(currentDroplet.speed <= 0 || currentDroplet.water <= 0)
                    {
                        break;
                    }

                    //Update droplet speed
                    currentDroplet.speed += heightDiff;

                    //Calculate sediment
                    float sedimentCapacity = heightDiff * currentDroplet.speed * currentDroplet.water;
                    
                    if(currentDroplet.sediment > sedimentCapacity || heightDiff < 0)
                    {
                        for (int i = 0; i < neighboringTiles.Count; i++)
                        {
                            currentDroplet.sediment -= currentDroplet.sediment - sedimentCapacity * 0.5f;
                            if(neighboringTiles[i].Height <= currentDroplet.position.Height)
                            {
                                tempTileHeights[neighboringTiles[i]]++;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < neighboringTiles.Count; i++)
                        {
                            //if(neighboringTiles[i].Height < currentDroplet.position.Height)
                            {
                                tempTileHeights[neighboringTiles[i]]--;// -= Mathf.Abs(heightDiff) / 10;
                                currentDroplet.sediment += Mathf.Abs(heightDiff) * 10;
                            }
                        }
                    }

                    currentDroplet.speed = Mathf.Sqrt(currentDroplet.speed * currentDroplet.speed + heightDiff);
                    currentDroplet.water--;
                }

                Debug.DrawLine(tileRainHits.Position, tileRainHits.Position + new Vector3(0, 50), Color.blue, 60);
                
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Debug.DrawLine(path[i].Position + new Vector3(0, path[i].Height * HexTile.HEIGHT_STEP), path[i + 1].Position + new Vector3(0, path[i + 1].Height * HexTile.HEIGHT_STEP), Color.blue, 60);
                }

                foreach (var tile in tempTileHeights.Keys)
                {
                    tile.SetHeight(tempTileHeights[tile]);
                }
                yield return RefreshForTest(0.01f, false);

            }
            yield return RefreshForTest(0.1f, false);
        }
        #endregion


        yield break;

        void ChangeTileHeight(HexTile tileToEdit, int amount, bool positive, Vector2Int randomizedFalloff, int iterationsUntilFalloffIncreases = 100, TectonicPlateType? biasType = null, int currentIterations = 0)
        {
            if(amount <= 0 || (biasType != null && tileToEdit.Plate.plateType != biasType.Value))
            {
                return;
            }

            if(currentIterations == iterationsUntilFalloffIncreases)
            {
                randomizedFalloff = new Vector2Int(randomizedFalloff.x + 1, randomizedFalloff.y + 1);
            }

            if(positive)
            {
                tempTileHeights[tileToEdit] += amount;
                var tileNeighbors = tileToEdit.Neighbors;
                for (int i = 0; i < tileNeighbors.Count; i++)
                {
                    if(tempTileHeights[tileNeighbors[i]] < tempTileHeights[tileToEdit])
                    {
                        int heightDiff = tempTileHeights[tileToEdit] - tempTileHeights[tileNeighbors[i]];
                        ChangeTileHeight(tileNeighbors[i], amount - Random.Range(randomizedFalloff.x, randomizedFalloff.y), positive, randomizedFalloff, iterationsUntilFalloffIncreases, biasType, currentIterations + 1);
                        //ChangeTileHeight(tileNeighbors[i], heightDiff - (amount + Random.Range(heightAdjustments.x, heightAdjustments.y)), positive, heightAdjustments, biasType);
                    }
                }
            }
            else
            {
                tempTileHeights[tileToEdit] -= amount;
                var tileNeighbors = tileToEdit.Neighbors;
                for (int i = 0; i < tileNeighbors.Count; i++)
                {
                    if (tempTileHeights[tileNeighbors[i]] > tempTileHeights[tileToEdit])
                    {
                        //Debug.DrawLine(tileNeighbors[i].Position, tileNeighbors[i].Position + new Vector3(0, 20), Color.blue, 60);
                        int heightDiff = tempTileHeights[tileNeighbors[i]] - tempTileHeights[tileToEdit];
                        ChangeTileHeight(tileNeighbors[i], amount - Random.Range(randomizedFalloff.x, randomizedFalloff.y), positive, randomizedFalloff, iterationsUntilFalloffIncreases, biasType, currentIterations + 1);
                        //ChangeTileHeight(tileNeighbors[i], heightDiff - (amount + Random.Range(heightAdjustments.x, heightAdjustments.y)), positive, heightAdjustments, biasType);
                    }
                }
            }
        }

        IEnumerator RefreshForTest(float time = 1, bool t = true)
        {
            for (int i = 0; i < globalTiles.Length; i++)
            {
                if (globalTiles[i] == null)
                {
                    continue;
                }

                if(t)
                    globalTiles[i].MaterialIndex = -1;
            }
            for (int i = 0; i < allBoards.GetLength(0); i++)
            {
                for (int j = 0; j < allBoards.GetLength(1); j++)
                {
                    AddBoardToRender(allBoards[i, j]);
                }
            }
            yield return new WaitForSeconds(time);
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

        HexBoard board = new HexBoard();// Instantiate(hexBoardPrefab, transform);
        //board.name = "HexBoard [" + x + ", " + y + "]";
        board.GridPosition = new Vector2Int(x, y);
        board.tileSize = tileSize;
        board.Init();
        //board.transform.localPosition = Vector3.zero;

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

        board.CreateAllTiles();
        //board.RunTerrainGeneration(cornerRect, biome);

        allBoards[x, y] = board;

        return board;
    }

    private int GetCornerHeight(Biome biome)
    {
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
        List<Biome> grabBag = new List<Biome>();
        grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Plains, 6));
        grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Hills, 3));
        grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Ocean, 5));
        grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Mountains, 1));
        grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Forest, 6));

        return grabBag[Random.Range(0, grabBag.Count)];
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

    public HexTile FetchTile(HexCoordinates coords) {
        int idx = HexCoordToGlobalIndex(coords);
        var tile = globalTiles[idx];
        if (tile == null) {
            Debug.LogError($"Null tile found for coords {coords}, index {idx}");
        }
        return tile;
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
        if(globalTiles == null || index >= globalTiles.Length || index < 0)
        {
            return null;
        }
        return globalTiles[index];
    }

    public List<HexTile> GetTileNeighbors_Uncached(HexTile tile) 
    {
        var index = tile.GlobalIndex;
        var offset = tile.Coordinates.Y % 2 == 0 ? -1 : 1;
        var neighbors = new List<HexTile>(6);

        //TODO wrap arounds?
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
                if(lookupTile != null)
                {
                    neighbors.Add(lookupTile);
                }
            }
        }
    }

    public IReadOnlyList<HexTile> GetTileNeighborsInDistance(HexTile tile, int distance) {
        return tile.GetTileNeighborsInDistance(distance);
    }

    public List<HexTile> GetTileNeighborsInDistance_Uncached(HexTile tile, int distance)
    {
        var allTiles = new List<HexTile> { tile };
        for (int i = -distance; i <= distance; i++) {
            for (int j = Mathf.Max(-distance, -i - distance); j <= Mathf.Min(distance, -i + distance); j++) {
                if (i == 0 && j == 0) {
                    continue;
                }
                var neighbor = GetTileInDirection(tile, i, j);
                if (neighbor != null) {
                    allTiles.Add(neighbor);
                }
            }
        }
        
        return allTiles;
    }

    public HexTile GetTileInDirection(HexTile tile, int directionX, int directionY)
    {
        HexCoordinates changedCoords = new HexCoordinates(tile.Coordinates.X + directionX, tile.Coordinates.Y + directionY);
        return GetTileFromCoordinate(changedCoords);
    }
    
    public IEnumerable<HexBoard> GetTileNeighborBoards(HexTile tile)
    {
        List<HexBoard> neighborBoards = new List<HexBoard>();
        var otherTiles = tile.Neighbors;

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

    private void OnDisable()
    {
        foreach(var board in globalBoards.Values)
        {
            Material mat = board.OnDisable();
            if(mat != null)
            {
                Destroy(mat);
            }
        }
    }
}
