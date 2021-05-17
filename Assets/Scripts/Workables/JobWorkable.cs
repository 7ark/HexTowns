using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobWorkable : Workable
{
    public override bool DoWork()
    {
        return OnWorkTick(); //Work is never done. It is neverending. You cannot escape it; and so neither shall Peeple.
    }
}
