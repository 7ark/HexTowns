using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceWorkable : Workable
{
    [SerializeField]
    private ResourceType resourceToReceiveOnCompletion;
    [SerializeField]
    private int resourceAmount;

    private GameObject symbol;

    public override void BeginWorking()
    {
        symbol = SymbolHandler.Instance.DisplaySymbol(SymbolType.Destroy, transform.position + new Vector3(0, 6));
        symbol.transform.SetParent(transform);
        base.BeginWorking();
    }

    protected override void WorkCompleted()
    {
        base.WorkCompleted();

        ResourceHandler.Instance.GainResource(resourceToReceiveOnCompletion, resourceAmount);
        Destroy(gameObject);
        Destroy(symbol);
    }

    public override HexTile[] GetTilesAssociated()
    {
        return base.GetTilesAssociated();
    }
}
