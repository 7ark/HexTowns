using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkableGO : MonoBehaviour
{
    [SerializeField]
    private Workable workableInstance;

    public Workable Get()
    {
        return workableInstance;
    }
}
