

using System;
using System.Collections.Generic;
using UnityEngine;

        
namespace TerrainMods
{
    public class VoronoiTileMod : IBoardMod
    {
        struct SingleTileData
        {
            public int index;
            public Vector3 hexCoord;
            public int regionResult;
            public int distanceToCenter;
        }

        struct RegionData
        {
            public Vector3 hexCoord;
            public int regionIndex;
        }
        
        private HexCoordinates[] regionCenters;
        private ComputeShader computeShader;
        
        public VoronoiTileMod(HexCoordinates[] regionCenters, ComputeShader computeShader)
        {
            this.computeShader = computeShader;
            this.regionCenters = regionCenters;
        }
        
        public void ApplyModification(HexBoard hexBoard)
        {
            SingleTileData[] data = new SingleTileData[hexBoard.board.Count];
            SingleTileData[] dataOutput = new SingleTileData[hexBoard.board.Count];
            for (int i = 0; i < hexBoard.board.Count; i++)
            {
                var tile = hexBoard.board[i];
                data[i] = new SingleTileData()
                {
                    index = i,
                    hexCoord = new Vector3(tile.Coordinates.X, tile.Coordinates.Y, tile.Coordinates.Z),
                    regionResult = 0,
                    distanceToCenter = 0
                };
            }
            
            int kernal = computeShader.FindKernel("SetVoronoi");
            
            ComputeBuffer dataBuffer = new ComputeBuffer(data.Length, 24); //4 for index, 4 for height, 12 for vector, 4 for dist to edge
            dataBuffer.SetData(data);
            computeShader.SetBuffer(kernal, "tileData", dataBuffer);

            RegionData[] regionData = new RegionData[regionCenters.Length];
            for (int i = 0; i < regionCenters.Length; i++)
            {
                regionData[i] = new RegionData()
                {
                    hexCoord = new Vector3(regionCenters[i].X, regionCenters[i].Y, regionCenters[i].Z),
                    regionIndex = i
                };
            }
            
            ComputeBuffer regionBuffer = new ComputeBuffer(regionData.Length, 16); //12 for vector, 4 for region index
            regionBuffer.SetData(regionData);
            computeShader.SetBuffer(kernal, "regionsData", regionBuffer);
            
            computeShader.SetInt("regionsDataLength", regionData.Length);
            
            computeShader.Dispatch(kernal, data.Length, 1, 1);
            
            dataBuffer.GetData(dataOutput);

            int highest = 0;

            for (int i = 0; i < dataOutput.Length; i++)
            {
                if (dataOutput[i].distanceToCenter > highest)
                {
                    highest = dataOutput[i].distanceToCenter;
                }
                hexBoard.board[dataOutput[i].index].SetRegion(dataOutput[i].regionResult, dataOutput[i].distanceToCenter);
                hexBoard.board[dataOutput[i].index].SetHeight(dataOutput[i].regionResult);
            }

            hexBoard.FarthestDistanceFromCenter = highest;
            
            dataBuffer.Release();
            regionBuffer.Release();
        }
    }
}