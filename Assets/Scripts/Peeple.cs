using System.Collections;
using System.Collections.Generic;
using MEC;
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
        public bool anyBetterBedsAvailable;
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
            anyBetterBedsAvailable = false;
        }
    }
    private enum PeepleAIState { DoingNothing, Idling, Resting, Moving, DoingJob, EatingFood, Sleeping }

    public static int PeepleCarryingCapacity = 10;

    public PathfindMovement Movement { get; private set; }
    private Workable currentJob;
    public Workable Job { get { return currentJob; } }
    public bool Unemployed { get { return Job == null; } }
    private HexTile home;
    private int homeQuality = 0;
    private Workable currentWorkable = null;
    [SerializeField]
    private PeepleAIState currentAIState = PeepleAIState.DoingNothing;
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
        if (!peepleWorldState.foodAvailable)
        {
            peepleWorldState.eating = false;
        }
        if (peepleWorldState.resting && peepleWorldState.energy >= 100)
        {
            peepleWorldState.resting = false;
        }
        peepleWorldState.anyBetterBedsAvailable = BedTracker.AnyBetterBedsAvailable(homeQuality);
        peepleWorldState.isWorkingHours = GameTime.Instance.CurrentTime - 1 > GameTime.Instance.Sunrise && GameTime.Instance.CurrentTime + 1 < GameTime.Instance.Sunset;
        peepleWorldState.isNight = !GameTime.Instance.IsItLightOutside();
        peepleWorldState.anyJobsAvailable = PeepleJobHandler.Instance.AnyOpenJobs();
        peepleWorldState.hasJob = Job != null;
        if (!peepleWorldState.hasJob && peepleWorldState.location == PeepleLocation.Job)
        {
            peepleWorldState.location = PeepleLocation.Anywhere;
        }
        return peepleWorldState;
    }

    private void SetAIState(PeepleAIState newState)
    {
        if (newState == currentAIState)
        {
            return;
        }
        currentAIState = newState;
        if (currentAIState != PeepleAIState.Resting)
        {
            restingSymbol.SetActive(false);
            peepleWorldState.eating = false;
        }
        if (currentAIState != PeepleAIState.EatingFood)
        {
            peepleWorldState.eating = false;
        }
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
            (worldState) => { return true; },
            (worldState) => { worldState.energy += 2; worldState.hunger++; if (worldState.energy > 100) worldState.energy = 100; worldState.resting = true; },
            TakeBreak);
        PrimitiveTask<PeepleWS> idleTask = new PrimitiveTask<PeepleWS>("Idle",
            (worldState) => { return true; },
            (worldState) => { },
            Idle);
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
            (worldState) => { worldState.eating = true; worldState.hunger -= 12; if (worldState.hunger < 0) { worldState.hunger = 0; worldState.eating = false; } },
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
        PrimitiveTask<PeepleWS> checkForBetterBedTask = new PrimitiveTask<PeepleWS>("CheckForBetterBed",
            (worldState) => { return true; },
            (worldState) => { },
            CheckForBetterBed);

        Task htn = new CompoundTask<PeepleWS>("Peeple",
            new Method<PeepleWS>((ws) => { return ws.foodAvailable && (ws.eating || ws.hunger >= 100); }).AddSubTasks(
                checkForBetterBedTask,
                goHomeAndEatTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isNight; }).AddSubTasks(
                checkForBetterBedTask,
                moveToHomeTask,
                sleepTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                moveOutOfWorkAreaTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                takeBreakIfTiredTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                findJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                moveToJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                doJobTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                relaxTask,
                idleTask
            )
        );

        SetupHTN(htn);
    }

    public int GetHomeQuality()
    {
        return homeQuality;
    }

    protected override void Replanning()
    {
        Movement.CancelCurrentMovement();
        base.Replanning();
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
        if (currentJob == null)
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

    private IEnumerator<float> Eat(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.EatingFood);
        if (!ResourceHandler.Instance.UseResources(ResourceType.Food, 5))
        {
            peepleWorldState.eating = false;
            onComplete(false);
            yield break;
        }

        peepleWorldState.hunger -= 12;
        peepleWorldState.eating = true;
        if (peepleWorldState.hunger < 10)
        {
            peepleWorldState.eating = false;
        }
        if (peepleWorldState.hunger < 0)
        {
            peepleWorldState.hunger = 0;
        }

        yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);

        onComplete(true);
    }

    private IEnumerator<float> CheckForBetterBed(System.Action<bool> onComplete)
    {
        BedTracker.TryGetBetterBed(homeQuality, this);
        onComplete(true);

        yield break;
    }

    private IEnumerator<float> StopResting(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.DoingNothing);
        if (peepleWorldState.resting)
        {
            restingSymbol.SetActive(false);
            peepleWorldState.resting = false;
        }

        onComplete(true);

        yield break;
    }

    private IEnumerator<float> FindJob(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.DoingJob);
        PeepleJobHandler.Instance.FindJob(this);
        peepleWorldState.hasJob = true;

        onComplete(true);

        yield break;
    }

    private IEnumerator<float> MoveOutOfWorkArea(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.Moving);
        HexTile tileToMoveTo = null;
        HashSet<HexTile> tilesSeen = new HashSet<HexTile>();
        Queue<HexTile> currentTiles = new Queue<HexTile>();
        currentTiles.Enqueue(Movement.GetTileOn());
        while (currentTiles.Count > 0)
        {
            HexTile current = currentTiles.Dequeue();
            tilesSeen.Add(current);

            if (!current.WorkArea && !current.CantWalkThrough)
            {
                tileToMoveTo = current;
                break;
            }

            foreach (var neighbor in current.Neighbors) {
                if (!tilesSeen.Contains(neighbor)) {
                    currentTiles.Enqueue(neighbor);
                }
            }
        }

        bool waitToArrive = true;
        Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            if (success)
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

        while (waitToArrive)
        {
            yield return Timing.WaitForOneFrame;
        }

        peepleWorldState.location = PeepleLocation.Anywhere;

        onComplete(true);
    }

    private IEnumerator<float> MoveToHome(System.Action<bool> onComplete)
    {
        if (peepleWorldState.location == PeepleLocation.Home)
        {
            onComplete(true);
            yield break;
        }
        SetAIState(PeepleAIState.Moving);

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
            if (!Movement.IsMoving)
            {
                //What the fuck happened here
                moveSuccess = false;
                break;
            }
            yield return Timing.WaitForOneFrame;
        }

        peepleWorldState.location = PeepleLocation.Home;

        onComplete(moveSuccess);
    }

    public void DoUnofficialJob(Workable workable, System.Action onComplete)
    {
        if (workable == null)
        {
            onComplete();
            return;
        }
        workable.SetWorkLeft();

        Debug.Log("Starting unofficial job");
        Timing.RunCoroutine(UnofficialJobTick(workable, onComplete));
    }

    private IEnumerator<float> UnofficialJobTick(Workable workable, System.Action onComplete)
    {
        Debug.Log("Move to unofficial job site");
        yield return Timing.WaitUntilDone(MoveToJobSite(workable));

        Debug.Log("Arrived at site. Doing work");
        while (true)
        {
            if (workable == null || workable.DoWork() || workable.WorkFinished)
            {
                break;
            }

            yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        }

        Debug.Log("Work done!");
        peepleWorldState.location = PeepleLocation.Anywhere;
        onComplete();
    }

    public IEnumerator<float> MoveToJobSite(Workable workable)
    {
        currentWorkable = currentJob;
        currentJob = workable;
        yield return Timing.WaitUntilDone(MoveToJob(null));
        currentJob = currentWorkable;
        currentWorkable = null;
    }

    private IEnumerator<float> MoveToJob(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.Moving);
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
        while (true)
        {
            if (Job == null)
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
                if (allTiles[jobTileIndex].BuildingOnTile != null && !(Job is JobWorkable))
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
                            if (onComplete != null)
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

                yield return Timing.WaitUntilFalse(() => { return waitingForGoalResults; });
            }

            if (movementCompleted)
            {
                yield break;
            }

        }
    }

    private IEnumerator<float> DoJob(System.Action<bool> onComplete)
    {
        if (Job == null)
        {
            SetAIState(PeepleAIState.DoingNothing);
            onComplete(false);
            yield break;
        }
        else if(Job.WorkFinished)
        {
            //Not sure how we got here but fuck it
            PeepleJobHandler.Instance.RemoveWorkable(Job);
            Job.LeaveWork(this);
            onComplete(false);
        }
        else
        {
            HexTile getNearbyTile = Job.GetAssociatedTileNextToTile(Movement.GetTileOn());
            if(getNearbyTile == null)
            {
                peepleWorldState.location = PeepleLocation.Anywhere;
                onComplete(false);
                yield break;
            }
            Vector3 diff = getNearbyTile.Position - Movement.GetTileOn().Position;
            iTween.RotateTo(gameObject, new Vector3(0, Quaternion.LookRotation(diff).eulerAngles.y, 0), 0.5f);

            SetAIState(PeepleAIState.DoingJob);

            Job.DoWork();
            peepleWorldState.hunger += 2;
            peepleWorldState.energy -= 1;
            if (peepleWorldState.energy < 0)
            {
                peepleWorldState.energy = 0;
            }

            yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
            onComplete(true);
        }
    }

    private IEnumerator<float> TakeBreak(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.Resting);
        if (currentWorkable != null)
        {
            currentJob = currentWorkable;
            peepleWorldState.hasJob = true;
            peepleWorldState.location = PeepleLocation.Anywhere;
        }

        peepleWorldState.energy += 2;
        peepleWorldState.hunger++;
        peepleWorldState.resting = true;
        restingSymbol.SetActive(true);
        if (peepleWorldState.energy > 100)
        {
            peepleWorldState.energy = 100;
            peepleWorldState.resting = false;
            restingSymbol.SetActive(false);
        }

        yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }
    private IEnumerator<float> Idle(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.Idling);
        peepleWorldState.location = PeepleLocation.Anywhere;

        HexTile tileOn = Movement.GetTileOn();
        var tileOptions = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tileOn, 3);

        HexTile tileToMoveTo = null;
        foreach (var option in tileOptions.Shuffle()) {
            if (option.CantWalkThrough || option.BuildingOnTile != null)
            {
                continue;
            }

            tileToMoveTo = option;
            break;
        }

        bool waitingToFinish = true;
        Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            waitingToFinish = false;
        });

        while (waitingToFinish)
        {
            yield return Timing.WaitForOneFrame;
        }

        yield return Timing.WaitForSeconds(Random.Range(1f, 6f));

        onComplete(true);
    }

    private IEnumerator<float> Sleep(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.Sleeping);
        peepleWorldState.energy += 5;
        if (peepleWorldState.energy > 100)
        {
            peepleWorldState.energy = 100;
        }
        peepleWorldState.resting = true;

        yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }
}
