using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildingModeHandler : MonoBehaviour
{
    public static readonly Vector3 YOffset = new Vector3(0, 1000);

    [SerializeField]
    private EventSystem eventSystem;
    [SerializeField]
    private GameObject[] worldObjects;
    [SerializeField]
    private GameObject[] buildingModeObjects;
    [SerializeField]
    private CodeRoadOne.CRO_Camera builderModeCamera;
    [SerializeField]
    private CodeRoadOne.CRO_Camera normalCamera;
    [SerializeField]
    private ResourceHandler resourceHandler;
    [SerializeField]
    private PlacementPrefabHandler placementHandler;
    [SerializeField]
    private HexagonBuildingBlock hexagonPrefab;
    [SerializeField]
    private Placeable placeablePrefab;
    [SerializeField]
    private Transform prefabParent;

    private HexagonBuildingBlock currentPrefabInstance;
    private List<HexagonBuildingBlock> allBuildingBlocks = new List<HexagonBuildingBlock>();
    private Dictionary<string, Placeable> madeBuildings = new Dictionary<string, Placeable>();
    Dictionary<ResourceType, int> resourceCostTotal = new Dictionary<ResourceType, int>();
    private HexagonWallBuildingBlock highlightedWall = null;
    private int tempBuildingCount = 1;

    public bool Active { get; private set; }

    private void Awake()
    {
        SetBuildingMode(false);
    }

    public void ToggleBuildingMode()
    {
        SetBuildingMode(!Active);
    }

    private void SetBuildingMode(bool active)
    {
        for (int i = 0; i < worldObjects.Length; i++)
        {
            worldObjects[i].SetActive(!active);
        }
        for (int i = 0; i < buildingModeObjects.Length; i++)
        {
            buildingModeObjects[i].SetActive(active);
        }

        if(active && !Active)
        {
            if(resourceCostTotal.Count == 0)
            {
                for (int i = 0; i < ResourceHandler.Instance.AllResources.Length; i++)
                {
                    resourceCostTotal.Add(ResourceHandler.Instance.AllResources[i], 0);
                }
            }
            else
            {
                resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
            }
            if(currentPrefabInstance != null)
            {
                Destroy(currentPrefabInstance.gameObject);
            }
            currentPrefabInstance = Instantiate(hexagonPrefab);
            //builderModeCamera.transform.position = new Vector3(0, 600);
            normalCamera.enabled = false;
            builderModeCamera.SetTargetedPosition(YOffset);
            builderModeCamera.GetStandardInput().RefreshCheckForMouse();
        }
        else if(Active && !active)
        {
            Destroy(currentPrefabInstance.gameObject);
            Vector3 pos = normalCamera.GetTargetedPosition();
            pos.y = 0;
            normalCamera.enabled = true;
            normalCamera.SetTargetedPosition(pos);
            normalCamera.GetStandardInput().RefreshCheckForMouse();
            placementHandler.UpdateButtons();
            resourceHandler.UpdateDisplayImages();
        }

        Active = active;
    }

    private bool AnyOthersHere(HexCoordinates coords)
    {
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            if(allBuildingBlocks[i].Coordinates == coords)
            {
                return true;
            }
        }

        return false;
    }

    public void SaveBuilding(string name)
    {
        int doorCount = 0;
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            if(allBuildingBlocks[i].HasDoor())
            {
                doorCount++;
            }
        }

        if(doorCount == 0)
        {
            return;
        }

        name = "DefaultTestingBuilding" + tempBuildingCount++;

        Placeable building = Instantiate(placeablePrefab, prefabParent);
        GameObject combined = new GameObject("CombinedBuildingShape");
        Vector3 averaged = Vector3.zero;
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            averaged += allBuildingBlocks[i].transform.position;
        }
        averaged /= allBuildingBlocks.Count;
        combined.transform.position = HexCoordinates.FromPosition(averaged).ToPosition();
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            HexagonBuildingBlock copy = allBuildingBlocks[i];
            copy.SetupForPrefab();
            GameObject copyGO = copy.gameObject;
            Destroy(copy);
            copyGO.transform.position -= YOffset;
            copyGO.transform.SetParent(combined.transform, true);
            building.AddObjectBase(copyGO);
        }
        combined.transform.localPosition = Vector3.zero;
        building.SetupFromScript(new GameObject[] { combined });
        combined.transform.localPosition = Vector3.zero;
        building.SetTotalWorkableSlots(Mathf.Max(1, allBuildingBlocks.Count / 5));
        building.SetWorkSteps(allBuildingBlocks.Count);
        building.transform.position = new Vector3(0, 500) + YOffset;
        allBuildingBlocks.Clear();

        List<ResourceCount> totalCost = new List<ResourceCount>();
        for (int i = 0; i < ResourceHandler.Instance.AllResources.Length; i++)
        {
            totalCost.Add(new ResourceCount()
            {
                ResourceType = ResourceHandler.Instance.AllResources[i],
                Amount = resourceCostTotal[ResourceHandler.Instance.AllResources[i]]
            });
        }

        resourceCostTotal.Clear();
        for (int i = 0; i < ResourceHandler.Instance.AllResources.Length; i++)
        {
            resourceCostTotal.Add(ResourceHandler.Instance.AllResources[i], 0);
        }
        resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
        placementHandler.AddNewPlaceable(name, building, totalCost.ToArray());
    }

    public void ClearBuilding()
    {
        resourceCostTotal.Clear();
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            Destroy(allBuildingBlocks[i].gameObject);
        }
        allBuildingBlocks.Clear();
        for (int i = 0; i < ResourceHandler.Instance.AllResources.Length; i++)
        {
            resourceCostTotal.Add(ResourceHandler.Instance.AllResources[i], 0);
        }
        resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
    }

    private void Update()
    {
        if(Active)
        {
            if (eventSystem.currentInputModule.IsPointerOverGameObject(-1))
            {
                return;
            }

            RaycastHit hit;
            if (Physics.Raycast(builderModeCamera.GetCamera().ScreenPointToRay(Mouse.current.position.ReadValue()), out hit))
            {
                if (hit.transform != null)
                {
                    HexagonWallBuildingBlock wall = hit.transform.GetComponentInParent<HexagonWallBuildingBlock>();

                    if(wall != highlightedWall && highlightedWall != null)
                    {
                        highlightedWall.SetMaterial(false);
                    }
                    highlightedWall = wall;

                    if (wall != null)
                    {
                        highlightedWall.SetMaterial(true);
                        currentPrefabInstance.gameObject.SetActive(false);

                        if(Mouse.current.leftButton.wasPressedThisFrame)
                        {
                            wall.RotateWallType();
                        }
                    }
                    else
                    {
                        HexCoordinates coordinates = HexCoordinates.FromPosition(hit.point);
                        if (AnyOthersHere(coordinates))
                        {
                            currentPrefabInstance.gameObject.SetActive(false);
                        }
                        else
                        {
                            currentPrefabInstance.gameObject.SetActive(true);
                            currentPrefabInstance.transform.position = coordinates.ToPosition() + new Vector3(0, hit.point.y);
                        }
                    }
                }
            }

            if(currentPrefabInstance.gameObject.activeSelf && Mouse.current.leftButton.wasPressedThisFrame)
            {
                currentPrefabInstance.Place();
                //TODO: Replace this with cost based on selected material type
                resourceCostTotal[ResourceType.Wood] += 5;
                resourceCostTotal[ResourceType.Stone] += 2;
                currentPrefabInstance.AddAssociatedResourceCost(new ResourceCount()
                {
                    ResourceType = ResourceType.Wood,
                    Amount = 5
                });
                currentPrefabInstance.AddAssociatedResourceCost(new ResourceCount()
                {
                    ResourceType = ResourceType.Stone,
                    Amount = 2
                });
                resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
                currentPrefabInstance.Coordinates = HexCoordinates.FromPosition(currentPrefabInstance.transform.position);
                allBuildingBlocks.Add(currentPrefabInstance);
                currentPrefabInstance = Instantiate(hexagonPrefab);

                UpdateAllNeighbors();
            }
        }
    }

    public void UpdateAllNeighbors()
    {
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            HexCoordinates[] neighbors = GetNeighbors(allBuildingBlocks[i].Coordinates);
            allBuildingBlocks[i].UpdateNeighbors(neighbors);
        }
    }

    public HexCoordinates[] GetNeighbors(HexCoordinates coords)
    {
        List<HexCoordinates> final = new List<HexCoordinates>();
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            if(HexCoordinates.HexDistance(coords, allBuildingBlocks[i].Coordinates) == 1)
            {
                final.Add(allBuildingBlocks[i].Coordinates);
            }
        }

        return final.ToArray();
    }
}
