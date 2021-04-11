using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Peeple : MonoBehaviour
{
    public PathfindMovement Movement { get; private set; }
    private Workable currentJob;
    private bool atJob = false;
    private bool movingToJob = false;
    private int jobTileIndex = 0;
    public Workable Job { get { return currentJob; } }
    public bool Unemployed { get { return Job == null; } }

    private void Awake()
    {
        Movement = GetComponent<PathfindMovement>();

        int id = PeepleHandler.Instance.AddPeepleToExistance(this);
        name = "Peeple #" + id;
    }

    public void SetCurrentJob(Workable job)
    {
        currentJob = job;
        if(currentJob == null)
        {
            atJob = false;
            if(movingToJob)
            {
                Movement.SetGoal(Movement.GetTileOn());
            }
            movingToJob = false;
        }
        jobTileIndex = 0;
    }

    public void DoLifeTick()
    {
        //Work
        if (Job == null)
        {
            //Need to get job
            //Debug.Log(name + " is looking for a job");
            if(PeepleJobHandler.Instance.FindJob(this))
            {
                Debug.Log(name + " found job at " + Job.name);
                DoJobTick();
            }
        }
        else
        {
            DoJobTick();
        }

        if(Unemployed)
        {
            
        }
    }

    private void DoJobTick()
    {
        //If we have a job
        if (Job != null)
        {
            //If we're at the job site location, do our work
            if (atJob)
            {
                Debug.Log(name + " is at job " + Job.name + " and did one unit of work");
                if(currentJob.DoWork())
                {
                    atJob = false;
                }
            }
            else if (!movingToJob) //Otherwise we need to walk over to the job
            {
                Debug.Log(name + " isn't at their job, so they're moving towards it.");
                //We get all the tiles that are workable
                movingToJob = true;
                List<HexTile> allTiles = new List<HexTile>(currentJob.GetWorkableTiles());
                allTiles.Sort((x, y) => { return Vector3.Distance(x.Position, transform.position).CompareTo(Vector3.Distance(y.Position, transform.position)); });
                if (jobTileIndex >= allTiles.Count)
                {
                    //This would mean we've tried to go to all the tiles, are our mover said it can't get there. We're leaving this job.
                    Debug.LogError("Job " + currentJob.name + " isn't reachable!", currentJob.gameObject);
                    currentJob.MarkAsUnreachable();
                    currentJob.LeaveWork(this);
                }
                else
                {
                    //Try to move to the job site location.
                    Job.SetTileWorking(allTiles[jobTileIndex]);
                    Debug.Log("Telling job " + Job.name + " that im gonna work on a tile " + allTiles[jobTileIndex].Coordinates);
                    Movement.SetGoal(allTiles[jobTileIndex], arrivedComplete: (success) =>
                    {
                        if(Job != null)
                        {
                            //If we were able to move there, great! 
                            if (success)
                            {
                                atJob = true;
                                movingToJob = false;

                            }
                            else
                            {
                                Job.StopWorkingTile(allTiles[jobTileIndex]);
                                Debug.Log("Telling job " + Job.name + " that im NOT gonna work on a tile " + allTiles[jobTileIndex].Coordinates);

                                //Otherwise we're gonna try a different work site location next tick
                                jobTileIndex++;
                                movingToJob = false;
                            }
                        }
                        else
                        {
                            Debug.LogError("Got to job, but its gone!");
                        }
                    });
                }
            }
            else if(movingToJob)
            {
                movingToJob = Movement.IsMoving;
                if(!movingToJob)
                {
                    HexTile[] allTiles = currentJob.GetWorkableTiles();
                    Job.StopWorkingTile(allTiles[jobTileIndex]);
                    jobTileIndex = 0;
                }
            }
        }
    }

    //private void Update()
    //{
    //    //Work
    //    if(Job == null)
    //    {
    //        //Need to get job
    //        if(PeepleJobHandler.Instance.FindJob(this))
    //        {
    //            if (atJob)
    //            {
    //                if (currentJob.DoWork())
    //                {
    //                    currentJob = null;
    //                    atJob = false;
    //                }
    //            }
    //            else if (!movingToJob)
    //            {
    //                movingToJob = true;
    //                Movement.SetGoal(currentJob.GetTileAssociated(), arrivedComplete: () => { atJob = true; movingToJob = false; });
    //            }
    //        }
    //    }
    //}

    //public void DoWorkTick()
    //{
    //    if(Job == null)
    //    {
    //        return;
    //    }
    //
    //    if(atJob)
    //    {
    //        if(currentJob.DoWork())
    //        {
    //            currentJob = null;
    //            atJob = false;
    //        }
    //    }
    //    else if(!movingToJob)
    //    {
    //        movingToJob = true;
    //        Movement.SetGoal(currentJob.GetTileAssociated(), arrivedComplete: () => { atJob = true; movingToJob = false; });
    //    }
    //}
}
