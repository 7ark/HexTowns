using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Peeple : HTN_Agent<Peeple.PeepleWS>
{
    public enum PeepleLocation { Job, Home, InWorkArea, Anywhere }
    [System.Serializable]
    public struct PeepleWS
    {
        public PeepleLocation location;
        public bool hasJob;
        public bool anyJobsAvailable;
        public int energy;
        public bool resting;
        public bool hasHome;
        public bool isWorkingHours;
        public bool isNight;
        public int hunger;
        public bool eating;
        public bool foodAvailable;

        public PeepleWS(bool initDefault)
        {
            location = PeepleLocation.Anywhere;
            energy = 100;
            hasJob = false;
            anyJobsAvailable = false;
            resting = false;
            hasHome = false;
            isWorkingHours = true;
            isNight = false;
            hunger = 0;
            foodAvailable = false;
            eating = false;
        }
    }

    public PathfindMovement Movement { get; private set; }
    private Workable currentJob;
    public Workable Job { get { return currentJob; } }
    public bool Unemployed { get { return Job == null; } }
    private HexTile home;
    private int homeQuality = 0;
    [SerializeField]
    private PeepleWS peepleWorldState = new PeepleWS(true);

    private GameObject restingSymbol;

    protected override PeepleWS GetCurrentWorldState()
    {
        if (Movement.GetTileOn().WorkArea)
        {
            peepleWorldState.location = PeepleLocation.InWorkArea;
        }
        peepleWorldState.foodAvailable = ResourceHandler.Instance.IsThereEnoughResource(ResourceType.Food, 5);
        peepleWorldState.isWorkingHours = GameTime.Instance.CurrentTime - 1 > GameTime.Instance.Sunrise && GameTime.Instance.CurrentTime + 1 < GameTime.Instance.Sunset;
        peepleWorldState.isNight = !GameTime.Instance.IsItLightOutside();
        peepleWorldState.anyJobsAvailable = PeepleJobHandler.Instance.AnyOpenJobs();
        peepleWorldState.hasJob = Job != null;
        if(!peepleWorldState.hasJob && peepleWorldState.location == PeepleLocation.Job)
        {
            peepleWorldState.location = PeepleLocation.Anywhere;
        }
        return peepleWorldState;
    }

    public void SetPeepleLocation(PeepleLocation loc)
    {
        peepleWorldState.location = loc;
    }

    private void Awake()
    {
        Movement = GetComponent<PathfindMovement>();

        int id = PeepleHandler.Instance.AddPeepleToExistance(this);
        name = "Peeple #" + id;

        restingSymbol = SymbolHandler.Instance.DisplaySymbol(SymbolType.PeepleRest, transform.position + new Vector3(0, 2));
        restingSymbol.transform.SetParent(transform, true);
        restingSymbol.SetActive(false);

        PrimitiveTask<PeepleWS> moveOutOfWorkAreaTask = new PrimitiveTask<PeepleWS>("MoveOutOfWorkArea",
            (worldState) => { return worldState.location == PeepleLocation.InWorkArea; },
            (worldState) => { worldState.location = PeepleLocation.Anywhere; },
            MoveOutOfWorkArea);
        PrimitiveTask<PeepleWS> relaxTask = new PrimitiveTask<PeepleWS>("Relax",
            (worldState) => { return true ; },
            (worldState) => { worldState.energy += 2; worldState.hunger++; if (worldState.energy > 100) worldState.energy = 100; worldState.resting = true;},
            TakeBreak);
        PrimitiveTask<PeepleWS> sleepTask = new PrimitiveTask<PeepleWS>("Sleep",
            (worldState) => { return true; },
            (worldState) => { worldState.energy += 5; if (worldState.energy > 100) worldState.energy = 100; worldState.resting = true; },
            Sleep);
        PrimitiveTask<PeepleWS> moveToHomeTask = new PrimitiveTask<PeepleWS>("MoveToHome",
            (worldState) => { return worldState.hasHome; },
            (worldState) => { worldState.location = PeepleLocation.Home; },
            MoveToHome);
        CompoundTask<PeepleWS> takeBreakIfTiredTask = new CompoundTask<PeepleWS>("TakeABreak",
            new Method<PeepleWS>((worldState) => { return worldState.energy < 25 || (worldState.resting && worldState.energy < 75); }).AddSubTasks(
                relaxTask
                )
            );
        CompoundTask<PeepleWS> restIndefinitely = new CompoundTask<PeepleWS>("Rest",
            new Method<PeepleWS>((worldState) => { return true; }).AddSubTasks(
                moveToHomeTask,
                relaxTask
                )
            );

        PrimitiveTask<PeepleWS> eatTask = new PrimitiveTask<PeepleWS>("Eat",
            (worldState) => { return true; },
            (worldState) => { worldState.eating = true; worldState.hunger -= 20; if (worldState.hunger < 0) { worldState.hunger = 0; worldState.eating = false; } },
            Eat);
        CompoundTask<PeepleWS> goHomeAndEatTask = new CompoundTask<PeepleWS>("EatFood", //TODO: Change this to go to the nearest food storage
            new Method<PeepleWS>((worldState) => { return true; }).AddSubTasks(
                moveToHomeTask,
                eatTask
                )
            );

        PrimitiveTask<PeepleWS> findJobTask = new PrimitiveTask<PeepleWS>("FindJob",
            (worldState) => { return !worldState.hasJob && worldState.anyJobsAvailable; },
            (worldState) => { worldState.hasJob = true; },
            FindJob);
        PrimitiveTask<PeepleWS> moveToJobTask = new PrimitiveTask<PeepleWS>("MoveToJob",
            (worldState) => { return worldState.hasJob && worldState.location != PeepleLocation.Job; },
            (worldState) => { worldState.location = PeepleLocation.Job; },
            MoveToJob);
        PrimitiveTask<PeepleWS> doJobTask = new PrimitiveTask<PeepleWS>("DoJob",
            (worldState) => { return worldState.hasJob && worldState.location == PeepleLocation.Job; },
            (worldState) => { worldState.hunger += 2; worldState.energy -= 1; if (worldState.energy < 0) worldState.energy = 0; },
            DoJob);
        PrimitiveTask<PeepleWS> stopRestingTask = new PrimitiveTask<PeepleWS>("StopResting",
            (worldState) => { return true; },
            (worldState) => { worldState.resting = false; },
            StopResting);

        Task htn = new CompoundTask<PeepleWS>("Peeple",
            new Method<PeepleWS>((ws) => { return ws.foodAvailable && (ws.eating || ws.hunger >= 100); }).AddSubTasks(
                goHomeAndEatTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isNight; }).AddSubTasks(
                stopRestingTask,
                moveToHomeTask,
                sleepTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                stopRestingTask,
                moveOutOfWorkAreaTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                takeBreakIfTiredTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                stopRestingTask,
                findJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                stopRestingTask,
                moveToJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                stopRestingTask,
                doJobTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                relaxTask
            )
        );

        SetupHTN(htn);
    }

    public int GetHomeQuality()
    {
        return homeQuality;
    }

    public void SetHome(HexTile home, int quality)
    {
        this.home = home;
        homeQuality = quality;
        peepleWorldState.hasHome = true;
    }

    public void SetCurrentJob(Workable job)
    {
        currentJob = job;
        if(currentJob == null)
        {
            peepleWorldState.hasJob = false;
            peepleWorldState.location = PeepleLocation.Anywhere;
        }
        else
        {
            peepleWorldState.hasJob = true;
            peepleWorldState.location = PeepleLocation.Anywhere;
        }
    }

    private IEnumerator Eat(System.Action<bool> onComplete)
    {
        if(!ResourceHandler.Instance.UseResources(ResourceType.Food, 5))
        {
            peepleWorldState.eating = false;
            onComplete(false);
            yield break;
        }

        peepleWorldState.hunger -= 20;
        peepleWorldState.eating = true;
        if(peepleWorldState.hunger < 10)
        {
            peepleWorldState.eating = false;
        }
        if (peepleWorldState.hunger < 0)
        {
            peepleWorldState.hunger = 0;
        }

        yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);

        onComplete(true);
    }

    private IEnumerator StopResting(System.Action<bool> onComplete)
    {
        if(peepleWorldState.resting)
        {
            restingSymbol.SetActive(false);
            peepleWorldState.resting = false;
        }

        onComplete(true);

        yield break;
    }

    private IEnumerator FindJob(System.Action<bool> onComplete)
    {
        PeepleJobHandler.Instance.FindJob(this);
        peepleWorldState.hasJob = true;

        onComplete(true);

        yield break;
    }

    private IEnumerator MoveOutOfWorkArea(System.Action<bool> onComplete)
    {
        HexTile tileToMoveTo = null;
        List<HexTile> tilesSeen = new List<HexTile>();
        Queue<HexTile> currentTiles = new Queue<HexTile>();
        currentTiles.Enqueue(Movement.GetTileOn());
        while(currentTiles.Count > 0)
        {
            HexTile current = currentTiles.Dequeue();
            tilesSeen.Add(current);

            if(!current.WorkArea && !current.CantWalkThrough)
            {
                tileToMoveTo = current;
                break;
            }

            List<HexTile> neighbors = HexBoardChunkHandler.Instance.GetTileNeighbors(current);
            for (int i = 0; i < neighbors.Count; i++)
            {
                if(!tilesSeen.Contains(neighbors[i]))
                {
                    currentTiles.Enqueue(neighbors[i]);
                }
            }
        }

        bool waitToArrive = true;
        Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            if(success)
            {
                waitToArrive = false;
            }
            else
            {
                Debug.LogError("Peeple failed trying to move to tile?? Making a debug line.", gameObject);
                Debug.DrawLine(tileToMoveTo.Position, tileToMoveTo.Position + new Vector3(0, 50), Color.red, 60);
                onComplete(false);
                waitToArrive = false;
            }
        });

        while(waitToArrive)
        {
            yield return null;
        }

        peepleWorldState.location = PeepleLocation.Anywhere;

        onComplete(true);
    }

    private IEnumerator MoveToHome(System.Action<bool> onComplete)
    {
        if(peepleWorldState.location == PeepleLocation.Home)
        {
            onComplete(true);
            yield break;
        }

        bool waitToArrive = true;
        bool moveSuccess = true;
        Movement.SetGoal(home, arrivedComplete: (success) =>
        {
            if (success)
            {
                waitToArrive = false;
                moveSuccess = true;
            }
            else
            {
                //Debug.LogError("Peeple failed trying to move to tile?? Making a debug line.", gameObject);
                //Debug.DrawLine(home.Position, home.Position + new Vector3(0, 50), Color.red, 60);
                moveSuccess = false;
                waitToArrive = false;
            }
        });

        while (waitToArrive)
        {
            yield return null;
        }

        peepleWorldState.location = PeepleLocation.Home;

        onComplete(moveSuccess);
    }

    public void DoUnofficialJob(Workable workable, System.Action onComplete)
    {
        workable.SetWorkLeft();

        Debug.Log("Starting unofficial job");
        StartCoroutine(UnofficialJobTick(workable, onComplete));
    }

    private IEnumerator UnofficialJobTick(Workable workable, System.Action onComplete)
    {
        Debug.Log("Move to unofficial job site");
        yield return MoveToJobSite(workable);

        Debug.Log("Arrived at site. Doing work");
        while (true)
        {
            if (workable == null || workable.DoWork() || workable.WorkFinished)
            {
                break;
            }

            yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        }

        Debug.Log("Work done!");
        peepleWorldState.location = PeepleLocation.Anywhere;
        onComplete();
    }

    public IEnumerator MoveToJobSite(Workable workable)
    {
        Workable currentWorkable = currentJob;
        currentJob = workable;
        yield return MoveToJob(null);
        currentJob = currentWorkable;
    }

    private IEnumerator MoveToJob(System.Action<bool> onComplete)
    {
        if(onComplete != null)
        {
            Debug.Log("Move to job");
        }
        bool movementCompleted = false;
        bool waitingForGoalResults = false;
        int jobTileIndex = 0;

        if (Job == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        List<HexTile> allTiles = new List<HexTile>(currentJob.GetWorkableTiles());
        allTiles.Sort((x, y) => { return Vector3.Distance(x.Position, transform.position).CompareTo(Vector3.Distance(y.Position, transform.position)); });
        while(true)
        {
            if(Job == null)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (jobTileIndex >= allTiles.Count)
            {
                //This would mean we've tried to go to all the tiles, are our mover said it can't get there. We're leaving this job.
                //Debug.LogError("Job " + currentJob.name + " isn't reachable!", currentJob.gameObject);
                currentJob.MarkAsUnreachable();
                currentJob.LeaveWork(this);
                onComplete?.Invoke(false);
                movementCompleted = true;
            }
            else
            {
                if(allTiles[jobTileIndex].BuildingOnTile != null && !(Job is JobWorkable))
                {
                    jobTileIndex++;
                    continue;
                }
                //Try to move to the job site location.
                Job.SetTileWorking(allTiles[jobTileIndex]);
                //Debug.Log("Telling job " + Job.name + " that im gonna work on a tile " + allTiles[jobTileIndex].Coordinates);
                waitingForGoalResults = true;
                Movement.SetGoal(allTiles[jobTileIndex], arrivedComplete: (success) =>
                {
                    waitingForGoalResults = false;
                    if (Job != null)
                    {
                        //If we were able to move there, great! 
                        if (success)
                        {
                            if(onComplete != null)
                            {
                                peepleWorldState.location = PeepleLocation.Job;
                            }
                            onComplete?.Invoke(true);
                            movementCompleted = true;
                        }
                        else
                        {
                            Job.StopWorkingTile(allTiles[jobTileIndex]);
                            //Debug.Log("Telling job " + Job.name + " that im NOT gonna work on a tile " + allTiles[jobTileIndex].Coordinates);

                            //Otherwise we're gonna try a different work site location next tick
                            jobTileIndex++;
                        }
                    }
                    else
                    {
                        //Debug.LogError("Got to job, but its gone!");
                        onComplete?.Invoke(false);
                        movementCompleted = true;
                    }
                });
            }

            if(movementCompleted)
            {
                yield break;
            }

            while(waitingForGoalResults)
            {
                yield return null;
            }
        }
    }

    private IEnumerator DoJob(System.Action<bool> onComplete)
    {
        if(Job == null)
        {
            onComplete(false);
            yield break;
        }

        Job.DoWork();
        peepleWorldState.hunger += 2;
        peepleWorldState.energy -= 1;
        if(peepleWorldState.energy < 0)
        {
            peepleWorldState.energy = 0;
        }

        yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }

    private IEnumerator TakeBreak(System.Action<bool> onComplete)
    {
        peepleWorldState.energy += 2;
        peepleWorldState.hunger++;
        if(peepleWorldState.energy > 100)
        {
            peepleWorldState.energy = 100;
        }
        peepleWorldState.resting = true;
        restingSymbol.SetActive(peepleWorldState.energy < 100);

        yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }

    private IEnumerator Sleep(System.Action<bool> onComplete)
    {
        peepleWorldState.energy += 5;
        if (peepleWorldState.energy > 100)
        {
            peepleWorldState.energy = 100;
        }
        peepleWorldState.resting = true;
        restingSymbol.SetActive(peepleWorldState.energy < 100);

        yield return new WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }

    //public void DoLifeTick()
    //{
    //    if(currentTaskList == null)
    //    {
    //
    //    }
    //
    //    //Work
    //    if (Job == null)
    //    {
    //        //Need to get job
    //        //Debug.Log(name + " is looking for a job");
    //        if(PeepleJobHandler.Instance.FindJob(this))
    //        {
    //            Debug.Log(name + " found job at " + Job.name);
    //            DoJobTick();
    //        }
    //    }
    //    else
    //    {
    //        DoJobTick();
    //    }
    //
    //    if(Unemployed)
    //    {
    //        
    //    }
    //}
    //
    //private void DoJobTick()
    //{
    //    //If we have a job
    //    if (Job != null)
    //    {
    //        //If we're at the job site location, do our work
    //        if (atJob)
    //        {
    //            Debug.Log(name + " is at job " + Job.name + " and did one unit of work");
    //            if(currentJob.DoWork())
    //            {
    //                atJob = false;
    //            }
    //        }
    //        else if (!movingToJob) //Otherwise we need to walk over to the job
    //        {
    //            Debug.Log(name + " isn't at their job, so they're moving towards it.");
    //            //We get all the tiles that are workable
    //            movingToJob = true;
    //            List<HexTile> allTiles = new List<HexTile>(currentJob.GetWorkableTiles());
    //            allTiles.Sort((x, y) => { return Vector3.Distance(x.Position, transform.position).CompareTo(Vector3.Distance(y.Position, transform.position)); });
    //            if (jobTileIndex >= allTiles.Count)
    //            {
    //                //This would mean we've tried to go to all the tiles, are our mover said it can't get there. We're leaving this job.
    //                Debug.LogError("Job " + currentJob.name + " isn't reachable!", currentJob.gameObject);
    //                currentJob.MarkAsUnreachable();
    //                currentJob.LeaveWork(this);
    //            }
    //            else
    //            {
    //                //Try to move to the job site location.
    //                Job.SetTileWorking(allTiles[jobTileIndex]);
    //                Debug.Log("Telling job " + Job.name + " that im gonna work on a tile " + allTiles[jobTileIndex].Coordinates);
    //                Movement.SetGoal(allTiles[jobTileIndex], arrivedComplete: (success) =>
    //                {
    //                    if(Job != null)
    //                    {
    //                        //If we were able to move there, great! 
    //                        if (success)
    //                        {
    //                            atJob = true;
    //                            movingToJob = false;
    //
    //                        }
    //                        else
    //                        {
    //                            Job.StopWorkingTile(allTiles[jobTileIndex]);
    //                            Debug.Log("Telling job " + Job.name + " that im NOT gonna work on a tile " + allTiles[jobTileIndex].Coordinates);
    //
    //                            //Otherwise we're gonna try a different work site location next tick
    //                            jobTileIndex++;
    //                            movingToJob = false;
    //                        }
    //                    }
    //                    else
    //                    {
    //                        Debug.LogError("Got to job, but its gone!");
    //                    }
    //                });
    //            }
    //        }
    //        else if(movingToJob)
    //        {
    //            movingToJob = Movement.IsMoving;
    //            if(!movingToJob)
    //            {
    //                HexTile[] allTiles = currentJob.GetWorkableTiles();
    //                Job.StopWorkingTile(allTiles[jobTileIndex]);
    //                jobTileIndex = 0;
    //            }
    //        }
    //    }
    //}

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
