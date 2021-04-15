using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Placeable : Workable
{
    [SerializeField]
    private GameObject previewDisplay;
    [SerializeField]
    private GameObject[] visualOptions;
    [SerializeField]
    private Transform visualsParent;
    [SerializeField]
    private float rotationSnapping = 60;
    [SerializeField]
    private Material terrainPreview;
    [SerializeField]
    private Material previewMatGood;
    [SerializeField]
    private Material previewMatBad;

    private int selectionType = 0;
    private GameObject selectedOption;
    protected List<HexTile> homeTiles = new List<HexTile>();
    private MeshRenderer[] previewRenderers;
    public System.Action OnPlaced;
    private HexTile lastHovered;
    private HexagonPreviewArea.PreviewRenderData previewTerrain = null;

    private GameObject associatedPhysicalGameObject;
    private int rotated = 0;

    public string PlaceableName { get; set; }

    private int modHeight = 0;
    public virtual int ModifiedHeight
    {
        get { return modHeight; }
        set
        {
            modHeight = value;
            associatedPhysicalGameObject.transform.localPosition = new Vector3(0, ModifiedHeight * HexTile.HEIGHT_STEP);
            if(lastHovered != null)
            {
                Hover(lastHovered);
            }
        }
    }
    [SerializeField, HideInInspector]
    private List<GameObject> objectsToCheckTilesUnder = new List<GameObject>();
    [SerializeField, HideInInspector]
    private List<BuildingHexagon> hexagonObjectData = new List<BuildingHexagon>();

    public GameObject PhysicalRepresentation { get { return associatedPhysicalGameObject; } }

    public virtual void Init(GameObject associatedPhysicalGameObject)
    {
        this.associatedPhysicalGameObject = associatedPhysicalGameObject;

        if (visualOptions.Length > 0)
        {
            Setup();
        }
    }

    public void AddObjectBase(GameObject obj, BuildingHexagon hex)
    {
        objectsToCheckTilesUnder.Add(obj);
        hexagonObjectData.Add(hex);
    }

    private void Setup()
    {
        selectionType = Random.Range(0, visualOptions.Length);

        selectedOption = visualOptions[selectionType];

        GameObject.Destroy(previewDisplay);
        previewDisplay = GameObject.Instantiate(selectedOption, associatedPhysicalGameObject.transform);
        previewDisplay.SetActive(true);
        previewDisplay.transform.localPosition = selectedOption.transform.localPosition;
        previewDisplay.transform.localScale = selectedOption.transform.localScale;
        MeshRenderer[] rends = previewDisplay.GetComponentsInChildren<MeshRenderer>();
        previewRenderers = rends;
        for (int j = 0; j < previewRenderers.Length; j++)
        {
            List<Material> sharedMats = new List<Material>();
            for (int i = 0; i < previewRenderers[j].sharedMaterials.Length; i++)
            {
                sharedMats.Add(previewMatGood);
            }
            previewRenderers[j].sharedMaterials = sharedMats.ToArray();
        }
    }

    public void SetupFromScript(GameObject[] visuals)
    {
        visualOptions = visuals;
        for (int i = 0; i < visuals.Length; i++)
        {
            visuals[i].transform.SetParent(visualsParent);
            visuals[i].SetActive(false);
        }

        Setup();
    }

    public void Hover(HexTile tileHoveringAt)
    {
        lastHovered = tileHoveringAt;
        List<HexTile> tilesDisplay = new List<HexTile>();
        tilesDisplay.Add(tileHoveringAt);
        for (int i = 0; i < objectsToCheckTilesUnder.Count; i++)
        {
            tilesDisplay.Add(HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(objectsToCheckTilesUnder[i].transform.position)));
        }

        if(previewTerrain == null)
        {
            previewTerrain = HexagonPreviewArea.CreateUniqueReference();
        }
        HexagonPreviewArea.DisplayArea(previewTerrain, tilesDisplay, tileHoveringAt.Height + ModifiedHeight);
        //HexagonPreviewArea.AddAreaToDisplay(tilesDisplay.ToArray(), tileHoveringAt.Height + ModifiedHeight, terrainHeightPreview, null);
    }

    public bool Place(Vector3 position, HexTile tilePlacedOn)
    {
        if(!CanPlaceHere(tilePlacedOn))
        {
            return false;
        }
        associatedPhysicalGameObject.transform.position = position + new Vector3(0, ModifiedHeight * HexTile.HEIGHT_STEP);

        homeTiles.Clear();
        homeTiles.Add(tilePlacedOn);
        for (int i = 0; i < objectsToCheckTilesUnder.Count; i++)
        {
            HexTile tile = HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(objectsToCheckTilesUnder[i].transform.position));
            if(!homeTiles.Contains(tile))
            {
                homeTiles.Add(tile);
            }
        }

        HexagonPreviewArea.StopDisplay(previewTerrain);

        OnPlaced?.Invoke();

        for (int i = 0; i < homeTiles.Count; i++)
        {
            homeTiles[i].AddWorkableToTile(this, tilePlacedOn.Height + ModifiedHeight + 1);
        }

        TerrainModificationHandler.Instance.RequestTerrainModification(homeTiles.ToArray(), tilePlacedOn.Height + ModifiedHeight, onComplete: () =>
        {
            for (int i = 0; i < homeTiles.Count; i++)
            {
                homeTiles[i].HeightLocked = true;
            }

            BeginWorking();
        });

        return true;
    }

    public override void CancelWork()
    {
        base.CancelWork();
        DestroySelf();
    }

    protected override void WorkCompleted(bool completedSuccessfully)
    {
        previewDisplay.SetActive(false);
        visualOptions[selectionType].SetActive(true);

        //Setup building
        Building building = new Building();
        for (int i = 0; i < objectsToCheckTilesUnder.Count; i++)
        {
            HexTile tile = HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(objectsToCheckTilesUnder[i].transform.position));

            hexagonObjectData[i].Rotate(rotated);
            building.AddPiece(tile, hexagonObjectData[i]);
        }

        base.WorkCompleted(completedSuccessfully);
    }

    public bool CanPlaceHere(HexTile tilePlacedOn)
    {
        List<HexTile> otherTiles = new List<HexTile>();
        for (int i = 0; i < objectsToCheckTilesUnder.Count; i++)
        {
            otherTiles.Add(HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(objectsToCheckTilesUnder[i].transform.position)));
        }
        for (int i = 0; i < otherTiles.Count; i++)
        {
            if (otherTiles[i].HeightLocked || otherTiles[i].HasWorkables)
            {
                return false;
            }
        }

        return true;
    }

    public void SetPreviewMaterial(bool good)
    {
        for (int j = 0; j < previewRenderers.Length; j++)
        {
            List<Material> mats = new List<Material>();
            for (int i = 0; i < previewRenderers[j].sharedMaterials.Length; i++)
            {
                mats.Add(good ? previewMatGood : previewMatBad);
            }
            previewRenderers[j].sharedMaterials = mats.ToArray();
        }
    }

    public void ShowPreview()
    {
        previewDisplay.SetActive(true);
    }

    public void Rotate(int amount)
    {
        associatedPhysicalGameObject.transform.Rotate(new Vector3(0, rotationSnapping * -amount));
        rotated += amount;
        if(rotated >= 6)
        {
            rotated = 0;
        }
    }

    public override List<HexTile> GetTilesAssociated()
    {
        return homeTiles;
    }
}
