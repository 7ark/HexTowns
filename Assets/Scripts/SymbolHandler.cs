using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SymbolType { Destroy }

public class SymbolHandler : MonoBehaviour
{
    [System.Serializable]
    private struct SymbolMaterialData
    {
        public SymbolType SymbolType;
        public Material Mat;
    }
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Billboard symbolQuadPrefab;
    [SerializeField]
    private SymbolMaterialData[] symbolData;

    public static SymbolHandler Instance;

    private Dictionary<SymbolType, Material> symbolToMaterial = new Dictionary<SymbolType, Material>();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < symbolData.Length; i++)
        {
            symbolToMaterial.Add(symbolData[i].SymbolType, symbolData[i].Mat);
        }
    }

    public GameObject DisplaySymbol(SymbolType type, Vector3 position)
    {
        Billboard symbol = Instantiate(symbolQuadPrefab, position, Quaternion.identity);
        symbol.SetCameraInstance(cam);
        symbol.GetComponent<Renderer>().sharedMaterial = symbolToMaterial[type];

        return symbol.gameObject;
    }
}
