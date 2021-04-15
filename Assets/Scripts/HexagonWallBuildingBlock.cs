using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WallStructureType { None = -1, Solid, Window, Door, Count_Value }
public class HexagonWallBuildingBlock : MonoBehaviour
{
    [System.Serializable]
    private struct WallData
    {
        public WallStructureType wallStructureType;
        public GameObject associatedObject;
    }

    [SerializeField]
    private GameObject leftCorner;
    [SerializeField]
    private GameObject rightCorner;
    [SerializeField]
    private Collider wallCollider;
    [SerializeField]
    private WallData[] wallTypes;
    [SerializeField]
    private Material normalMaterial;
    [SerializeField]
    private Material highlightMaterial;

    private WallStructureType wallType = WallStructureType.Solid;
    public WallStructureType currentWallType
    {
        get
        {
            if(!gameObject.activeSelf)
            {
                return WallStructureType.None;
            }
            return wallType;
        }
        private set
        {
            wallType = value;
        }
    }
    private Dictionary<WallStructureType, GameObject> typeToWall = new Dictionary<WallStructureType, GameObject>();
    private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    private void Awake()
    {
        for (int i = 0; i < wallTypes.Length; i++)
        {
            typeToWall.Add(wallTypes[i].wallStructureType, wallTypes[i].associatedObject);
            meshRenderers.Add(wallTypes[i].associatedObject.GetComponent<MeshRenderer>());
            wallTypes[i].associatedObject.SetActive(false);
        }

        leftCorner.SetActive(false);
        rightCorner.SetActive(false);

        SetMaterial(false);
        ChangeWallTo(currentWallType);
    }

    public void SetMaterial(bool highlight)
    {
        for (int i = 0; i < meshRenderers.Count; i++)
        {
            List<Material> mat = new List<Material>();
            for (int j = 0; j < meshRenderers[i].sharedMaterials.Length; j++)
            {
                mat.Add(highlight ? highlightMaterial : normalMaterial);
            }
            meshRenderers[i].sharedMaterials = mat.ToArray();
        }
    }

    public void SetCornerState(bool left, bool active = true)
    {
        if(left)
        {
            leftCorner.SetActive(active);
        }
        else
        {
            rightCorner.SetActive(active);
        }
    }

    public void SetupForPrefab()
    {
        SetMaterial(false);
        for (int i = 0; i < wallTypes.Length; i++)
        {
            if(!wallTypes[i].associatedObject.activeSelf)
            {
                Destroy(wallTypes[i].associatedObject);
            }
        }

        if(!leftCorner.activeSelf)
        {
            Destroy(leftCorner);
        }

        if (!rightCorner.activeSelf)
        {
            Destroy(rightCorner);
        }

        Destroy(wallCollider.gameObject);
    }

    public void SetColliderActive(bool active)
    {
        wallCollider.gameObject.SetActive(active);
    }

    public void ChangeWallTo(WallStructureType type)
    {
        typeToWall[currentWallType].SetActive(false);

        currentWallType = type;

        typeToWall[currentWallType].SetActive(true);
    }

    public void RotateWallType()
    {
        int val = (int)currentWallType;
        val++;
        if(val >= (int)WallStructureType.Count_Value)
        {
            val = 0;
        }

        ChangeWallTo((WallStructureType)val);
    }
}
