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
    public static ResourceHandler Instance;

    [SerializeField]
    private PlacementPrefabHandler placementPrefabHandler;
    [SerializeField]
    private ResourceDisplayInfo[] displayInfo;
    [SerializeField]
    private Image[] displayImages;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private Dictionary<Image, TextMeshProUGUI> imageToText = new Dictionary<Image, TextMeshProUGUI>();
    public Dictionary<ResourceType, Sprite> ResourceVisuals { get; private set; } = new Dictionary<ResourceType, Sprite>();
    private bool displayingOther = false;

    public ResourceType[] AllResources { get; private set; }

    private void Awake()
    {
        Instance = this;

        AllResources = new ResourceType[System.Enum.GetValues(typeof(ResourceType)).Length];
        for (int i = 0; i < AllResources.Length; i++)
        {
            resources.Add((ResourceType)i, 10000);
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

        SetDefaultValues();
        UpdateDisplayImages();
    }

    private void SetDefaultValues()
    {
        resources[ResourceType.Flags] = 1;
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

    public void GainResource(ResourceType type, int amount)
    {
        resources[type] += amount;

        UpdateDisplayImages(false);
        placementPrefabHandler.UpdateButtons();
    }

    public bool UseResources(ResourceType type, int amount)
    {
        if(IsThereEnoughResource(type, amount))
        {
            resources[type] -= amount;

            UpdateDisplayImages(false);
            placementPrefabHandler.UpdateButtons();
            return true;
        }

        return false;
    }
}

public enum ResourceType
{
    Flags,
    Stone,
    Wood,
    Food
}
