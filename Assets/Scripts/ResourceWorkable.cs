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
    private HexTile tileOn;

    public ResourceWorkable(HexTile tileOn, int workStepsRequired, ResourceType resourceToReceiveOnCompletion, int resourceAmount) : base(workStepsRequired)
    {
        this.tileOn = tileOn;
        this.resourceToReceiveOnCompletion = resourceToReceiveOnCompletion;
        this.resourceAmount = resourceAmount;
    }

    public override void BeginWorking()
    {
        symbol = SymbolHandler.Instance.DisplaySymbol(SymbolType.Destroy, tileOn.Position + new Vector3(0, tileOn.Height * HexTile.HEIGHT_STEP) + new Vector3(0, 6));
        base.BeginWorking();
    }

    protected override void WorkCompleted(bool completedSuccessfully)
    {
        base.WorkCompleted(completedSuccessfully);

        if(completedSuccessfully)
        {
            ResourceHandler.Instance.GainResource(resourceToReceiveOnCompletion, resourceAmount);
            DestroySelf();
        }
        GameObject.Destroy(symbol);
    }

    public override List<HexTile> GetTilesAssociated()
    {
        return base.GetTilesAssociated();
    }
}
