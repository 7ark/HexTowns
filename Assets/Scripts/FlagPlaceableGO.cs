using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagPlaceableGO : PlaceableGO
{
    [SerializeField]
    private FlagPlaceable flagPlaceableInstance;

    private void Awake()
    {
        flagPlaceableInstance.Init(gameObject);
    }

    public override Placeable Get()
    {
        return flagPlaceableInstance;
    }
}
