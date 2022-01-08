using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexagonTextureReference : MonoBehaviour
{
    [System.Serializable]
    private struct TextureInfo
    {
        public bool overrideBiome;
        public Vector2Int heightRange;
        public HexTextureType[] textureChoices;
    }

    [System.Serializable]
    private struct TextureBiomeInfo
    {
        public Biome biome;
        public HexTextureType[] textureChoices;
    }

    public enum HexTextureType
    {
        Dirt_V1,
        
        Sand_Underwater_V1,
        Sand_Underwater_V2,
        
        Sand_V1,
        Sand_V2,
        
        Grass_V1,
        Grass_V2,
        Grass_V3,
        Grass_V4,
        Grass_V5,
        Grass_V6,
        Grass_V7,
        
        Grass_Flowers_V1,
        Grass_Flowers_V2,
        Grass_Flowers_V3,
        Grass_Flowers_V4,
        Grass_Flowers_V5,
        Grass_Flowers_V6,
        Grass_Flowers_V7,
        Grass_Flowers_V8,
        
        Grass_Yellow_V1,
        Grass_Yellow_V2,
        Grass_Yellow_V3,
        Grass_Yellow_V4,
        Grass_Yellow_V5,
        
        Stone_V1,
        Stone_V2,
        Stone_V3,
        Stone_V4,
        Stone_V5,
        
        Snow_V1,
        Snow_V2,
        Snow_V3,
    }
    
    [System.Serializable]
    private struct TextureInformation
    {
        public HexTextureType textureType;
        public Texture2D texture;
    }

    [SerializeField]
    private List<TextureInformation> textureInfo;
    [SerializeField]
    private List<TextureInfo> textureData;
    [SerializeField]
    private List<TextureBiomeInfo> textureBiomeData;

    private List<Texture2D> allTextures = new List<Texture2D>();
    private Dictionary<int, int[]> textureHeightIndices = new Dictionary<int, int[]>();
    private Dictionary<int, bool> heightOverridesBiome = new Dictionary<int, bool>();
    private Dictionary<Biome, int[]> textureBiomeIndices = new Dictionary<Biome, int[]>();

    private Dictionary<HexTextureType, Texture2D> textureOptions = new Dictionary<HexTextureType, Texture2D>();

    private Texture2D atlas;
    private int greatestHeight = 0;
    private int lowestHeight = int.MaxValue;

    private void OnValidate()
    {
        if (textureInfo.Count < System.Enum.GetValues(typeof(HexTextureType)).Length)
        {
            for (int i = 0; i < System.Enum.GetValues(typeof(HexTextureType)).Length; i++)
            {
                if (textureInfo.Count - 1 < i)
                {
                    textureInfo.Add(new TextureInformation());
                }

                textureInfo[i] = new TextureInformation()
                {
                    textureType = (HexTextureType) i,
                    texture = textureInfo[i].texture
                };
            }
        }
    }

    public int GetTextureIndex(int height, Biome biome, bool defaultToMaxMin = true)
    {
        // return textureBiomeIndices[biome][0];
        
        if (textureBiomeIndices.ContainsKey(biome) && (!heightOverridesBiome.ContainsKey(height) || !heightOverridesBiome[height]))
        {
            int[] rangeBiome = textureBiomeIndices[biome];
            int resultBiome = rangeBiome[Random.Range(0, rangeBiome.Length)];
            return resultBiome;
        }
        
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
        int[] range = textureHeightIndices[height];
        int result = range[Random.Range(0, range.Length)];
        return result;
    }

    private List<Texture2D> TexturesAsBiomeVisualization()
    {

        for (int i = 0; i < System.Enum.GetValues(typeof(Biome)).Length; i++)
        {
            Biome b = (Biome) i;
            Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            
            Color[] pix = texture.GetPixels();

            Color newColor = Color.white;
            switch (b)
            {
                case Biome.Desert:
                    newColor = Color.yellow;
                    break;
                case Biome.Grassland:
                    newColor = new Color(1f, 0.79f, 0.71f);
                    break;
                case Biome.Savannah:
                    newColor = new Color(1f, 0.47f, 0.32f);
                    break;
                case Biome.Shrubland:
                    newColor = new Color(0.43f, 0.21f, 0.06f);
                    break;
                case Biome.Taiga:
                    newColor = new Color(0.37f, 0.58f, 1f);
                    break;
                case Biome.Tundra:
                    newColor = new Color(0.68f, 0.99f, 1f);
                    break;
                case Biome.TemperateForest:
                    newColor = new Color(0.6f, 1f, 0.58f);
                    break;
                case Biome.TemperateRainforest:
                    newColor = new Color(0.26f, 0.49f, 0.29f);
                    break;
                case Biome.TropicalRainforest:
                    newColor = new Color(0.08f, 0.28f, 0.07f);
                    break;
            }
            for (int j = 0; j < pix.Length; j++)
            {
                pix[j] = newColor;
            }
            
            texture.SetPixels(pix);
            texture.Apply();
            
            allTextures.Add(texture);
            textureBiomeIndices.Add((Biome)i, new []{ i });
        }
        
        return allTextures;
    }

    public List<Texture2D> OrganizeAllTextures()
    {
        // texToIndex.Clear();
        allTextures.Clear();

        // return TexturesAsBiomeVisualization();

        for (int i = 0; i < textureInfo.Count; i++)
        {
            textureOptions.Add(textureInfo[i].textureType, textureInfo[i].texture);
            
        }

        for (int i = 0; i < System.Enum.GetValues(typeof(HexTextureType)).Length; i++)
        {
            allTextures.Add(textureOptions[(HexTextureType)i]);
        }

        for (int i = 0; i < textureBiomeData.Count; i++)
        {
            int[] textureIndices = new int[textureBiomeData[i].textureChoices.Length];
            for (int j = 0; j < textureIndices.Length; j++)
            {
                textureIndices[j] = (int) textureBiomeData[i].textureChoices[j];
            }
            textureBiomeIndices.Add(textureBiomeData[i].biome, textureIndices);
        }
        
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
                textureHeightIndices.Add(j, null);
            }
            int[] textureIndices = new int[textureData[i].textureChoices.Length];
            for (int j = 0; j < textureData[i].textureChoices.Length; j++)
            {
                textureIndices[j] = (int)textureData[i].textureChoices[j];
            }
            for (int j = textureData[i].heightRange.x; j <= textureData[i].heightRange.y; j++)
            {
                textureHeightIndices[j] = textureIndices;
                
                heightOverridesBiome.Add(j, textureData[i].overrideBiome);
            }
        }

        return allTextures;
    }
}
