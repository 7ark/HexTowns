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
    [System.Serializable]
    private struct TextureInfoIndex
    {
        public Vector2Int heightRange;
        public int[] textureChoices;
    }

    [SerializeField]
    private List<TextureInfo> textureData;
    [SerializeField]
    private int textureSize = 512;

    private List<Texture2D> allTextures = new List<Texture2D>();
    private Dictionary<Texture2D, int> texToIndex = new Dictionary<Texture2D, int>();
    private Dictionary<int, int[]> textureDataIndices = new Dictionary<int, int[]>();

    public static Rect[] ATLAS_UVs;
    private Texture2D atlas;
    private int greatestHeight = 0;
    private int lowestHeight = int.MaxValue;

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
        //Texture2D texRef = GetTextureFromHeight(height, defaultToMaxMin);
        //int index = texToIndex[texRef];
        if(defaultToMaxMin)
        {
            if (height > greatestHeight)
            {
                height = greatestHeight;
            }
            if (height < lowestHeight)
            {
                height = lowestHeight;
            }
        }
        int[] range = textureDataIndices[height];
        return range[Random.Range(0, range.Length)];
    }

    public List<Texture2D> OrganizeAllTextures()
    {
        texToIndex.Clear();
        allTextures.Clear();
        int index = 0;
        for (int i = 0; i < textureData.Count; i++)
        {
            for (int j = textureData[i].heightRange.x; j <= textureData[i].heightRange.y; j++)
            {
                if(j > greatestHeight)
                {
                    greatestHeight = j;
                }
                if(j < lowestHeight)
                {
                    lowestHeight = j;
                }
                textureDataIndices.Add(j, null);
            }
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
            int[] textureIndices = new int[textureData[i].textureChoices.Length];
            for (int j = 0; j < textureData[i].textureChoices.Length; j++)
            {
                textureIndices[j] = texToIndex[textureData[i].textureChoices[j]];
            }
            for (int j = textureData[i].heightRange.x; j <= textureData[i].heightRange.y; j++)
            {
                textureDataIndices[j] = textureIndices;
            }
        }

        return allTextures;
    }
}
