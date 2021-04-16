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

    public ResourceType ResourceReturn { get { return resourceToReceiveOnCompletion; } }
    public bool AbleToBeHarvested { get; set; }

    public ResourceWorkable(HexTile tileOn, int workStepsRequired, ResourceType resourceToReceiveOnCompletion, int resourceAmount, bool canHarvest = true) : base(workStepsRequired)
    {
        this.tileOn = tileOn;
        this.resourceToReceiveOnCompletion = resourceToReceiveOnCompletion;
        this.resourceAmount = resourceAmount;
        AbleToBeHarvested = canHarvest;
    }

    public override void BeginWorking()
    {
        if(!AbleToBeHarvested)
        {
            return;
        }

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
