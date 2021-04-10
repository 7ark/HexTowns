using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HexTextureReference", menuName = "ScriptableObjects/HexTextureReference", order = 1)]
public class HexTextureReference : ScriptableObject
{
    [System.Serializable]
    private struct TextureInfo
    {
        public Vector2Int heightRange;
        public Material[] materialChoices;
    }

    [SerializeField]
    private List<TextureInfo> textureData;
    [SerializeField]
    private int textureSize = 1024;

    private List<Texture2D> allTextures = new List<Texture2D>();

    public static Rect[] ATLAS_UVs;
    private Dictionary<Material, int> matToRectIndex = new Dictionary<Material, int>();

    /// <summary>
    /// Gets a random material based on the height passed
    /// </summary>
    /// <param name="height">Height of the tile</param>
    /// <param name="defaultToMaxMin">Instead of returning null, use the highest or lowest options available</param>
    /// <returns></returns>
    public Material GetMaterial(int height, bool defaultToMaxMin = true)
    {
        int lowestIndex = 0;
        int HighestIndex = 0;
        int lowestHeight = int.MaxValue;
        int highestHeight = 0;
        for (int i = 0; i < textureData.Count; i++)
        {
            if(height >= textureData[i].heightRange.x && height <= textureData[i].heightRange.y)
            {
                return textureData[i].materialChoices[Random.Range(0, textureData[i].materialChoices.Length)];
            }
            if(textureData[i].heightRange.x < lowestHeight)
            {
                lowestHeight = textureData[i].heightRange.x;
                lowestIndex = i;
            }
            if(textureData[i].heightRange.y > highestHeight)
            {
                highestHeight = textureData[i].heightRange.y;
                HighestIndex = i;
            }
        }

        if(defaultToMaxMin)
        {
            if (height < textureData[lowestIndex].heightRange.x)
            {
                return textureData[lowestIndex].materialChoices[Random.Range(0, textureData[lowestIndex].materialChoices.Length)];
            }
            else if (height > textureData[HighestIndex].heightRange.y)
            {
                return textureData[HighestIndex].materialChoices[Random.Range(0, textureData[HighestIndex].materialChoices.Length)];
            }
        }

        return null;
    }

    public int GetMaterialIndex(int height, bool defaultToMaxMin = true)
    {
        Material mat = GetMaterial(height, defaultToMaxMin);
        return matToRectIndex[mat];
    }

    public Texture2D CreateAtlas()
    {
        matToRectIndex.Clear();
        allTextures.Clear();
        int index = 0;
        for (int i = 0; i < textureData.Count; i++)
        {
            for (int j = 0; j < textureData[i].materialChoices.Length; j++)
            {
                if(!allTextures.Contains((Texture2D)textureData[i].materialChoices[j].mainTexture))
                {
                    allTextures.Add((Texture2D)textureData[i].materialChoices[j].mainTexture);
                    matToRectIndex.Add(textureData[i].materialChoices[j], index);
                    index++;
                }
            }
        }


        int sideRes = (allTextures.Count / 2) * textureSize + 2;

        Texture2D newTexture = new Texture2D(sideRes, sideRes);
        Rect[] vals = newTexture.PackTextures(allTextures.ToArray(), 2, sideRes);
        ATLAS_UVs = vals;

        allTextures.Clear();
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        return newTexture;
    }
}
