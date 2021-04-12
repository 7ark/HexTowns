using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeepleHandler : MonoBehaviour
{
    public const float STANDARD_ACTION_TICK = 1;
    public static PeepleHandler Instance;

    private List<Peeple> allPeeple = new List<Peeple>();
    private float actionTickTimer = 0;

    private void Awake()
    {
        Instance = this;
    }

    public int AddPeepleToExistance(Peeple peeple)
    {
        allPeeple.Add(peeple);

        return allPeeple.Count;
    }

    public Peeple[] GetPeepleOnTiles(HexTile[] tiles)
    {
        List<Peeple> result = new List<Peeple>();

        List<HexTile> tileList = new List<HexTile>(tiles);
        for (int i = 0; i < allPeeple.Count; i++)
        {
            if(tileList.Contains(allPeeple[i].Movement.GetTileOn()))
            {
                result.Add(allPeeple[i]);
            }
        }

        return result.ToArray();
    }

    //private void Update()
    //{
    //    actionTickTimer -= Time.deltaTime;
    //    if(actionTickTimer <= 0)
    //    {
    //        actionTickTimer = STANDARD_ACTION_TICK;
    //
    //        for (int i = 0; i < allPeeple.Count; i++)
    //        {
    //            allPeeple[i].DoLifeTick();
    //        }
    //    }
    //}
}
