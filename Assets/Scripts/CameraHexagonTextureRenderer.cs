using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraHexagonTextureRenderer : MonoBehaviour
{
    private Texture2D texture;
    private RenderTexture renderTex;
    public void SetTexture(Texture2D tex, RenderTexture renderTexture)
    {
        texture = tex;
        renderTex = renderTexture;
    }

    private void OnRenderImage(RenderTexture from, RenderTexture to)
    {
        RenderTexture.active = renderTex;

        Vector2Int mouse = Vector2Int.FloorToInt(Mouse.current.position.ReadValue());
        mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        mouse.y = (Screen.height - 1) - Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        Vector2 max = mouse + Vector2.one;
        texture.ReadPixels(Rect.MinMaxRect(mouse.x, mouse.y, max.x, max.y), 0, 0);
    }
}
