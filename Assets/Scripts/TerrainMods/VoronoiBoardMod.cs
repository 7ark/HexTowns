using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TerrainMods
{
    public class VoronoiBoardMod : IBoardMod
    {
        private Vector2Int[] voronoiPoints;
        private Dictionary<Vector2Int, Biome> biomeLayout;

        public void ApplyModification(HexBoard board) {
            if (biomeLayout == null)
                GenerateBiomesData();
            
            Random.State s = Random.state;

            int seed = HexBoardChunkHandler.Seed;
            int x = board.GridPosition.x;
            int y = board.GridPosition.y;
            
            Biome biome = Biome.None;
            //Setup corners
            int sX = x;
            int sY = y;
            BoardCorners cornerRect = new BoardCorners();
            string seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
            //Debug.Log(seedStringed);
            Random.InitState(System.Convert.ToInt32(seedStringed));

            if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
            {
                biome = GetRandomBiome();
                biomeLayout.Add(new Vector2Int(sX, sY), biome);
            }
            Random.InitState(System.Convert.ToInt32(seedStringed));
            cornerRect.lowerLeft = GetCornerHeight(biome);

            sX = x + 1;
            sY = y;
            seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
            //Debug.Log(seedStringed);
            Random.InitState(System.Convert.ToInt32(seedStringed));
            if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
            {
                biome = GetRandomBiome();
                biomeLayout.Add(new Vector2Int(sX, sY), biome);
            }
            Random.InitState(System.Convert.ToInt32(seedStringed));
            cornerRect.lowerRight = GetCornerHeight(biome);

            sX = x;
            sY = y + 1;
            seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
            //Debug.Log(seedStringed);
            Random.InitState(System.Convert.ToInt32(seedStringed));
            if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
            {
                biome = GetRandomBiome();
                biomeLayout.Add(new Vector2Int(sX, sY), biome);
            }
            Random.InitState(System.Convert.ToInt32(seedStringed));
            cornerRect.upperLeft = GetCornerHeight(biome);

            sX = x + 1;
            sY = y + 1;
            seedStringed = seed.ToString().Substring(0, 2) + (sX < 0 ? sX + 1000 : sX).ToString() + (sY < 0 ? sY + 1000 : sY).ToString();
            //Debug.Log(seedStringed);
            Random.InitState(System.Convert.ToInt32(seedStringed));
            if (!biomeLayout.TryGetValue(new Vector2Int(sX, sY), out biome))
            {
                biome = GetRandomBiome();
                biomeLayout.Add(new Vector2Int(sX, sY), biome);
            }
            Random.InitState(System.Convert.ToInt32(seedStringed));
            cornerRect.upperRight = GetCornerHeight(biome);

            Random.state = s;

            board.CornerHeights = cornerRect;

            if (!biomeLayout.TryGetValue(new Vector2Int(x, y), out biome))
            {
                biome = GetRandomBiome();
                biomeLayout.Add(new Vector2Int(x, y), biome);
            }

            board.BiomeTerrain = biome;
            GenerateTerrainAlgorithm(board, cornerRect, biome);
        }
        
        private void GenerateTerrainAlgorithm(HexBoard board, BoardCorners cornerData, Biome biome) {
            var board2D = board.board2D;
            //Set 4 corners
            board2D[0, 0].SetHeight(cornerData.lowerLeft);
            board2D[board.Size.x - 1, 0].SetHeight(cornerData.lowerRight);
            board2D[0, board.Size.y - 1].SetHeight(cornerData.upperLeft);
            board2D[board.Size.x - 1, board.Size.y - 1].SetHeight(cornerData.upperRight);

            DiamondAlgStep(board2D, new RectInt(0, 0, board.Size.x - 1, board.Size.y - 1), biome);

            var highestPoint = -500;
            foreach (var tile in board.board2D) {
                if(highestPoint < tile.Height) {
                    highestPoint = tile.Height;
                }
            }

            board.HighestPoint = highestPoint;
        }

        private void DiamondAlgStep(HexTile[,] board2D, RectInt diamond, Biome biome, int depth = 0)
        {
            if(diamond.width < 2 || diamond.height < 2)
            {
                return;
            }

            int halfX = diamond.xMin + (diamond.xMax - diamond.xMin) / 2;
            int halfY = diamond.yMin + (diamond.yMax - diamond.yMin) / 2;

            int averageHeight =
                ((board2D[diamond.xMin, diamond.yMin].Height +
                board2D[diamond.xMin, diamond.yMax].Height +
                board2D[diamond.xMax, diamond.yMin].Height +
                board2D[diamond.xMax, diamond.yMax].Height) / 4) + (depth % 6 == 0 ? SpecialRandom(new Vector2Int(-30, 80)) : SpecialRandom(biome));

            board2D[halfX, halfY].SetHeight(averageHeight);

            board2D[halfX, diamond.yMin].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMin].Height, board2D[diamond.xMax, diamond.yMin].Height, 0.5f)) + SpecialRandom(biome));
            board2D[halfX, diamond.yMax].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMax].Height, board2D[diamond.xMax, diamond.yMax].Height, 0.5f)) + SpecialRandom(biome));
            board2D[diamond.xMin, halfY].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMin, diamond.yMin].Height, board2D[diamond.xMin, diamond.yMax].Height, 0.5f)) + SpecialRandom(biome));
            board2D[diamond.xMax, halfY].SetHeight(Mathf.RoundToInt(Mathf.Lerp(board2D[diamond.xMax, diamond.yMin].Height, board2D[diamond.xMax, diamond.yMax].Height, 0.5f)) + SpecialRandom(biome));

            DiamondAlgStep(board2D, new RectInt(diamond.xMin, diamond.yMin, halfX - diamond.xMin, halfY - diamond.yMin), biome, depth + 1);
            DiamondAlgStep(board2D, new RectInt(halfX, diamond.yMin, diamond.xMax - halfX, halfY - diamond.yMin), biome, depth + 1);
            DiamondAlgStep(board2D, new RectInt(diamond.xMin, halfY, halfX - diamond.xMin, diamond.yMax - halfY), biome, depth + 1);
            DiamondAlgStep(board2D, new RectInt(halfX, halfY, diamond.xMax - halfX, diamond.yMax - halfY), biome, depth + 1);
        }
        
        private void GenerateBiomesData() {
            biomeLayout = new Dictionary<Vector2Int, Biome>();
            var boardSize = HexBoardChunkHandler.BoardSize;
            Dictionary<Vector2Int, List<Vector2Int>> voronoiChunks = new Dictionary<Vector2Int, List<Vector2Int>>();
            Dictionary<Vector2Int, Vector2Int> chunkToVoronoiPoint = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, Biome> voronoiToBiome = new Dictionary<Vector2Int, Biome>();

            voronoiPoints = new Vector2Int[boardSize.x * boardSize.y];
            for (int i = 0; i < voronoiPoints.Length; i++)
            {
                voronoiPoints[i] = new Vector2Int(Random.Range(0, boardSize.x), Random.Range(0, boardSize.y));
                if(!voronoiChunks.ContainsKey(voronoiPoints[i]))
                {
                    voronoiChunks.Add(voronoiPoints[i], new List<Vector2Int>());
                    voronoiToBiome.Add(voronoiPoints[i], GetRandomBiome());
                }
            }


            for (int x = 0; x < boardSize.x; x++)
            for (int y = 0; y < boardSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Vector2Int voronoiPointClosestTo = new Vector2Int(0, 0);
                float voronoiDistance = float.MaxValue;
                for (int i = 0; i < voronoiPoints.Length; i++)
                {
                    float dist = (voronoiPoints[i] - pos).magnitude;

                    if(dist < voronoiDistance)
                    {
                        voronoiDistance = dist;
                        voronoiPointClosestTo = voronoiPoints[i];
                    }
                }

                voronoiChunks[voronoiPointClosestTo].Add(pos);
                chunkToVoronoiPoint.Add(pos, voronoiPointClosestTo);
            }

            for (int x = 0; x < boardSize.x; x++)
            for (int y = 0; y < boardSize.y; y++)
            {
                Vector2Int voronoiPoint = chunkToVoronoiPoint[new Vector2Int(x, y)];
                biomeLayout.Add(new Vector2Int(x, y), voronoiToBiome[voronoiPoint]);
            }
        }

        private List<Biome> _lazyBiomeBag;
        private List<Biome> _biomeBag => _lazyBiomeBag ??= FillBag();
        private List<Biome> FillBag() {
            List<Biome> grabBag = new List<Biome>();
            // grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Plains, 6));
            // grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Hills, 3));
            // grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Ocean, 5));
            // grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Mountains, 1));
            // grabBag.AddRange(System.Linq.Enumerable.Repeat(Biome.Forest, 6));
            return grabBag;
        }
        
        private Biome GetRandomBiome() {
            return _biomeBag[Random.Range(0, _biomeBag.Count)];
        }
        
        private int SpecialRandom(Vector2Int range)
        {
            int val = Random.Range(0, 3);
            if(val < 2)
            {
                return 0;
            }
            else
            {
                return Random.Range(range.x, range.y + 1);
            }
        }

        private int SpecialRandom(Biome biome)
        {
            // switch (biome)
            // {
            //     case Biome.Hills:
            //         return Random.Range(-2, 4);
            //     case Biome.Plains:
            //         return Random.Range(-2, 3);
            //     case Biome.Ocean:
            //         return Random.Range(-8, 3);
            //     case Biome.Mountains:
            //         return Random.Range(-2, 13);
            //     case Biome.Desert:
            //         return Random.Range(-5, 6);
            //     case Biome.Forest:
            //         return Random.Range(-1, 4);
            // }

            return 0;
        }
        
        private int GetCornerHeight(Biome biome)
        {
            // switch (biome)
            // {
            //     case Biome.Hills:
            //         const int minHeightHills = 25;
            //         const int maxHeightHills = 161;
            //
            //         return Random.Range(minHeightHills, maxHeightHills);
            //     case Biome.Plains:
            //         const int minHeightPlains = -2;
            //         const int maxHeightPlains = 51;
            //
            //         return Random.Range(minHeightPlains, maxHeightPlains);
            //     case Biome.Ocean:
            //         const int minHeightOcean = -150;
            //         const int maxHeightOcean = 21;
            //
            //         return Random.Range(minHeightOcean, maxHeightOcean);
            //     case Biome.Mountains:
            //         const int minHeightMountains = 50;
            //         const int maxHeightMountains = 301;
            //
            //         if(Random.Range(0, 4) == 0)
            //         {
            //             return Random.Range(minHeightMountains, maxHeightMountains);
            //         }
            //         else
            //         {
            //             return Random.Range(minHeightMountains, maxHeightMountains + 200);
            //         }
            //
            //     case Biome.Desert:
            //         const int minHeightDesert = 20;
            //         const int maxHeightDesert = 61;
            //
            //         return Random.Range(minHeightDesert, maxHeightDesert);
            //     case Biome.Forest:
            //         const int minHeightForest = -5;
            //         const int maxHeightForest = 101;
            //
            //         return Random.Range(minHeightForest, maxHeightForest);
            // }

            return 0;
        }
    }
}