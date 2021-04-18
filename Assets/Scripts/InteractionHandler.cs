using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class InteractionHandler : MonoBehaviour
{
    private const float timeBetweenHoldings = 0.1f;
    private const float timeBeforeNotCancelling = 0.2f;

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

    //private PreviewTile previewTile;

    private MasterInput inputMaster;
    private Camera cameraInstance;

    private PlaceableGO placeablePreview;
    private PlaceableGO placeablePreviewPrefab;
    private GenerateHexagonHandler generateHexagonHandler;
    private Mesh hoverMesh;
    private GameObject hoverDisplay;
    private float holdTimer = 0;
    private System.Action onPlaceablePlaced;
    private int multiSelectionsLeft = 0;
    private List<HexTile> multiSelections = new List<HexTile>();
    private System.Action<HexTile[], int> onMultiselectComplete;
    private HexagonPreviewArea.PreviewRenderData multiSelectPreview;
    private int multiselectHeightMod = 0;
    private bool multiSelectCanChangeHeight = true;
    private HexTile lastHoveredTile;
    private List<GameObject> toCombine = new List<GameObject>();
    private bool forceHoverUpdate = false;
    private bool destructionModeActive = false;
    private MeshRenderer hoverMeshRenderer;
    private float cancelTimer = 0;
    private RenderTexture selectionRenderTexture;
    private Texture2D hexSelectTex;
    private HexCoordinates currentCoordinates;


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
        hexSelectTex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);


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

    private void OnDestroy()
    {
        Destroy(hexSelectTex);
        Destroy(selectionRenderTexture);
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
        multiSelectPreview = HexagonPreviewArea.CreateUniqueReference();// Instantiate(terrainModPreviewPrefab);
    }

    public void DisplayPlaceablePreview(PlaceableGO placeablePrefab, string prefabName, System.Action onPlaced = null)
    {
        if(placeablePreview != null)
        {
            Destroy(placeablePreview.gameObject);
        }
        placeablePreviewPrefab = placeablePrefab;
        placeablePreview = Instantiate(placeablePrefab, hoverObject.transform);
        placeablePreview.Get().PlaceableName = prefabName;
        placeablePreview.transform.localPosition = Vector3.zero;
        if(onPlaced != null)
        {
            placeablePreview.Get().OnPlaced += onPlaced;
        }
        forceHoverUpdate = true;
        //hoverDisplay.transform.localScale = new Vector3(placeablePreview.Size, placeablePreview.Size, placeablePreview.Size);
    }

    private void CancelPlaceable()
    {
        if(placeablePreview != null)
        {
            placeablePreview.Get().CancelPlacing();
            Destroy(placeablePreview.gameObject);
            placeablePreview = null;
            hoverDisplay.transform.localScale = Vector3.one;
            hoverDisplay.transform.localPosition = Vector3.zero;
        }
    }

    private void OnGPUReadback(AsyncGPUReadbackRequest request)
    {
        NativeArray<Color> pixel = request.GetData<Color>();
        Color c = pixel[0];
        Vector2Int v;
        unsafe
        {
            v = *(Vector2Int*)&c;
        }

        currentCoordinates = new HexCoordinates(v.x, v.y);
    }

    private Peeple selectedPeep;

    private void Update()
    {
        if (eventSystem.currentInputModule.IsPointerOverGameObject(-1))
        {
            return;
        }
        selectionCamera.targetTexture = selectionRenderTexture;
        RenderTexture.active = selectionRenderTexture;

        Vector2Int mouse = Vector2Int.FloorToInt(Mouse.current.position.ReadValue());
        mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        mouse.y = Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        //mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        //mouse.y = (Screen.height - 1) - Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        var request = AsyncGPUReadback.Request(selectionRenderTexture, 0, mouse.x, 1, mouse.y, 1, 0, 1, OnGPUReadback);

        HexTile currentTile = HexBoardChunkHandler.Instance.GetTileFromCoordinate(currentCoordinates);

        Hover(currentTile);

        holdTimer -= Time.unscaledDeltaTime;

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
                placeablePreview.Get().Rotate(-1);
            }
            if(Keyboard.current.eKey.wasPressedThisFrame)
            {
                placeablePreview.Get().Rotate(1);
            }
            if(Keyboard.current.pageUpKey.isPressed && holdTimer < 0)
            {
                placeablePreview.Get().ModifiedHeight++;
                hoverDisplay.transform.localPosition = placeablePreview.transform.localPosition;
                holdTimer = timeBetweenHoldings;
            }
            if(Keyboard.current.pageDownKey.isPressed && holdTimer < 0)
            {
                placeablePreview.Get().ModifiedHeight--;
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
                //multiSelectRenderer.sharedMaterial = badShape ? badTerrainMaterial : goodTerrainMaterial;

                if(potentialSelections[potentialSelections.Count - 2] != potentialSelections[potentialSelections.Count - 1])
                {
                    HexagonPreviewArea.DisplayArea(multiSelectPreview, HexagonSelectionHandler.Instance.GetBorderBetweenTiles(potentialSelections.ToArray()), multiSelectCanChangeHeight ? potentialSelections[0].Height + multiselectHeightMod : -500, badShape);

                    //HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetBorderBetweenTiles(potentialSelections.ToArray()), multiSelectCanChangeHeight ? potentialSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);

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
                bool placeableGood = placeablePreview.Get().CanPlaceHere(tile);
                placeablePreview.Get().SetPreviewMaterial(placeableGood);
                placeablePreview.Get().Hover(tile);
            }
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
        HexagonPreviewArea.StopDisplay(multiSelectPreview);
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
        HexagonPreviewArea.StopDisplay(multiSelectPreview);
        multiSelectPreview = null;
    }

    private void UpdateMultiSelectPreview(bool includePreview = true)
    {
        List<HexTile> toPreview = new List<HexTile>(multiSelections);
        if(includePreview)
        {
            toPreview.Add(lastHoveredTile);
        }
        HexagonPreviewArea.DisplayArea(multiSelectPreview, HexagonSelectionHandler.Instance.GetBorderBetweenTiles(toPreview.ToArray()), multiSelectCanChangeHeight ? multiSelections[0].Height + multiselectHeightMod : -500);
        //HexagonPreviewArea.AddAreaToDisplay(HexagonSelectionHandler.Instance.GetBorderBetweenTiles(toPreview.ToArray()), multiSelectCanChangeHeight ? multiSelections[0].Height + multiselectHeightMod : -500, multiSelectPreview, null);
    }

    private void Select(HexTile tile)
    {
        if(tile != null)
        {
            if(selectedPeep == null)
            {
                Peeple[] allPeeps = PeepleHandler.Instance.GetPeepleOnTiles(new HexTile[] { tile });
                if(allPeeps.Length > 0)
                {
                    selectedPeep = allPeeps[0];
                }
            }
            else
            {
                selectedPeep.Movement.SetGoal(tile);
                selectedPeep = null;
            }

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
                if(placeablePreview.Get().Place(tile.Coordinates.ToPosition() + new Vector3(0, tile.Height * HexTile.HEIGHT_STEP), tile))
                {
                    toCombine.Add(placeablePreview.gameObject);
                    placeablePreview.Get().OnWorkFinished += (success) =>
                    {
                        meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                        meshCombiner.CombineAll();
                    };
                    placeablePreview.Get().OnDestroyed += (go) =>
                    {
                        toCombine.Remove(((Placeable)go).PhysicalRepresentation);
                        meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                        meshCombiner.CombineAll();
                    };
                    meshCombiner.searchOptions.parentGOs = toCombine.ToArray();
                    meshCombiner.CombineAll();

                    string placeableName = placeablePreview.Get().PlaceableName;
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
