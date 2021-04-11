using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InteractionHandler : MonoBehaviour
{
    private const float timeBetweenHoldings = 0.1f;
    private const float timeBeforeNotCancelling = 0.1f;

    [SerializeField]
    private EventSystem eventSystem;
    [SerializeField]
    private HexBoardChunkHandler chunkHandler;
    [SerializeField]
    private GameObject hoverObject;
    [SerializeField]
    private PlacementPrefabHandler placementPrefabHandler;
    [SerializeField]
    private GameObject terrainModPreviewPrefab;
    [SerializeField]
    private Material goodTerrainMaterial;
    [SerializeField]
    private Material badTerrainMaterial;
    [SerializeField]
    private Material hoverPreviewMaterial;
    [SerializeField]
    private Material hoverBadPreviewMaterial;
    [SerializeField]
    private MeshCombineStudio.MeshCombiner meshCombiner;
    [SerializeField]
    private Camera selectionCamera;
    [SerializeField]
    private CameraHexagonTextureRenderer cameraHexagonTexture;

    //private PreviewTile previewTile;

    private MasterInput inputMaster;
    private Camera cameraInstance;

    private Placeable placeablePreview;
    private Placeable placeablePreviewPrefab;
    private GenerateHexagonHandler generateHexagonHandler;
    private Mesh hoverMesh;
    private GameObject hoverDisplay;
    private float holdTimer = 0;
    private System.Action onPlaceablePlaced;
    private int multiSelectionsLeft = 0;
    private List<HexTile> multiSelections = new List<HexTile>();
    private System.Action<HexTile[], int> onMultiselectComplete;
    private GameObject multiSelectPreview;
    private int multiselectHeightMod = 0;
    private bool multiSelectCanChangeHeight = true;
    private MeshRenderer multiSelectRenderer;
    private HexTile lastHoveredTile;
    private List<GameObject> toCombine = new List<GameObject>();
    private bool forceHoverUpdate = false;
    private bool destructionModeActive = false;
    private MeshRenderer hoverMeshRenderer;
    private float cancelTimer = 0;
    private RenderTexture selectionRenderTexture;
    private Texture2D hexSelectTex;


    private void Awake()
    {
        hoverMeshRenderer = hoverObject.GetComponentInChildren<MeshRenderer>();
        generateHexagonHandler = gameObject.AddComponent<GenerateHexagonHandler>();
        cameraInstance = Camera.main;
        inputMaster = new MasterInput();
        inputMaster.Enable();

        MeshFilter hoverFilter = hoverObject.GetComponentInChildren<MeshFilter>();
        hoverMesh = hoverFilter.mesh = new Mesh();
        hoverDisplay = hoverFilter.gameObject;
        selectionRenderTexture = new RenderTexture(Screen.width, Screen.height, 16, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
        selectionCamera.targetTexture = selectionRenderTexture;
        hexSelectTex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        cameraHexagonTexture.SetTexture(hexSelectTex, selectionRenderTexture);


        //previewTile = hoverObject.GetComponent<PreviewTile>();

        TriangulateTileJob job = new TriangulateTileJob()
        {
            position = Vector3.zero,
            vertices = new NativeList<Vector3>(Allocator.TempJob),
            triangles = new NativeList<int>(Allocator.TempJob),
            textureID = 0,
            uvs = new NativeList<Vector2>(Allocator.TempJob),
            uvData = new NativeArray<Rect>(0, Allocator.TempJob),
            height = 0,
            scale = 1,
            neighborArrayCount = 0,
            neighborHeight = new NativeArray<int>(0, Allocator.TempJob),
            neighborPositions = new NativeArray<Vector3>(0, Allocator.TempJob)
        };
        generateHexagonHandler.GenerateHexagon(job, (vertices, triangles, uvs) =>
        {
            hoverMesh.vertices = vertices.ToArray();
            hoverMesh.triangles = triangles.ToArray();
            hoverMesh.RecalculateNormals();
        });
        SetHoverMaterial(true);
    }

    public void SetHoverMaterial(bool good)
    {
        hoverMeshRenderer.sharedMaterial = good ? hoverPreviewMaterial : hoverBadPreviewMaterial;
    }

    public void StartDestruction()
    {
        if(multiSelectionsLeft > 0)
        {
            return;
        }
        CancelPlaceable();

        destructionModeActive = true;
        SetHoverMaterial(false);
    }

    private void SetupMultiSelectObject()
    {
        multiSelectPreview = Instantiate(terrainModPreviewPrefab);
        multiSelectRenderer = multiSelectPreview.GetComponent<MeshRenderer>();
    }

    public void DisplayPlaceablePreview(Placeable placeablePrefab, string prefabName, System.Action onPlaced = null)
    {
        if(placeablePreview != null)
        {
            Destroy(placeablePreview.gameObject);
        }
        placeablePreviewPrefab = placeablePrefab;
        placeablePreview = Instantiate(placeablePrefab, hoverObject.transform);
        placeablePreview.PlaceableName = prefabName;
        placeablePreview.transform.localPosition = Vector3.zero;
        if(onPlaced != null)
        {
            placeablePreview.OnPlaced += onPlaced;
        }
        forceHoverUpdate = true;
        //hoverDisplay.transform.localScale = new Vector3(placeablePreview.Size, placeablePreview.Size, placeablePreview.Size);
    }

    private void CancelPlaceable()
    {
        if(placeablePreview != null)
        {
            Destroy(placeablePreview.gameObject);
            placeablePreview = null;
            hoverDisplay.transform.localScale = Vector3.one;
            hoverDisplay.transform.localPosition = Vector3.zero;
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RenderTexture.active = selectionRenderTexture;

        Vector2Int mouse = Vector2Int.FloorToInt(Mouse.current.position.ReadValue());
        mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        mouse.y = (Screen.height - 1) - Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        Vector2 max = mouse + Vector2.one;
        hexSelectTex.ReadPixels(Rect.MinMaxRect(mouse.x, mouse.y, max.x, max.y), 0, 0);
    }

    private void Update()
    {
        if (eventSystem.currentInputModule.IsPointerOverGameObject(-1))
        {
            return;
        }
        //RenderTexture.active = selectionRenderTexture;
        //
        //Vector2Int mouse = Vector2Int.FloorToInt(Mouse.current.position.ReadValue());
        //mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        //mouse.y = (Screen.height - 1) - Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        //Vector2 max = mouse + Vector2.one;
        //hexSelectTex.ReadPixels(Rect.MinMaxRect(mouse.x, mouse.y, max.x, max.y), 0, 0);

        Color c = hexSelectTex.GetPixel(0, 0);
        Vector2Int v;
        unsafe
        {
            v = *(Vector2Int*)&c;
        }

        HexCoordinates currentHoveredCoordinate = new HexCoordinates(v.x, v.y);

        HexTile currentTile = HexBoardChunkHandler.Instance.GetTileFromCoordinate(currentHoveredCoordinate);

        Hover(currentTile);

        holdTimer -= Time.deltaTime;

        bool cancellingAction = false;
        if(Mouse.current.rightButton.wasPressedThisFrame)
        {
            cancelTimer = timeBeforeNotCancelling;
        }
        if(cancelTimer > 0)
        {
            cancelTimer -= Time.deltaTime;
        }
        if(Mouse.current.rightButton.wasReleasedThisFrame)
        {
            if(cancelTimer > 0)
            {
                cancellingAction = true;
            }
        }

        if(placeablePreview != null)
        {
            if(Keyboard.current.qKey.wasPressedThisFrame)
            {
                placeablePreview.Rotate(-1);
            }
            if(Keyboard.current.eKey.wasPressedThisFrame)
            {
                placeablePreview.Rotate(1);
            }
            if(Keyboard.current.pageUpKey.isPressed && holdTimer < 0)
            {
                placeablePreview.ModifiedHeight++;
                hoverDisplay.transform.localPosition = placeablePreview.transform.localPosition;
                holdTimer = timeBetweenHoldings;
            }
            if(Keyboard.current.pageDownKey.isPressed && holdTimer < 0)
            {
                placeablePreview.ModifiedHeight--;
                hoverDisplay.transform.localPosition = placeablePreview.transform.localPosition;
                holdTimer = timeBetweenHoldings;
            }
            if(Keyboard.current.escapeKey.wasPressedThisFrame || cancellingAction)
            {
                CancelPlaceable();
            }
        }
        if(multiSelectionsLeft > 0)
        {
            if(multiSelectCanChangeHeight)
            {
                if (Keyboard.current.pageUpKey.isPressed && holdTimer < 0)
                {
                    multiselectHeightMod++;
                    holdTimer = timeBetweenHoldings;
                    UpdateMultiSelectPreview();
                }
                if (Keyboard.current.pageDownKey.isPressed && holdTimer < 0)
                {
                    multiselectHeightMod--;
                    holdTimer = timeBetweenHoldings;
                    UpdateMultiSelectPreview();
                }
            }
            if(Keyboard.current.enterKey.wasPressedThisFrame)
            {
                AddTileToMultiselect(lastHoveredTile);
                EndMultiSelect();
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame || cancellingAction)
            {
                CancelMultiSelect();
            }
        }
        if(destructionModeActive)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || cancellingAction)
            {
                destructionModeActive = false;
                SetHoverMaterial(true);
            }
        }

        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            Select(currentTile);
        }
    }

    private void Hover(HexTile tile)
    {
        if(tile != null && (tile != lastHoveredTile || forceHoverUpdate))
        {
            forceHoverUpdate = false;
            lastHoveredTile = tile;
            hoverObject.transform.position = tile.Coordinates.ToPosition() + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP + 0.1f);
            //previewTile.tile = tile;
            if(multiSelectionsLeft > 0 && multiSelections.Count > 0)
            {
                List<HexTile> potentialSelections = new List<HexTile>(multiSelections);
                potentialSelections.Add(tile);

                bool badShape = HexagonSelectionHandler.Instance.DoBordersCross(potentialSelections.ToArray());
                multiSelectRenderer.sharedMaterial = badShape ? badTerrainMaterial : goodTerrainMaterial;

                if(potentialSelections[potentialSelections.Count - 2] != potentialSelections[potentialSelections.Count - 1])
                {
                    HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetBorderBetweenTiles(potentialSelections.ToArray()), multiSelectCanChangeHeight ? potentialSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);

                    //if (badShape)
                    //{
                    //    HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetBorderBetweenTiles(potentialSelections.ToArray()), multiSelectCanChangeHeight ? potentialSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);
                    //}
                    //else
                    //{
                    //    HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetFilledAreaBetweenTiles(potentialSelections.ToArray()), multiSelectCanChangeHeight ? potentialSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);
                    //}
                }
            }
            if (placeablePreview != null)
            {
                bool placeableGood = placeablePreview.CanPlaceHere(tile);
                placeablePreview.SetPreviewMaterial(placeableGood);
                placeablePreview.Hover(tile);
            }
        }
    }

    private void LateUpdate()
    {
        if(multiSelectionsLeft > 0)
        {
            HexagonPreviewArea.DisplayArea(multiSelectPreview, generateHexagonHandler);
        }
    }

    public bool StartMultiSelection(int pointsToSelect, bool selectHeight, System.Action<HexTile[], int> onComplete)
    {
        if(placeablePreview != null || multiSelectionsLeft > 0)
        {
            return false;
        }
        multiSelectCanChangeHeight = selectHeight;
        multiSelectionsLeft = pointsToSelect;
        onMultiselectComplete = onComplete;
        multiselectHeightMod = 0;
        SetupMultiSelectObject();

        return true;
    }

    private void CancelMultiSelect()
    {
        multiSelectionsLeft = 0;
        onMultiselectComplete = null;
        multiSelections.Clear();
        Destroy(multiSelectPreview);
        multiSelectRenderer = null;
        multiSelectPreview = null;
    }

    private void AddTileToMultiselect(HexTile tile)
    {
        List<HexTile> potentialSelections = new List<HexTile>(multiSelections);
        potentialSelections.Add(tile);
   
        multiSelections.Add(tile);
        multiSelectionsLeft--;

        if (multiSelections.Count > 1)
        {
            UpdateMultiSelectPreview(false);
        }

        if (multiSelectionsLeft <= 0)
        {
            EndMultiSelect();
        }
    }

    private void EndMultiSelect()
    {
        if (HexagonSelectionHandler.Instance.DoBordersCross(multiSelections.ToArray()))
        {
            CancelMultiSelect();
            return;
        }
        multiSelectionsLeft = 0;
        onMultiselectComplete?.Invoke(multiSelections.ToArray(), multiSelections[0].Height + multiselectHeightMod);
        multiSelections.Clear();
        multiSelectPreview.SetActive(false);
    }

    private void UpdateMultiSelectPreview(bool includePreview = true)
    {
        List<HexTile> toPreview = new List<HexTile>(multiSelections);
        if(includePreview)
        {
            toPreview.Add(lastHoveredTile);
        }
        HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetBorderBetweenTiles(toPreview.ToArray()), multiSelectCanChangeHeight ? multiSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);
    }

    private void Select(HexTile tile)
    {
        if(tile != null)
        {
            if(multiSelectionsLeft > 0)
            {
                //Check to make sure this wouldnt intersect
                if(multiSelections.Contains(tile))
                {
                    multiSelectionsLeft = 0;
                    EndMultiSelect();
                }
                else if(!multiSelections.Contains(tile))
                {
                    AddTileToMultiselect(tile);
                }
            }
            else if (placeablePreview != null)
            {
                if(placeablePreview.Place(tile.Coordinates.ToPosition() + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP), tile))
                {
                    toCombine.Add(placeablePreview.gameObject);
                    placeablePreview.OnBuilt += () =>
                    {
                        meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                        meshCombiner.CombineAll();
                    };
                    placeablePreview.OnDestroyed += (go) =>
                    {
                        toCombine.Remove(go);
                        meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                        meshCombiner.CombineAll();
                    };
                    meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                    meshCombiner.CombineAll();

                    string placeableName = placeablePreview.PlaceableName;
                    placeablePreview.transform.SetParent(null);
                    placeablePreview = null;
                    hoverDisplay.transform.localScale = Vector3.one;
                    hoverDisplay.transform.localPosition = Vector3.zero;

                    if(Keyboard.current.shiftKey.isPressed)
                    {
                        placementPrefabHandler.StartPlacingItem(placeableName);
                    }
                }
            }
            else if(destructionModeActive)
            {
                if(tile.HasWorkables)
                {
                    tile.GetWorkable().CancelWork();
                }
            }
            else
            {
                //tempMover.SetGoal(tile);

                //List<HexTile> tiles = new List<HexTile>()
                //{
                //    tile
                //};
                //tiles.AddRange(board.GetTileNeighbors(tile));

                //TerrainModificationHandler.Instance.RequestTerrainModification(tiles.ToArray(), tile.Height);
                //board.ChunkHandler.FlattenArea(tiles.ToArray(), tile.Height);
            }
            //AdjustHeightOfTile(board, tile, tile.Height + 1);
        }

    }
}
