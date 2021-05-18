using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeepleJobHandler : MonoBehaviour
{
    public static PeepleJobHandler Instance;

    private List<Workable> workables = new List<Workable>();

    private void Awake()
    {
        Instance = this;
    }

    public void AddWorkable(Workable work)
    {
        workables.Add(work);
    }

    public void RemoveWorkable(Workable work)
    {
        if(workables.Contains(work))
        {
            workables.Remove(work);
        }
    }

    public bool AnyJobsNeedResources()
    {
        for (int i = workables.Count - 1; i >= 0; i--)
        {
            if (workables[i] == null)
            {
                workables.RemoveAt(i);
            }
        }
        List<Workable> jobsAvailable = new List<Workable>(workables);

        for (int i = 0; i < jobsAvailable.Count; i++)
        {
            if (jobsAvailable[i].RequiresResources)
            {
                return true;
            }
        }

        return false;
    }

    public bool AnyJobsWaitingOnResourcesHaveResources()
    {
        for (int i = workables.Count - 1; i >= 0; i--)
        {
            if (workables[i] == null)
            {
                workables.RemoveAt(i);
            }
        }
        List<Workable> jobsAvailable = new List<Workable>(workables);

        for (int i = 0; i < jobsAvailable.Count; i++)
        {
            if (jobsAvailable[i].RequiresResources)
            {
                bool good = true;
                foreach(var key in jobsAvailable[i].ResourcesNeeded.Keys)
                {
                    if(!ResourceHandler.Instance.IsThereEnoughResource(key, jobsAvailable[i].ResourcesNeeded[key]))
                    {
                        good = false;
                    }
                }

                if(good)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public Workable GetJobThatNeedsResources()
    {
        for (int i = workables.Count - 1; i >= 0; i--)
        {
            if (workables[i] == null)
            {
                workables.RemoveAt(i);
            }
        }
        List<Workable> jobsAvailable = new List<Workable>(workables);

        for (int i = 0; i < jobsAvailable.Count; i++)
        {
            if (jobsAvailable[i].RequiresResources)
            {
                return jobsAvailable[i];
            }
        }

        return null;
    }

    public bool AnyOpenJobs()
    {
        for (int i = workables.Count - 1; i >= 0; i--)
        {
            if (workables[i] == null)
            {
                workables.RemoveAt(i);
            }
        }
        List<Workable> jobsAvailable = new List<Workable>(workables);

        for (int i = 0; i < jobsAvailable.Count; i++)
        {
            if (!jobsAvailable[i].Unreachable && jobsAvailable[i].WorkSlotsAvailable > 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool FindJob(Peeple peeple)
    {
        for (int i = workables.Count - 1; i >= 0; i--)
        {
            if(workables[i] == null)
            {
                workables.RemoveAt(i);
            }
        }
        List<Workable> jobsAvailable = new List<Workable>(workables);
        jobsAvailable.Sort((x, y) => 
        { 
            return Vector3.Distance(x.GetClosestAssociatedTile(peeple.transform.position).Position, peeple.transform.position).CompareTo(Vector3.Distance(y.GetClosestAssociatedTile(peeple.transform.position).Position, peeple.transform.position)); 
        });

        for (int i = 0; i < jobsAvailable.Count; i++)
        {
            if(!jobsAvailable[i].Unreachable && jobsAvailable[i].WorkSlotsAvailable > 0)
            {
                jobsAvailable[i].AssignWorker(peeple);
                return true;
            }
        }

        return false;
    }
}
