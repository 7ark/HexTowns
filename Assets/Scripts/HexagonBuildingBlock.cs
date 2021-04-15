using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonBuildingBlock : MonoBehaviour
{
    [SerializeField]
    private Material previewMaterial;
    [SerializeField]
    private GameObject floor;
    [SerializeField]
    private GameObject top;
    [SerializeField]
    private HexagonWallBuildingBlock wallBuildingBlockPrefab;

    public HexCoordinates Coordinates { get; set; }
    private MeshRenderer[] meshRenderers;
    private List<Material[]> originalMaterials = new List<Material[]>();
    private HexCoordinates[] currentKnownNeighbors;
    private HexagonWallBuildingBlock[] walls;
    private List<ResourceCount> cost = new List<ResourceCount>();

    private void Awake()
    {
        walls = new HexagonWallBuildingBlock[6];
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i] = Instantiate(wallBuildingBlockPrefab, transform);
            walls[i].transform.localPosition = Vector3.zero;
        }
        float currentRotation = 0;
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].transform.localRotation = Quaternion.Euler(new Vector3(0, currentRotation, 0));
            currentRotation += 60;
        }
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].SetColliderActive(false);
        }

        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        SetRendererMaterials(true, true);
    }

    public void AddAssociatedResourceCost(ResourceCount resources)
    {
        cost.Add(resources);
    }

    public List<ResourceCount> GetResourceCost()
    {
        return cost;
    }

    public bool HasDoor()
    {
        for (int i = 0; i < walls.Length; i++)
        {
            if(walls[i].gameObject.activeSelf && walls[i].currentWallType == WallStructureType.Door)
            {
                return true;
            }
        }

        return false;
    }

    public BuildingHexagon GenerateHexagon()
    {
        List<WallStructureType> buildingHexWalls = new List<WallStructureType>();
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X + 1, Coordinates.Y))].currentWallType);
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X + 1, Coordinates.Y - 1))].currentWallType);
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X, Coordinates.Y - 1))].currentWallType);
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X - 1, Coordinates.Y))].currentWallType);
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X - 1, Coordinates.Y + 1))].currentWallType);
        buildingHexWalls.Add(walls[GetIndexOfNeighbor(new HexCoordinates(Coordinates.X, Coordinates.Y + 1))].currentWallType);

        return new BuildingHexagon()
        {
            walls = buildingHexWalls.ToArray()
        };
    }

    public void SetupForPrefab()
    {
        //SetRendererMaterials(false);
        top.SetActive(false);

        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].SetupForPrefab();
            Destroy(walls[i]);
        }
    }

    public void Place(bool fast = false)
    {
        top.SetActive(false);
        SetRendererMaterials(false);

        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        iTween.ScaleTo(gameObject, iTween.Hash("scale", originalScale, "time", 0.3f, "easetype", iTween.EaseType.easeOutBounce));
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].SetColliderActive(true);
        }
    }

    private void SetRendererMaterials(bool setToPreview, bool saveOriginals = false)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Material[] originals = new Material[meshRenderers[i].sharedMaterials.Length];
            Material[] mats = new Material[meshRenderers[i].sharedMaterials.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                originals[j] = meshRenderers[i].sharedMaterials[j];
                mats[j] = setToPreview ? previewMaterial : originalMaterials[i][j];
            }
            if(saveOriginals)
            {
                originalMaterials.Add(originals);
            }
            meshRenderers[i].sharedMaterials = mats;
        }
    }

    private int GetIndexOfNeighbor(HexCoordinates neighbor)
    {
        int sideIndex = -1;
        int xDiff = neighbor.X - Coordinates.X;
        int yDiff = neighbor.Y - Coordinates.Y;

        if (xDiff == 0)
        {
            if (yDiff == 1)
            {
                sideIndex = 0;
            }
            else if (yDiff == -1)
            {
                sideIndex = 3;
            }
        }
        if (yDiff == 0)
        {
            if (xDiff == 1)
            {
                sideIndex = 1;
            }
            else if (xDiff == -1)
            {
                sideIndex = 4;
            }
        }

        if (xDiff == 1 && yDiff == -1)
        {
            sideIndex = 2;
        }
        else if (xDiff == -1 && yDiff == 1)
        {
            sideIndex = 5;
        }

        return sideIndex;
    }

    public void UpdateNeighbors(HexCoordinates[] neighbors)
    {
        if(currentKnownNeighbors == neighbors)
        {
            return;
        }
        currentKnownNeighbors = neighbors;

        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(true);
        }
        for (int i = 0; i < neighbors.Length; i++)
        {
            int sideIndex = GetIndexOfNeighbor(neighbors[i]);

            int leftNeighborIndex = sideIndex - 1;
            if(leftNeighborIndex < 0)
            {
                leftNeighborIndex = 5;
            }
            int rightNeighborIndex = sideIndex + 1;
            if(rightNeighborIndex > 5)
            {
                rightNeighborIndex = 0;
            }
            walls[leftNeighborIndex].SetCornerState(true);
            walls[rightNeighborIndex].SetCornerState(false);

            walls[sideIndex].gameObject.SetActive(false);
        }
    }
}
