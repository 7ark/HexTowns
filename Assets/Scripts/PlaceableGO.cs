using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceableGO : MonoBehaviour
{
    [SerializeField]
    private Placeable placeableInstance;

    private void Awake()
    {
        placeableInstance.Init(gameObject);
    }

    public virtual Placeable Get()
    {
        return placeableInstance;
    }
}
