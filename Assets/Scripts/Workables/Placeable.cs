using MEC;
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
    protected HashSet<HexTile> homeTiles = new HashSet<HexTile>();
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

    public void CancelPlacing()
    {
        HexagonPreviewArea.StopDisplay(previewTerrain);
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

        foreach (var homeTile in homeTiles) {
            homeTile.AddWorkableToTile(this, tilePlacedOn.Height + ModifiedHeight + 1);

        }

        totalWorkSlots = new List<HexTile>(GetWorkableTiles()).Count;

        TerrainModificationHandler.Instance.RequestTerrainModification(homeTiles, tilePlacedOn.Height + ModifiedHeight, onComplete: () =>
        {
            foreach (var homeTile in homeTiles) {
                homeTile.HeightLocked = true;
            }

            BeginWorking();
        });

        return true;
    }

    public override void CancelWork()
    {
        base.CancelWork();
        if(!WorkFinished)
        {
            DestroySelf();
        }
    }

    public override void DestroySelf()
    {
        base.DestroySelf();

        GameObject.Destroy(associatedPhysicalGameObject);
    }

    public override IEnumerator<float> DoWork(Peeple specificPeepleWorking = null)
    {
        HexTile workingTile = specificPeepleWorking.Movement.GetTileOn();
        foreach(var key in resourcesToUse.Keys)
        {
            if(resourcesToUse[key] > ResourceHandler.Instance.PeekResourcesAtLocation(resourcePiles[key]).Resources.Count)
            {
                resourcesNeededToStart[key] += resourcesToUse[key] - ResourceHandler.Instance.PeekResourcesAtLocation(resourcePiles[key]).Resources.Count;
                waitingOnResources = true;
                yield break;
            }

            if(resourcesToUse[key] > 0 && ResourceHandler.Instance.PeekResourcesAtLocation(resourcePiles[key]).Resources.Count > 0)
            {
                if(HexCoordinates.HexDistance(resourcePiles[key].Coordinates, workingTile.Coordinates) > 1)
                {
                    specificPeepleWorking.Movement.SetGoal(resourcePiles[key].Neighbors[Random.Range(0, resourcePiles[key].Neighbors.Count)]);

                    yield return Timing.WaitUntilFalse(() => { return specificPeepleWorking.Movement.IsMoving; });
                }

                resourcesToUse[key]--; //TODO: Figure out wtf to do if somehow between here and there the Peeple gets distracted
                ResourceIndividual resource;
                if(!ResourceHandler.Instance.RetrieveResourceFromTile(key, resourcePiles[key], out resource))
                {
                    resourcesToUse[key]++;
                    yield break;
                }
                specificPeepleWorking.ResourceHolding = resource;

                if (HexCoordinates.HexDistance(resourcePiles[key].Coordinates, workingTile.Coordinates) > 1)
                {
                    specificPeepleWorking.Movement.SetGoal(workingTile);

                    yield return Timing.WaitUntilFalse(() => { return specificPeepleWorking.Movement.IsMoving; });
                }
                else
                {
                    yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
                }

                ResourceHandler.Instance.ConsumeResource(resource);
                specificPeepleWorking.ResourceHeldPlacing();

                yield break;
            }
        }

        WorkCompleted(true);
    }

    protected override void WorkCompleted(bool completedSuccessfully)
    {
        if(completedSuccessfully)
        {
            previewDisplay.SetActive(false);
            visualOptions[selectionType].SetActive(true);

            //Setup building
            Building building = new Building(this);
            for (int i = 0; i < objectsToCheckTilesUnder.Count; i++)
            {
                HexTile tile = HexBoardChunkHandler.Instance.GetTileFromCoordinate(HexCoordinates.FromPosition(objectsToCheckTilesUnder[i].transform.position));

                hexagonObjectData[i].Rotate(rotated);
                building.AddPiece(tile, hexagonObjectData[i]);
                if (hexagonObjectData[i].HasWorkStation && hexagonObjectData[i].WorkStation.RequiresWork)
                {
                    hexagonObjectData[i].WorkStation.Get().BeginWorking();
                }
                if (hexagonObjectData[i].HasWorkStation)
                {
                    hexagonObjectData[i].WorkStation.OnPlaced?.Invoke(tile);
                }
            }

            building.SetupWorkStations();
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
            if (otherTiles[i] == null || otherTiles[i].HeightLocked || otherTiles[i].HasWorkables)
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

    public override HashSet<HexTile> GetTilesAssociated()
    {
        return homeTiles;
    }
}
