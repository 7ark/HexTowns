using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraHexagonTextureRenderer : MonoBehaviour
{
    private Texture2D texture;
    private RenderTexture renderTex1;
    private RenderTexture renderTex2;
    int toUse = 1;
    public void SetTexture(Texture2D tex, RenderTexture renderTexture1, RenderTexture renderTexture2)
    {
        texture = tex;
        renderTex1 = renderTexture1;
        renderTex2 = renderTexture2;
    }

    public void SetToUse(int val)
    {
        toUse = val;
    }

    public RenderTexture Swap()
    {
        if(toUse == 1)
        {
            toUse = 2;
            return renderTex1;
        }
        else
        {
            toUse = 1;
            return renderTex2;
        }
    }

    private void OnRenderImage(RenderTexture from, RenderTexture to)
    {
        RenderTexture.active = toUse == 1 ? renderTex1 : renderTex2;

        Vector2Int mouse = Vector2Int.FloorToInt(Mouse.current.position.ReadValue());
        mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width - 1);
        mouse.y = (Screen.height - 1) - Mathf.Clamp(mouse.y, 0, Screen.height - 2);
        Vector2 max = mouse + Vector2.one;
        texture.ReadPixels(Rect.MinMaxRect(mouse.x, mouse.y, max.x, max.y), 0, 0);
    }
}
