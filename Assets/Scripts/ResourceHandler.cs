using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourcePhysicalData
{
    public ResourceType ResourceType;
    public HashSet<ResourceIndividual> Resources = new HashSet<ResourceIndividual>();
}

public class ResourceIndividual
{
    public ResourceType Type;
    public System.Guid physicalGuidReference;
    public GameObject Parent; //TODO: Make this not reliant on a GO? maybe a delegate
    public Vector3 FollowOffset;

    public ResourceIndividual(ResourceType type, System.Guid phyRef)
    {
        Type = type;
        physicalGuidReference = phyRef;
    }
}

public class ResourceHandler : MonoBehaviour
{
    [System.Serializable]
    private struct ResourceDisplayInfo
    {
        public ResourceType Type;
        public Sprite DisplayImage;
    }
    [System.Serializable]
    private struct ResourcePrefabData
    {
        public ResourceType type;
        public GameObject prefab;
    }
    public static ResourceHandler Instance;

    [SerializeField]
    private PlacementPrefabHandler placementPrefabHandler;
    [SerializeField]
    private ResourceDisplayInfo[] displayInfo;
    [SerializeField]
    private Image[] displayImages;
    [SerializeField]
    private ResourcePrefabData[] prefabs;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private Dictionary<Image, TextMeshProUGUI> imageToText = new Dictionary<Image, TextMeshProUGUI>();
    private Dictionary<HexTile, ResourcePhysicalData> tileResourceData = new Dictionary<HexTile, ResourcePhysicalData>();
    private Dictionary<HexTile, HashSet<System.Guid>> tileGuids = new Dictionary<HexTile, HashSet<System.Guid>>();
    private HashSet<HexTile> lockedTilesFromTakingResources = new HashSet<HexTile>();

    private HashSet<ResourceIndividual> allTrackedResources = new HashSet<ResourceIndividual>();
    private Dictionary<ResourceType, GameObject> resourcePrefabs = new Dictionary<ResourceType, GameObject>();
    private Dictionary<ResourceType, InstancedGenericObject> instancedResources = new Dictionary<ResourceType, InstancedGenericObject>();

    public Dictionary<ResourceType, Sprite> ResourceVisuals { get; private set; } = new Dictionary<ResourceType, Sprite>();
    private bool displayingOther = false;
    private int flagDayIncrement = 3;
    private int flagDay = 3;

    public ResourceType[] AllResources { get; private set; }

    private void Awake()
    {
        Instance = this;

        AllResources = new ResourceType[System.Enum.GetValues(typeof(ResourceType)).Length];
        for (int i = 0; i < AllResources.Length; i++)
        {
            resources.Add((ResourceType)i, 0);
            ResourceVisuals.Add((ResourceType)i, null);
            AllResources[i] = (ResourceType)i;
        }
        for (int i = 0; i < displayInfo.Length; i++)
        {
            ResourceVisuals[displayInfo[i].Type] = displayInfo[i].DisplayImage;
        }
        for (int i = 0; i < displayImages.Length; i++)
        {
            imageToText.Add(displayImages[i], displayImages[i].GetComponentInChildren<TextMeshProUGUI>());
        }
        for (int i = 0; i < prefabs.Length; i++)
        {
            resourcePrefabs.Add(prefabs[i].type, prefabs[i].prefab);
            instancedResources.Add(prefabs[i].type, new InstancedGenericObject(prefabs[i].prefab, true));
        }


        SetDefaultValues();
        UpdateDisplayImages();
    }

    private void Start()
    {
        GameTime.Instance.OnNewDay += (daysPassed) =>
        {
            if ((daysPassed + 1) == flagDay)
            {
                flagDayIncrement++;
                flagDay += flagDayIncrement;
                resources[ResourceType.Flags]++;
            }
        };
    }

    private void Update()
    {
        foreach(var resource in allTrackedResources)
        {
            if(resource.Parent != null)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(resource.Parent.transform.position + resource.FollowOffset, resource.Parent.transform.rotation, resourcePrefabs[resource.Type].transform.localScale);
                instancedResources[resource.Type].UpdateDataPoint(resource.physicalGuidReference, matrix);
            }
        }
        foreach(var key in instancedResources.Keys)
        {
            instancedResources[key].Update();
        }
    }

    private void SetDefaultValues()
    {
        resources[ResourceType.Flags] = 1;
        //resources[ResourceType.Food] = 200;
        //resources[ResourceType.Wood] = 50;
        //resources[ResourceType.Stone] = 25;
    }

    public void UpdateDisplayImages(bool forceUpdate = true)
    {
        if(displayingOther && !forceUpdate)
        {
            return;
        }
        displayingOther = false;

        for (int i = 0; i < displayImages.Length; i++)
        {
            displayImages[i].sprite = displayInfo[i].DisplayImage;
            imageToText[displayImages[i]].text = resources[displayInfo[i].Type].ToString("00");
            imageToText[displayImages[i]].color = Color.white;
        }
    }

    //public bool IsThereEnoughResource2(ResourceType type, int amount)
    //{
    //    return resources[type] >= amount;
    //}

    public void OverrideResourceDisplay(Dictionary<ResourceType, int> resourceOverride, Color textColor)
    {
        displayingOther = true;
        for (int i = 0; i < displayImages.Length; i++)
        {
            displayImages[i].sprite = displayInfo[i].DisplayImage;
            imageToText[displayImages[i]].text = resourceOverride.ContainsKey(displayInfo[i].Type) ? resourceOverride[displayInfo[i].Type].ToString("00") : "00";
            imageToText[displayImages[i]].color = textColor;
        }
    }


    public void SpawnNewResource(ResourceType type, int amount, HexTile gainedLocation)
    {
        resources[type] += amount;

        UpdateDisplayImages(false);
        placementPrefabHandler.UpdateButtons();

        if (!tileResourceData.ContainsKey(gainedLocation))
        {
            tileResourceData.Add(gainedLocation, new ResourcePhysicalData());
            tileGuids.Add(gainedLocation, new HashSet<System.Guid>());
        }
        else if (tileResourceData[gainedLocation].Resources.Count > 0)
        {
            if (tileResourceData[gainedLocation].ResourceType != type)
            {
                foreach(var tile in tileResourceData[gainedLocation].Resources)
                {
                    DropResourceNearby(tile, gainedLocation, true);
                }
                Debug.LogError("Adding resource to a tile that doesnt have that resource");
            }
        }

        tileResourceData[gainedLocation].ResourceType = type;
        for (int i = 0; i < amount; i++)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            System.Guid refGuid = instancedResources[type].AddDataPoint(matrix);
            ResourceIndividual resource = new ResourceIndividual(type, refGuid);
            allTrackedResources.Add(resource);
            tileResourceData[gainedLocation].Resources.Add(resource);
        }

        OrganizeResources(gainedLocation, type);
    }

    public void DropResourceNearby(ResourceIndividual resource, HexTile location, bool forceDifferentTile = false)
    {
        if(!forceDifferentTile && (!tileResourceData.ContainsKey(location) || tileResourceData[location].Resources.Count <= 0 || tileResourceData[location].ResourceType == resource.Type))
        {
            PlaceResourceOnTile(resource, location);
        }
        else
        {
            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors_Uncached(location);

            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!tileResourceData.ContainsKey(neighbors[i]) || tileResourceData[neighbors[i]].Resources.Count <= 0 || tileResourceData[neighbors[i]].ResourceType == resource.Type)
                {
                    PlaceResourceOnTile(resource, neighbors[i]);
                    return;
                }
            }

            Debug.LogError("Theres nowhere to drop this resource!");
        }
    }

    public void PlaceResourceOnTile(ResourceIndividual resource, HexTile location)
    {
        if(resource == null)
        {
            return;
        }
        if (!tileResourceData.ContainsKey(location))
        {
            tileResourceData.Add(location, new ResourcePhysicalData());
            tileGuids.Add(location, new HashSet<System.Guid>());
        }
        else if (tileResourceData[location].Resources.Count > 0)
        {
            if (tileResourceData[location].ResourceType != resource.Type)
            {
                Debug.LogError("Adding resource to a tile that doesnt have that resource. Dropping it elsewhere");
                DropResourceNearby(resource, location, true); 
            }
        }

        tileResourceData[location].ResourceType = resource.Type;
        tileResourceData[location].Resources.Add(resource);

        OrganizeResources(location, resource.Type);
    }

    public bool AnyFlags()
    {
        return resources[ResourceType.Flags] > 0;
    }

    public bool UseFlag()
    {
        if(AnyFlags())
        {
            resources[ResourceType.Flags]--;
            return true;
        }

        return false;
    }

    public void ConsumeResource(ResourceIndividual resource)
    {
        instancedResources[resource.Type].RemoveDataPoint(resource.physicalGuidReference);
        allTrackedResources.Remove(resource);

        resources[resource.Type]--;

        UpdateDisplayImages(false);
        placementPrefabHandler.UpdateButtons();
    }

    public ResourcePhysicalData PeekResourcesAtLocation(HexTile locationToRetrieve)
    {
        if(!tileResourceData.ContainsKey(locationToRetrieve))
        {
            return new ResourcePhysicalData();
        }
        return tileResourceData[locationToRetrieve];
    }

    public bool RetrieveResourceFromTile(ResourceType type, HexTile locationToRetrieve, out ResourceIndividual resourceRetrieved)
    {
        if (!tileResourceData.ContainsKey(locationToRetrieve))
        {
            tileResourceData.Add(locationToRetrieve, new ResourcePhysicalData());
            tileGuids.Add(locationToRetrieve, new HashSet<System.Guid>());
        }
        else if (tileResourceData[locationToRetrieve].Resources.Count > 0)
        {
            if (tileResourceData[locationToRetrieve].ResourceType != type)
            {
                Debug.LogError("Adding resource to a tile that doesnt have that resource");
            }
        }

        if(tileResourceData[locationToRetrieve].Resources.Count <= 0)
        {
            resourceRetrieved = null;
            return false;
        }

        ResourceIndividual firstResource = null;
        foreach(var resource in tileResourceData[locationToRetrieve].Resources)
        {
            firstResource = resource;
            break;
        }

        tileResourceData[locationToRetrieve].Resources.Remove(firstResource);
        resourceRetrieved = firstResource;


        OrganizeResources(locationToRetrieve, type);

        return true;
    }

    private void OrganizeResources(HexTile associatedTile, ResourceType type)
    {
        int amountToDisplay = tileResourceData[associatedTile].Resources.Count;
        float yChange = 0;
        bool directionX = true;
        Quaternion rotation = Quaternion.identity;
        int index = 0;
        foreach(var resource in tileResourceData[associatedTile].Resources)
        {
            if(type == ResourceType.Wood)
            {
                int modValue = 5;
                if (amountToDisplay < 5)
                {
                    modValue = amountToDisplay;
                }
                float offset = Mathf.Lerp(-0.5f, 0.5f, modValue <= 1 ? 0.5f : (float)(index % modValue) / (modValue - 1)) + Random.Range(-0.04f, 0.04f);
                Vector3 pos = new Vector3(directionX ? offset : 0, yChange, directionX ? 0 : offset);
                Quaternion rot = Quaternion.Euler(0, rotation.eulerAngles.y, 0/*Random.Range(0, 360)*/);

                Matrix4x4 matrix = Matrix4x4.TRS(associatedTile.Position + new Vector3(0, associatedTile.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP) + pos, rot, resourcePrefabs[type].transform.localScale);

                instancedResources[type].UpdateDataPoint(resource.physicalGuidReference, matrix);

                if (index % modValue == 0 && index != 0)
                {
                    yChange += 0.18f;
                    directionX = !directionX;
                    if (rotation == Quaternion.identity)
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }
                    else
                    {
                        rotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                int modValue = 4;
                int stackValue = (int)Mathf.Pow(modValue, 2);
                float xOffset = Mathf.Lerp(-0.5f, 0.5f, (float)(index % modValue) / (modValue - 1));
                float zOffset = Mathf.Lerp(-0.5f, 0.5f, Mathf.Floor(index % stackValue / modValue) / (modValue - 1));
                Vector3 pos = new Vector3(xOffset, yChange, zOffset);

                if(type == ResourceType.Food)
                {
                    int dir = Random.Range(0, 4);
                    rotation = Quaternion.Euler(0, dir * 90, 0);
                }

                Matrix4x4 matrix = Matrix4x4.TRS(associatedTile.Position + new Vector3(0, associatedTile.Height * HexTile.HEIGHT_STEP - HexTile.HEIGHT_STEP) + pos, rotation, resourcePrefabs[type].transform.localScale);
                instancedResources[type].UpdateDataPoint(resource.physicalGuidReference, matrix);

                if (index % stackValue == 0 && index != 0)
                {
                    yChange += 0.27f;
                    directionX = !directionX;
                }
            }

            index++;
        }
    }
}

public enum ResourceType
{
    Flags,
    Stone,
    Wood,
    Food
}
