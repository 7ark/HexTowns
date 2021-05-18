using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceHandler : MonoBehaviour
{
    [System.Serializable]
    private struct ResourceDisplayInfo
    {
        public ResourceType Type;
        public Sprite DisplayImage;
    }
    private class PhysicalResourceData
    {
        public InstancedType instanceType;
        public int amount;
    }
    public static ResourceHandler Instance;

    [SerializeField]
    private PlacementPrefabHandler placementPrefabHandler;
    [SerializeField]
    private ResourceDisplayInfo[] displayInfo;
    [SerializeField]
    private Image[] displayImages;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private Dictionary<Image, TextMeshProUGUI> imageToText = new Dictionary<Image, TextMeshProUGUI>();
    private Dictionary<ResourceType, InstancedType> resourceTypeToInstancedType = new Dictionary<ResourceType, InstancedType>();
    private Dictionary<HexTile, PhysicalResourceData> tileResourceData = new Dictionary<HexTile, PhysicalResourceData>();
    private Dictionary<HexTile, HashSet<System.Guid>> tileGuids = new Dictionary<HexTile, HashSet<System.Guid>>();
    private HashSet<HexTile> lockedTilesFromTakingResources = new HashSet<HexTile>();
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

        resourceTypeToInstancedType.Add(ResourceType.Wood, InstancedType.Resource_Log);
        resourceTypeToInstancedType.Add(ResourceType.Stone, InstancedType.Resource_Stone);
        resourceTypeToInstancedType.Add(ResourceType.Food, InstancedType.Resource_Food);

        SetDefaultValues();
        UpdateDisplayImages();
    }

    public InstancedType ResourceToInstanced(ResourceType type)
    {
        return resourceTypeToInstancedType[type];
    }

    private void Start()
    {
        GameTime.Instance.OnNewDay += (daysPassed) =>
        {
            if ((daysPassed + 1) == flagDay)
            {
                flagDayIncrement++;
                flagDay += flagDayIncrement;
                GainResource(ResourceType.Flags, 1, null);
            }
        };
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

    public bool IsThereEnoughResource(ResourceType type, int amount)
    {
        return resources[type] >= amount;
    }

    public bool IsThereEnoughResource(ResourceType type, int amount, HexTile tile)
    {
        return tileResourceData[tile].instanceType == resourceTypeToInstancedType[type] && tileResourceData[tile].amount >= amount;
    }

    public bool IsThereEnoughResource(InstancedType type, int amount, HexTile tile)
    {
        return tileResourceData[tile].instanceType == type && tileResourceData[tile].amount >= amount;
    }

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

    public void GainResource(ResourceType type, int amount, HexTile gainedLocation, bool addToData = true)
    {
        if(addToData)
        {
            resources[type] += amount;

            UpdateDisplayImages(false);
            placementPrefabHandler.UpdateButtons();
        }

        if(resourceTypeToInstancedType.ContainsKey(type))
        {
            if (!tileResourceData.ContainsKey(gainedLocation))
            {
                tileResourceData.Add(gainedLocation, new PhysicalResourceData());
                tileGuids.Add(gainedLocation, new HashSet<System.Guid>());
            }
            else if(tileResourceData[gainedLocation].amount > 0)
            {
                if(tileResourceData[gainedLocation].instanceType != resourceTypeToInstancedType[type])
                {
                    Debug.LogError("Adding resource to a tile that doesnt have that resource");
                }
            }

            List<System.Guid> existingResources = new List<System.Guid>(tileGuids[gainedLocation]);
            //foreach(var guid in tileGuids[gainedLocation])
            //{
            //    gainedLocation.ParentBoard.RemoveType(resourceTypeToInstancedType[type], guid);
            //}

            tileResourceData[gainedLocation].instanceType = resourceTypeToInstancedType[type];
            tileResourceData[gainedLocation].amount += amount;

            OrganizeResources(gainedLocation, type, existingResources);

            //int amountToDisplay = Mathf.CeilToInt(tileResourceData[gainedLocation].amount); //TODO: Change 5 to something dependent on resource
            //float yChange = 0;
            //bool directionX = true;
            //Quaternion rotation = Quaternion.identity;
            //for (int i = 0; i < amountToDisplay; i++)
            //{
            //    int modValue = 5;
            //    if(amountToDisplay < 5)
            //    {
            //        modValue = amountToDisplay;
            //    }
            //    float offset = Mathf.Lerp(-0.5f, 0.5f, (float)(i % modValue) / (modValue - 1)) + Random.Range(-0.04f, 0.04f);
            //    Vector3 pos = new Vector3(directionX ? offset : 0, yChange, directionX ? 0 : offset);
            //    Quaternion rot = Quaternion.Euler(0, rotation.eulerAngles.y, 0/*Random.Range(0, 360)*/);
            //    if (i < existingResources.Count)
            //    {
            //        gainedLocation.ParentBoard.ModifyType(resourceTypeToInstancedType[type], gainedLocation, existingResources[i], pos, rot);
            //    }
            //    else
            //    {
            //        tileGuids[gainedLocation].Add(gainedLocation.ParentBoard.AddInstancedType(resourceTypeToInstancedType[type], gainedLocation, rotation: rot, posAdjustment: pos));
            //    }
            //    if(i % 5 == 0 && i != 0)
            //    {
            //        yChange += 0.18f;
            //        directionX = !directionX;
            //        if(rotation == Quaternion.identity)
            //        {
            //            rotation = Quaternion.Euler(0, 90, 0);
            //        }
            //        else
            //        {
            //            rotation = Quaternion.identity;
            //        }
            //    }
            //}

        }
    }


    public int GetResourceRepresentationValue(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Stone:
                return 1;
            case ResourceType.Wood:
                return 1;
            case ResourceType.Food:
                return 5;
        }

        return 1;
    }

    public void LockResources(HexTile tile)
    {
        lockedTilesFromTakingResources.Add(tile);
    }

    public void UnlockTile(HexTile tile)
    {
        lockedTilesFromTakingResources.Remove(tile);
    }

    public bool AreResourcesLocked(HexTile tile)
    {
        return lockedTilesFromTakingResources.Contains(tile);
    }

    public bool UseFlag()
    {
        if(IsThereEnoughResource(ResourceType.Flags, 1))
        {
            resources[ResourceType.Flags]--;
            return true;
        }

        return false;
    }

    public bool UseResources(ResourceType type, int amount, HexTile locationToRetrieve)
    {
        if(PickupResources(type, amount, locationToRetrieve))
        {
            resources[type] -= amount;

            UpdateDisplayImages(false);
            placementPrefabHandler.UpdateButtons();
            return true;
        }

        return false;
    }

    public int ResourcesAtLocation(HexTile locationToRetrieve)
    {
        if(!tileResourceData.ContainsKey(locationToRetrieve))
        {
            return 0;
        }
        return tileResourceData[locationToRetrieve].amount;
    }

    public bool PickupResources(ResourceType type, int amount, HexTile locationToRetrieve) //TODO: Add restrictions or something on making sure amount is a factor of the resource type divisor
    {
        if(AreResourcesLocked(locationToRetrieve))
        {
            Debug.LogError("Tried to take from locked resources");
            return false;
        }
        if (resourceTypeToInstancedType.ContainsKey(type))
        {
            if (!tileResourceData.ContainsKey(locationToRetrieve))
            {
                tileResourceData.Add(locationToRetrieve, new PhysicalResourceData());
                tileGuids.Add(locationToRetrieve, new HashSet<System.Guid>());
            }
            else if (tileResourceData[locationToRetrieve].amount > 0)
            {
                if (tileResourceData[locationToRetrieve].instanceType != resourceTypeToInstancedType[type])
                {
                    Debug.LogError("Picking up resource to a tile that doesnt have that resource");
                }
            }

            if(tileResourceData[locationToRetrieve].amount <= 0) //TODO: Make sure theres enough in this stack to take
            {
                return false;
            }

            int resourceValue = GetResourceRepresentationValue(type);

            amount /= resourceValue;

            int amountCount = 0;
            HashSet<System.Guid> resourcesToRemove = new HashSet<System.Guid>();
            foreach (var guid in tileGuids[locationToRetrieve])
            {
                resourcesToRemove.Add(guid);

                amountCount++;
                if(amountCount >= amount)
                {
                    break;
                }
            }

            amountCount *= resourceValue;

            foreach (var guid in resourcesToRemove)
            {
                tileGuids[locationToRetrieve].Remove(guid);
                locationToRetrieve.ParentBoard.RemoveType(resourceTypeToInstancedType[type], guid);
            }

            List<System.Guid> existingResources = new List<System.Guid>(tileGuids[locationToRetrieve]);

            tileResourceData[locationToRetrieve].instanceType = resourceTypeToInstancedType[type];
            tileResourceData[locationToRetrieve].amount -= amount;

            OrganizeResources(locationToRetrieve, type, existingResources);

            //int amountToDisplay = Mathf.CeilToInt(tileResourceData[locationToRetrieve].amount); //TODO: Change 5 to something dependent on resource
            //float yChange = 0;
            //bool directionX = true;
            //Quaternion rotation = Quaternion.identity;
            //for (int i = 0; i < amountToDisplay; i++)
            //{
            //    int modValue = 5;
            //    if (amountToDisplay < 5)
            //    {
            //        modValue = amountToDisplay;
            //    }
            //    float offset = Mathf.Lerp(-0.5f, 0.5f, modValue <= 1 ? 0.5f : (float)(i % modValue) / (modValue - 1)) + Random.Range(-0.04f, 0.04f);
            //    Vector3 pos = new Vector3(directionX ? offset : 0, yChange, directionX ? 0 : offset);
            //    Quaternion rot = Quaternion.Euler(0, rotation.eulerAngles.y, 0/*Random.Range(0, 360)*/);
            //    locationToRetrieve.ParentBoard.ModifyType(resourceTypeToInstancedType[type], locationToRetrieve, existingResources[i], pos, rot);
            //    if (i % 5 == 0 && i != 0)
            //    {
            //        yChange += 0.18f;
            //        directionX = !directionX;
            //        if (rotation == Quaternion.identity)
            //        {
            //            rotation = Quaternion.Euler(0, 90, 0);
            //        }
            //        else
            //        {
            //            rotation = Quaternion.identity;
            //        }
            //    }
            //}
        }

        return true;
    }

    private void OrganizeResources(HexTile associatedTile, ResourceType type, List<System.Guid> existingResources)
    {
        int amountToRepresent = GetResourceRepresentationValue(type);
        int amountToDisplay = Mathf.CeilToInt(tileResourceData[associatedTile].amount / amountToRepresent);
        float yChange = 0;
        bool directionX = true;
        Quaternion rotation = Quaternion.identity;
        for (int i = 0; i < amountToDisplay; i++)
        {
            if(type == ResourceType.Wood)
            {
                int modValue = 5;
                if (amountToDisplay < 5)
                {
                    modValue = amountToDisplay;
                }
                float offset = Mathf.Lerp(-0.5f, 0.5f, modValue <= 1 ? 0.5f : (float)(i % modValue) / (modValue - 1)) + Random.Range(-0.04f, 0.04f);
                Vector3 pos = new Vector3(directionX ? offset : 0, yChange, directionX ? 0 : offset);
                Quaternion rot = Quaternion.Euler(0, rotation.eulerAngles.y, 0/*Random.Range(0, 360)*/);

                if (i < existingResources.Count)
                {
                    associatedTile.ParentBoard.ModifyType(resourceTypeToInstancedType[type], associatedTile, existingResources[i], pos, rot);
                }
                else
                {
                    tileGuids[associatedTile].Add(associatedTile.ParentBoard.AddInstancedType(resourceTypeToInstancedType[type], associatedTile, rotation: rot, posAdjustment: pos));
                }

                //associatedTile.ParentBoard.ModifyType(resourceTypeToInstancedType[type], associatedTile, existingResources[i], pos, rot);
                if (i % modValue == 0 && i != 0)
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
                float xOffset = Mathf.Lerp(-0.5f, 0.5f, (float)(i % modValue) / (modValue - 1));
                float zOffset = Mathf.Lerp(-0.5f, 0.5f, Mathf.Floor(i % stackValue / modValue) / (modValue - 1));
                Vector3 pos = new Vector3(xOffset, yChange, zOffset);

                if(type == ResourceType.Food)
                {
                    int dir = Random.Range(0, 4);
                    rotation = Quaternion.Euler(0, dir * 90, 0);
                }

                if (i < existingResources.Count)
                {
                    associatedTile.ParentBoard.ModifyType(resourceTypeToInstancedType[type], associatedTile, existingResources[i], pos, rotation);
                }
                else
                {
                    tileGuids[associatedTile].Add(associatedTile.ParentBoard.AddInstancedType(resourceTypeToInstancedType[type], associatedTile, rotation: rotation, posAdjustment: pos));
                }

                if (i % stackValue == 0 && i != 0)
                {
                    yChange += 0.27f;
                    directionX = !directionX;
                    //if (rotation == Quaternion.identity)
                    //{
                    //    rotation = Quaternion.Euler(0, 45, 0);
                    //}
                    //else
                    //{
                    //    rotation = Quaternion.identity;
                    //}
                }
            }
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
