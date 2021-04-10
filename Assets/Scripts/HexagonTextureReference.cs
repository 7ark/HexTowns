using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonTextureReference : MonoBehaviour
{
    [System.Serializable]
    private struct TextureInfo
    {
        public Vector2Int heightRange;
        public Texture2D[] textureChoices;
    }

    [SerializeField]
    private List<TextureInfo> textureData;
    [SerializeField]
    private int textureSize = 512;

    private List<Texture2D> allTextures = new List<Texture2D>();
    private Dictionary<Texture2D, int> texToIndex = new Dictionary<Texture2D, int>();

    public static Rect[] ATLAS_UVs;
    private Texture2D atlas;

    /// <summary>
    /// Gets a random material based on the height passed
    /// </summary>
    /// <param name="height">Height of the tile</param>
    /// <param name="defaultToMaxMin">Instead of returning null, use the highest or lowest options available</param>
    /// <returns></returns>
    public Texture2D GetTextureFromHeight(int height, bool defaultToMaxMin = true)
    {
        int lowestIndex = 0;
        int HighestIndex = 0;
        int lowestHeight = int.MaxValue;
        int highestHeight = 0;
        for (int i = 0; i < textureData.Count; i++)
        {
            if (height >= textureData[i].heightRange.x && height <= textureData[i].heightRange.y)
            {
                return textureData[i].textureChoices[Random.Range(0, textureData[i].textureChoices.Length)];
            }
            if (textureData[i].heightRange.x < lowestHeight)
            {
                lowestHeight = textureData[i].heightRange.x;
                lowestIndex = i;
            }
            if (textureData[i].heightRange.y > highestHeight)
            {
                highestHeight = textureData[i].heightRange.y;
                HighestIndex = i;
            }
        }
    
        if (defaultToMaxMin)
        {
            if (height < textureData[lowestIndex].heightRange.x)
            {
                return textureData[lowestIndex].textureChoices[Random.Range(0, textureData[lowestIndex].textureChoices.Length)];
            }
            else if (height > textureData[HighestIndex].heightRange.y)
            {
                return textureData[HighestIndex].textureChoices[Random.Range(0, textureData[HighestIndex].textureChoices.Length)];
            }
        }
    
        return null;
    }

    public int GetTextureIndex(int height, bool defaultToMaxMin = true)
    {
        Texture2D texRef = GetTextureFromHeight(height, defaultToMaxMin);
        int index = texToIndex[texRef];
        return index;
    }

    public Texture2D CreateAtlas()
    {
        texToIndex.Clear();
        allTextures.Clear();
        int index = 0;
        for (int i = 0; i < textureData.Count; i++)
        {
            for (int j = 0; j < textureData[i].textureChoices.Length; j++)
            {
                if (!allTextures.Contains(textureData[i].textureChoices[j]))
                {
                    Texture2D tex = textureData[i].textureChoices[j];
                    allTextures.Add(tex);
                    texToIndex.Add(tex, index);
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

        atlas = newTexture;
        return newTexture;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < allTextures.Count; i++)
        {
            Destroy(allTextures[i]);
        }
        Destroy(atlas);
    }
}
