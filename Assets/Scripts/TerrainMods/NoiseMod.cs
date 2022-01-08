

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


namespace TerrainMods
{
    public class NoiseMod : IBoardMod
    {
        public void ApplyModification(HexBoard hexBoard)
        {
            float precipitationX = 150;
            float precipitationY = 75;

            float tempX = 450;
            float tempY = -45;

            float oceanX = 900;
            float oceanY = -250;
            
            float scale = 0.03f;
            float oceanCutoff = 0.3f;
            
            for (int i = 0; i < hexBoard.board.Count; i++)
            {
                float heightMod = 400 + (50 - hexBoard.board[i].DistanceToCenterOfRegion) * 10;

                float humidity = GetNoise(precipitationX + hexBoard.board[i].Coordinates.X * 0.01f,
                    precipitationY + hexBoard.board[i].Coordinates.Y * 0.01f, 1, 1);
                
                float temp = GetNoise(tempX + hexBoard.board[i].Coordinates.X * 0.01f,
                    tempY + hexBoard.board[i].Coordinates.Y * 0.01f, 1, 1);
                
                float oceanArea = GetNoise(oceanX + hexBoard.board[i].Coordinates.X * 0.003f,
                    oceanY + hexBoard.board[i].Coordinates.Y * 0.003f, 1, 0.5f);

                heightMod *= (humidity + 0.2f);

                float treeLikelyhood = humidity;
                treeLikelyhood += (temp - 0.5f) * 0.2f;

                // if (humidity > 0.5f)
                // {
                //     heightMod *= (1 - humidity) * 0.5f;
                // }

                heightMod -= oceanArea * 500;
                if (oceanArea > oceanCutoff)
                {
                    heightMod -= (oceanArea - oceanCutoff) * 1500;
                }

                float heightRatio = GetNoise(hexBoard.board[i].Coordinates.X * scale,
                    hexBoard.board[i].Coordinates.Y * scale,   oceanArea > 0.5f ? 3 : 10);
            
                hexBoard.board[i].SetHeight((int)(heightRatio * heightMod - 0));
                
                SetTileBiome(hexBoard.board[i], humidity, temp, heightRatio);

                treeLikelyhood -= hexBoard.board[i].Height * 0.001f;
                
                // if (hexBoard.board[i].Biome == Biome.Shrubland)
                // {
                //     treeLikelyhood += 0.2f;
                // }
                // else if (hexBoard.board[i].Biome == Biome.Grassland)
                // {
                //     treeLikelyhood += 0.1f;
                // }
                
                if (treeLikelyhood > 0.4f && hexBoard.board[i].Height < 150 && hexBoard.board[i].Height > 0 && Random.Range(0f, 1f) < treeLikelyhood && Random.Range(0, (int)((1 - treeLikelyhood) * 10)) == 0)
                {
                    bool cancelPlace = false;
                    TreeType treeType = TreeType.Oak;
                    switch (hexBoard.board[i].Biome)
                    {
                        case Biome.Tundra:
                            treeType = TreeType.Pine_Snow;
                            break;
                        case Biome.Taiga:
                            treeType = TreeType.Pine;
                            break;
                        case Biome.TemperateRainforest:
                            treeType = TreeType.Jungle;
                            cancelPlace = Random.Range(0f, 1f) < 0.8f;
                            break;
                        case Biome.TropicalRainforest:
                            treeType = TreeType.Jungle;
                            cancelPlace = Random.Range(0, 10) == 0;
                            break;
                    }

                    if (!cancelPlace)
                    {
                        hexBoard.AddEnvironmental(hexBoard.board[i], BoardInstancedType.Tree, (int)treeType);
                    }
                }

                if (hexBoard.board[i].Height > 100 && Random.Range(0f, 1f) < 0.1f)
                {
                    hexBoard.AddEnvironmental(hexBoard.board[i], BoardInstancedType.Rock, (int)RockType.Stone);
                }

                float plantLikelyhood = humidity + 0.1f;

                if (temp < 0.2f)
                {
                    plantLikelyhood = 0;
                }

                if (hexBoard.board[i].Biome == Biome.Shrubland || hexBoard.board[i].Biome == Biome.Grassland)
                {
                    plantLikelyhood += 0.1f;
                }


                if (plantLikelyhood > 0.4f && hexBoard.board[i].Height < 150 && hexBoard.board[i].Height > 0 &&
                    Random.Range(0f, 1f) < plantLikelyhood && Random.Range(0, (int) ((1 - plantLikelyhood) * 10)) == 0)
                {
                    if (temp > 0.7f && humidity < 0.7f)
                    {
                        //cactus
                    } 
                    PlantType plantType = PlantType.Bush;
                    if (Random.Range(0f, 1f) < 0.6f)
                    {
                        plantType = PlantType.Fern;
                    }
                    
                    hexBoard.AddEnvironmental(hexBoard.board[i], BoardInstancedType.Plant, (int)plantType);
                }
            }
            
        }

        private void SetTileBiome(HexTile tile, float humidity, float temp, float height)
        {
            if (temp < 0.15f)
            {
                tile.Biome = Biome.Tundra;
            }
            else if (temp < 0.4f)
            {
                if (humidity < 0.2f)
                {
                    tile.Biome = Biome.Grassland;
                }
                else if (humidity < 0.5f)
                {
                    tile.Biome = Biome.Shrubland;
                }
                else
                {
                    tile.Biome = Biome.Taiga;
                }
            }
            else if (temp < 0.7f)
            {
                if (humidity < 0.2f)
                {
                    tile.Biome = Biome.Grassland;
                }
                else if (humidity < 0.4f)
                {
                    tile.Biome = Biome.Shrubland;
                }
                else if (humidity < 0.7f)
                {
                    tile.Biome = Biome.TemperateForest;
                }
                else
                {
                    tile.Biome = Biome.TemperateRainforest;
                }
            }
            else
            {
                if (humidity < 0.2f)
                {
                    tile.Biome = Biome.Desert;
                }
                else if (humidity < 0.7f)
                {
                    tile.Biome = Biome.Savannah;
                }
                else
                {
                    tile.Biome = Biome.TropicalRainforest;
                }
            }
        }

        private float GetNoise(float x, float y, int layers, float exp = 3)
        {
            float seed = HexBoardChunkHandler.Seed;
            float val = 0;
            float mod = 1;
            float oppMod = 1;
            float tot = 0;
            for (int i = 0; i < layers; i++)
            {
                val += mod * Mathf.PerlinNoise(x * oppMod + seed, y * oppMod + seed);

                tot += mod;
                mod *= 0.5f;
                oppMod *= 2f;
            }
        
            val = val / tot;
            if (exp != 1)
            {
                val = Mathf.Pow(val, 3);
            }
            return val;
        }
    }
}