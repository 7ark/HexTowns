using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class BuildingModeHandler : MonoBehaviour
{
    public static readonly Vector3 YOffset = new Vector3(0, 1000);
    [System.Serializable]
    private struct ItemData
    {
        public string itemName;
        public JobWorkableGO objectPrefab;
    }

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
    private PlaceableGO placeablePrefab;
    [SerializeField]
    private Transform prefabParent;
    [SerializeField]
    private ItemData[] allItemsAvailable;
    [SerializeField]
    private GameObject namePanel;
    [SerializeField]
    private TMP_InputField nameInputField;

    private HexagonBuildingBlock currentBuildingBlockPrefabInstance;
    private List<HexagonBuildingBlock> allBuildingBlocks = new List<HexagonBuildingBlock>();
    private List<JobWorkableGO> allPlacedItems = new List<JobWorkableGO>();
    private Dictionary<ResourceType, int> resourceCostTotal = new Dictionary<ResourceType, int>();
    private Dictionary<string, JobWorkableGO> placeableItems = new Dictionary<string, JobWorkableGO>();
    private Dictionary<HexCoordinates, JobWorkableGO> placeableItemLocations = new Dictionary<HexCoordinates, JobWorkableGO>();
    private HexagonWallBuildingBlock highlightedWall = null;
    private JobWorkableGO itemPrefabInstance = null;
    private int defaultNamedCount = 1;

    public bool Active { get; private set; }

    private void Awake()
    {
        for (int i = 0; i < allItemsAvailable.Length; i++)
        {
            placeableItems.Add(allItemsAvailable[i].itemName, allItemsAvailable[i].objectPrefab);
        }

        SetBuildingMode(false);
    }

    public void ToggleBuildingMode()
    {
        SetBuildingMode(!Active);
    }

    public void SelectItemToPlace(string item)
    {
        itemPrefabInstance = Instantiate(placeableItems[item]);
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
            if(currentBuildingBlockPrefabInstance != null)
            {
                Destroy(currentBuildingBlockPrefabInstance.gameObject);
            }
            currentBuildingBlockPrefabInstance = Instantiate(hexagonPrefab);
            //builderModeCamera.transform.position = new Vector3(0, 600);
            normalCamera.enabled = false;
            builderModeCamera.SetTargetedPosition(YOffset);
            builderModeCamera.GetStandardInput().RefreshCheckForMouse();
        }
        else if(Active && !active)
        {
            Destroy(currentBuildingBlockPrefabInstance.gameObject);
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

    private bool AnyOthersHere(HexCoordinates coords, out HexagonBuildingBlock buildingBlock)
    {
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            if(allBuildingBlocks[i].Coordinates == coords)
            {
                buildingBlock = allBuildingBlocks[i];
                return true;
            }
        }

        buildingBlock = null;
        return false;
    }

    public void DisplayNamePanel()
    {
        if (GetAmountOfDoors() == 0)
        {
            return;
        }

        namePanel.SetActive(true);
        nameInputField.text = "Default Name #" + defaultNamedCount;
        nameInputField.Select();
    }

    public void ConfirmBuilding()
    {
        if(nameInputField.text != string.Empty)
        {
            SaveBuilding(nameInputField.text);

            if(nameInputField.text == "Default Name #" + defaultNamedCount)
            {
                defaultNamedCount++;
            }
        }
    }

    private int GetAmountOfDoors()
    {
        int doorCount = 0;
        for (int i = 0; i < allBuildingBlocks.Count; i++)
        {
            if (allBuildingBlocks[i].HasDoor())
            {
                doorCount++;
            }
        }

        return doorCount;
    }

    public void SaveBuilding(string name)
    {
        if(GetAmountOfDoors() == 0)
        {
            return;
        }

        PlaceableGO building = Instantiate(placeablePrefab, prefabParent);
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
            BuildingHexagon hex = copy.GenerateHexagon();
            if(placeableItemLocations.ContainsKey(copy.Coordinates))
            {
                hex.SetWorkStation(placeableItemLocations[copy.Coordinates].GetComponent<JobWorkableGO>());
            }
            Destroy(copy);
            copyGO.transform.position -= YOffset;
            copyGO.transform.SetParent(combined.transform, true);
            building.Get().AddObjectBase(copyGO, hex);
        }
        for (int i = 0; i < allPlacedItems.Count; i++)
        {
            JobWorkableGO item = allPlacedItems[i];
            item.transform.position -= YOffset;
            item.transform.SetParent(combined.transform, true);
        }
        combined.transform.localPosition = Vector3.zero;
        building.Get().SetupFromScript(new GameObject[] { combined });
        combined.transform.localPosition = Vector3.zero;
        building.Get().SetTotalWorkableSlots(Mathf.Max(1, allBuildingBlocks.Count));
        building.Get().SetWorkSteps(allBuildingBlocks.Count);
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

        placeableItemLocations.Clear();
        allPlacedItems.Clear();
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

        for (int i = 0; i < allPlacedItems.Count; i++)
        {
            Destroy(allPlacedItems[i].gameObject);
        }
        allPlacedItems.Clear();
        placeableItemLocations.Clear();
    }

    private void Update()
    {
        if(Active)
        {
            if (eventSystem.currentInputModule.IsPointerOverGameObject(-1))
            {
                return;
            }

            bool hoveredOverWall = false;
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
                        hoveredOverWall = true;
                        highlightedWall.SetMaterial(true);
                        currentBuildingBlockPrefabInstance.gameObject.SetActive(false);

                        if(Mouse.current.leftButton.wasPressedThisFrame)
                        {
                            wall.RotateWallType();
                        }
                    }
                    else
                    {
                        HexCoordinates coordinates = HexCoordinates.FromPosition(hit.point);
                        Vector3 centerPosition = coordinates.ToPosition() + new Vector3(0, hit.point.y);
                        if(itemPrefabInstance != null)
                        {
                            HexagonBuildingBlock block;
                            currentBuildingBlockPrefabInstance.gameObject.SetActive(false);
                            if (AnyOthersHere(coordinates, out block))
                            {
                                float forcedRotation = -1;
                                bool cantPlace = false;
                                if(placeableItemLocations.ContainsKey(coordinates))
                                {
                                    if(placeableItemLocations[coordinates] != null)
                                    {
                                        cantPlace = true;
                                    }
                                    //else
                                    //{
                                    //    forcedRotation = placeableItemLocations[coordinates][0].transform.rotation.eulerAngles.y + 180;
                                    //}
                                }

                                if(cantPlace)
                                {
                                    itemPrefabInstance.gameObject.SetActive(false);
                                }
                                else
                                {
                                    itemPrefabInstance.gameObject.SetActive(true);
                                    itemPrefabInstance.transform.position = centerPosition + new Vector3(0, 0.2f);
                                    if(forcedRotation == -1)
                                    {
                                        Vector3 differenceVector = hit.point - centerPosition;
                                        float degrees = Mathf.Atan2(differenceVector.x, differenceVector.z) * Mathf.Rad2Deg;
                                        degrees = Mathf.RoundToInt(degrees / 60) * 60;
                                        itemPrefabInstance.transform.rotation = Quaternion.Euler(new Vector3(0, degrees));
                                    }
                                    else
                                    {
                                        itemPrefabInstance.transform.rotation = Quaternion.Euler(new Vector3(0, forcedRotation));
                                    }
                                }
                            }
                            else
                            {
                                itemPrefabInstance.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            HexagonBuildingBlock block;
                            if (AnyOthersHere(coordinates, out block))
                            {
                                currentBuildingBlockPrefabInstance.gameObject.SetActive(false);

                                if(Mouse.current.rightButton.wasPressedThisFrame)
                                {
                                    allBuildingBlocks.Remove(block);

                                    if (placeableItemLocations.ContainsKey(coordinates))
                                    {
                                        JobWorkableGO placedItem = placeableItemLocations[coordinates];
                                        allPlacedItems.Remove(placedItem);
                                        Destroy(placedItem.gameObject);
                                    }

                                    Destroy(block.gameObject);

                                    resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
                                    UpdateAllNeighbors();
                                }
                            }
                            else
                            {
                                currentBuildingBlockPrefabInstance.gameObject.SetActive(true);
                                currentBuildingBlockPrefabInstance.transform.position = centerPosition;
                            }
                        }
                    }
                }
            }
            if(Mouse.current.leftButton.wasPressedThisFrame && !hoveredOverWall)
            {
                if(itemPrefabInstance != null && itemPrefabInstance.gameObject.activeSelf)
                {
                    HexCoordinates coords = HexCoordinates.FromPosition(itemPrefabInstance.transform.position);
                    if(!placeableItemLocations.ContainsKey(coords))
                    {
                        placeableItemLocations.Add(coords, null);
                    }
                    if(placeableItemLocations[coords] == null)
                    {
                        //Cost
                        HexagonBuildingBlock blockAssociated = null;
                        for (int i = 0; i < allBuildingBlocks.Count; i++)
                        {
                            if (allBuildingBlocks[i].Coordinates == coords)
                            {
                                blockAssociated = allBuildingBlocks[i];
                                break;
                            }
                        }

                        for (int i = 0; i < itemPrefabInstance.Cost.Length; i++)
                        {
                            resourceCostTotal[itemPrefabInstance.Cost[i].ResourceType] += itemPrefabInstance.Cost[i].Amount;
                            blockAssociated.AddAssociatedResourceCost(itemPrefabInstance.Cost[i]);
                        }
                        resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);

                        placeableItemLocations[coords] = itemPrefabInstance;
                        allPlacedItems.Add(itemPrefabInstance);
                        itemPrefabInstance = null;
                    }
                }
                else if (currentBuildingBlockPrefabInstance.gameObject.activeSelf)
                {
                    currentBuildingBlockPrefabInstance.Place();
                    //TODO: Replace this with cost based on selected material type
                    resourceCostTotal[ResourceType.Wood] += 5;
                    resourceCostTotal[ResourceType.Stone] += 2;
                    currentBuildingBlockPrefabInstance.AddAssociatedResourceCost(new ResourceCount()
                    {
                        ResourceType = ResourceType.Wood,
                        Amount = 5
                    });
                    currentBuildingBlockPrefabInstance.AddAssociatedResourceCost(new ResourceCount()
                    {
                        ResourceType = ResourceType.Stone,
                        Amount = 2
                    });
                    resourceHandler.OverrideResourceDisplay(resourceCostTotal, Color.green);
                    currentBuildingBlockPrefabInstance.Coordinates = HexCoordinates.FromPosition(currentBuildingBlockPrefabInstance.transform.position);
                    allBuildingBlocks.Add(currentBuildingBlockPrefabInstance);
                    currentBuildingBlockPrefabInstance = Instantiate(hexagonPrefab);

                    UpdateAllNeighbors();
                }
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
