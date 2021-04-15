using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public struct ResourceCount
{
    public ResourceType ResourceType;
    public int Amount;
}

public class PlacementPrefabHandler : MonoBehaviour
{
    [System.Serializable]
    private struct PlacementPrefabData
    {
        public string Name;
        public PlaceableGO PlaceablePrefab;
        public ResourceCount[] ResourcesUsed;
        [HideInInspector]
        public Button ButtonReference;
    }
    [SerializeField]
    private InteractionHandler interactionHandler;
    [SerializeField]
    private ResourceHandler resourceHandler;
    [SerializeField]
    private List<PlacementPrefabData> prefabData;
    [SerializeField]
    private Button buttonPrefab;
    [SerializeField]
    private Transform buttonParent;

    private Dictionary<string, PlaceableGO> nameToPrefab = new Dictionary<string, PlaceableGO>();
    private Dictionary<string, Button> nameToButtonReference = new Dictionary<string, Button>();
    private Dictionary<string, ResourceCount[]> nameToResourceInfo = new Dictionary<string, ResourceCount[]>();
    private Dictionary<string, Dictionary<ResourceType, int>> nameToResourceDictionaries = new Dictionary<string, Dictionary<ResourceType, int>>();

    private void Awake()
    {
        for (int i = 0; i < prefabData.Count; i++)
        {
            var data = prefabData[i];

            int index = i;
            string buildingName = prefabData[index].Name;
            Button newButton = AddButtonItem(buildingName);

            data.ButtonReference = newButton;
            prefabData[i] = data;
        }

        SetupDictionaries();
    }

    private Button AddButtonItem(string buildingName)
    {
        Button newButton = Instantiate(buttonPrefab, buttonParent);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = buildingName;
        newButton.onClick.AddListener(() => { StartPlacingItem(buildingName); });
        EventTrigger eventTrigger = newButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entryPointerEnter = new EventTrigger.Entry();
        entryPointerEnter.eventID = EventTriggerType.PointerEnter;
        entryPointerEnter.callback.AddListener((eventData) => { resourceHandler.OverrideResourceDisplay(nameToResourceDictionaries[buildingName], Color.green); });
        eventTrigger.triggers.Add(entryPointerEnter);
        EventTrigger.Entry entryPointerExit = new EventTrigger.Entry();
        entryPointerExit.eventID = EventTriggerType.PointerExit;
        entryPointerExit.callback.AddListener((eventData) => { resourceHandler.UpdateDisplayImages(); });
        eventTrigger.triggers.Add(entryPointerExit);

        return newButton;
    }

    private void SetupDictionaries()
    {
        nameToPrefab.Clear();
        nameToButtonReference.Clear();
        nameToResourceInfo.Clear();
        nameToResourceDictionaries.Clear();

        for (int i = 0; i < prefabData.Count; i++)
        {
            nameToPrefab.Add(prefabData[i].Name, prefabData[i].PlaceablePrefab);
            nameToResourceInfo.Add(prefabData[i].Name, prefabData[i].ResourcesUsed);
            nameToButtonReference.Add(prefabData[i].Name, prefabData[i].ButtonReference);
            Dictionary<ResourceType, int> resourceInfo = new Dictionary<ResourceType, int>();
            for (int j = 0; j < prefabData[i].ResourcesUsed.Length; j++)
            {
                resourceInfo.Add(prefabData[i].ResourcesUsed[j].ResourceType, prefabData[i].ResourcesUsed[j].Amount);
            }
            nameToResourceDictionaries.Add(prefabData[i].Name, resourceInfo);
        }
    }

    public void AddNewPlaceable(string placeableName, PlaceableGO prefab, ResourceCount[] resources)
    {
        Button newButton = AddButtonItem(placeableName);
        prefabData.Add(new PlacementPrefabData()
        {
            ButtonReference = newButton,
            Name = placeableName,
            PlaceablePrefab = prefab,
            ResourcesUsed = resources
        });

        SetupDictionaries();
    }

    public void StartPlacingItem(string name)
    {
        ResourceCount[] resourcesRequired = nameToResourceInfo[name];
        bool good = true;
        for (int i = 0; i < resourcesRequired.Length; i++)
        {
            if(!ResourceHandler.Instance.IsThereEnoughResource(resourcesRequired[i].ResourceType, resourcesRequired[i].Amount))
            {
                good = false;
                break;
            }
        }

        if(good)
        {
            interactionHandler.DisplayPlaceablePreview(nameToPrefab[name], name, () =>
            {
                for (int i = 0; i < resourcesRequired.Length; i++)
                {
                    ResourceHandler.Instance.UseResources(resourcesRequired[i].ResourceType, resourcesRequired[i].Amount);
                }

                UpdateButtons();
            });
        }

    }

    public void UpdateButtons()
    {
        for (int i = 0; i < prefabData.Count; i++)
        {
            bool allGood = true;
            for (int j = 0; j < prefabData[i].ResourcesUsed.Length; j++)
            {
                if(!ResourceHandler.Instance.IsThereEnoughResource(prefabData[i].ResourcesUsed[j].ResourceType, prefabData[i].ResourcesUsed[j].Amount))
                {
                    allGood = false;
                    break;
                }
            }

            prefabData[i].ButtonReference.interactable = allGood;
        }
    }

    public void StartModifyingTerrain()
    {
        interactionHandler.StartMultiSelection(100, true, (tiles, height) => 
        {
            HexTile[] resultTiles = HexagonSelectionHandler.Instance.GetFilledAreaBetweenTiles(tiles);//HexagonSelectionHandler.Instance.GetAllTilesBetweenTwoTiles(tiles[0], tiles[1]);
            TerrainModificationHandler.Instance.RequestTerrainModification(resultTiles, height);
        });
    }

    public void CollectResources()
    {
        interactionHandler.StartMultiSelection(100, false, (tiles, height) =>
        {
            HexTile[] resultTiles = HexagonSelectionHandler.Instance.GetFilledAreaBetweenTiles(tiles);//HexagonSelectionHandler.Instance.GetAllTilesBetweenTwoTiles(tiles[0], tiles[1]);

            for (int i = 0; i < resultTiles.Length; i++)
            {
                if(resultTiles[i].HasEnvironmentalItems)
                {
                    Workable[] workableObjects = resultTiles[i].GetEnvironmentalItemsAsWorkable();
                    for (int j = 0; j < workableObjects.Length; j++)
                    {
                        workableObjects[j].BeginWorking();
                        resultTiles[i].AddWorkableToTile(workableObjects[j], resultTiles[i].Height);
                    }
                }
            }

            //TerrainModificationHandler.Instance.RequestTerrainModification(resultTiles, height);
        });
    }
}
