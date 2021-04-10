using System;
using UnityEditor;
using UnityEngine;

public class PreviewTile : MonoBehaviour
{
    public HexTile tile;

    //private void OnDrawGizmosSelected() {
    //    if (tile != null) {
    //        var neighbors = tile.ParentBoard.ChunkHandler.GetNeighbors(tile);
    //        foreach (var neighbor in neighbors) {
    //            if (neighbor != null) {
    //                Debug.DrawRay(neighbor.Position + new Vector3(0, neighbor.Height * HexTile.HEIGHT_STEP),
    //                    Vector3.up * 10f, Color.red);
    //            }
    //        }
    //        
    //        Handles.Label(tile.Position + new Vector3(0,tile.Height * HexTile.HEIGHT_STEP + 1f), $"{tile.Coordinates} : {tile.Index}");
    //    }
    //}
}