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
        public bool doesJobRequireResources;
        public bool anyBetterBedsAvailable;
        private int energyBacking;
        public int energy { get { return energyBacking; } set { energyBacking = value; if (energyBacking < 0) energyBacking = 0; } }
        public bool hasHome;
        public bool isWorkingHours;
        public bool isNight;
        public int hunger;
        public bool anyResourcesNeedStoring;

        public PeepleWS(bool initDefault)
        {
            location = PeepleLocation.Anywhere;
            energyBacking = 100;
            hasJob = false;
            anyJobsAvailable = false;
            hasHome = false;
            isWorkingHours = true;
            isNight = false;
            hunger = 0;
            anyBetterBedsAvailable = false;
            doesJobRequireResources = false;
            anyResourcesNeedStoring = false;
        }
    }
    private enum PeepleAIState { DoingNothing, Idling, Resting, Moving, DoingJob, EatingFood, Sleeping }

    public static int PeepleCarryingCapacity = 1;

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
    [SerializeField]
    private List<string> PEEPLE_LOG = new List<string>();

    public void AddLog(string log)
    {
        PEEPLE_LOG.Add(log);

        while(PEEPLE_LOG.Count > 2000)
        {
            PEEPLE_LOG.RemoveAt(0);
        }
    }

    private GameObject restingSymbol;
    private ResourceIndividual resourceHeldBacking = null;
    public ResourceIndividual ResourceHolding
    {
        get
        {
            return resourceHeldBacking;
        }
        set
        {
            if(resourceHeldBacking != null)
            {
                resourceHeldBacking.Parent = null;
                ResourceHandler.Instance.DropResourceNearby(resourceHeldBacking, Movement.GetTileOn());
            }

            resourceHeldBacking = value;
            if (value != null)
            {
                resourceHeldBacking.Parent = gameObject;
                resourceHeldBacking.FollowOffset = new Vector3(0, 2);
            }
        }
    }

    public void ResourceHeldPlacing()
    {
        if(resourceHeldBacking != null)
        {
            resourceHeldBacking.Parent = null;
            resourceHeldBacking = null;
        }
    }


    private struct SeenData<T>
    {
        public float TimeSeen;
        public T Data;
    }

    //Memory
    private class PeepleMemory
    {
        private const int SIGHT_RANGE = 10;

        //Track storage locations
        private Dictionary<ResourceType, HashSet<HexTile>> seenStorages = new Dictionary<ResourceType, HashSet<HexTile>>();
        private Dictionary<HexTile, float> storageLastSeen = new Dictionary<HexTile, float>();

        //Track seen resources
        private Dictionary<ResourceType, HashSet<HexTile>> seenResources = new Dictionary<ResourceType, HashSet<HexTile>>();
        private Dictionary<HexTile, float> resourceLastSeen = new Dictionary<HexTile, float>();

        //Track seen job sites
        private HashSet<HexTile> seenJobSites = new HashSet<HexTile>();

        //Track seen Peeple

        public PeepleMemory()
        {
            for (int i = 1; i < System.Enum.GetValues(typeof(ResourceType)).Length; i++)
            {
                seenStorages.Add((ResourceType)i , new HashSet<HexTile>());
                seenResources.Add((ResourceType)i, new HashSet<HexTile>());
            }
        }

        public void UpdateAtTile(Peeple peeple, HexTile tile)
        {
            var tilesAround = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tile, SIGHT_RANGE);

            float timeSnapshot = GameTime.Instance.GetTimeSnapshot();

            foreach (var tileNearby in tilesAround)
            {
                //Storage
                foreach (var key in seenStorages.Keys)
                {
                    if(seenStorages[key].Contains(tileNearby) && !tileNearby.IsStorageTile)
                    {
                        seenStorages[key].Remove(tileNearby);
                        storageLastSeen.Remove(tileNearby);
                    }
                }

                if(tileNearby.IsStorageTile)
                {
                    ResourceType resource = StorageTracker.GetStorageAt(tileNearby).resourceType;
                    if(!seenStorages[resource].Contains(tileNearby))
                    {
                        seenStorages[resource].Add(tileNearby);
                        storageLastSeen.Add(tileNearby, timeSnapshot);
                    }
                    else
                    {
                        storageLastSeen[tileNearby] = timeSnapshot;
                    }
                }

                //Resources
                ResourcePhysicalData resourceData = ResourceHandler.Instance.PeekResourcesAtLocation(tileNearby);

                if (resourceData.ResourceType != ResourceType.Flags)
                {
                    foreach (var key in seenResources.Keys)
                    {
                        if (seenResources[key].Contains(tileNearby) && resourceData.Resources.Count <= 0)
                        {
                            seenResources[key].Remove(tileNearby);
                            resourceLastSeen.Remove(tileNearby);
                        }
                    }

                    if (resourceData.Resources.Count > 0)
                    {
                        if (!seenResources[resourceData.ResourceType].Contains(tileNearby))
                        {
                            seenResources[resourceData.ResourceType].Add(tileNearby);
                            if (resourceLastSeen.ContainsKey(tileNearby))
                            {
                                resourceLastSeen[tileNearby] = timeSnapshot;
                            }
                            else
                            {
                                resourceLastSeen.Add(tileNearby, timeSnapshot);
                            }
                        }
                        else
                        {
                            resourceLastSeen[tileNearby] = timeSnapshot;
                        }
                    }
                }

                //Job sites

                HashSet<HexTile> toRemove = new HashSet<HexTile>();
                foreach(var jobTile in seenJobSites)
                {
                    if(jobTile.IsStorageTile)
                    {
                        toRemove.Add(jobTile);
                    }
                }
                foreach(var jobTile in toRemove)
                {
                    seenJobSites.Remove(jobTile);
                }

                if (peeple.Job != null && peeple.Job.ResourcePiles.ContainsValue(tileNearby))
                {
                    if(tileNearby.IsStorageTile)
                    {
                        Debug.LogError("Somehow a storage tile is being added as a job resource pile");
                        Debug.Break();
                    }
                    seenJobSites.Add(tileNearby);
                }
            }

        }

        public void FlushJobSiteMemory(Workable specificJob = null)
        {
            if(specificJob == null)
            {
                seenJobSites.Clear();
            }
            else
            {
                foreach(var jobTile in specificJob.ResourcePiles.Values)
                {
                    if(seenJobSites.Contains(jobTile))
                    {
                        seenJobSites.Remove(jobTile);
                    }
                }
            }
        }

        public HashSet<HexTile> GetSeenJobSites()
        {
            return seenJobSites;
        }

        public HashSet<HexTile> GetSeenStorageLocations(ResourceType type)
        {
            return seenStorages[type];
        }

        public HashSet<HexTile> GetSeenResourceLocations(ResourceType type)
        {
            return seenResources[type];
        }

        public HashSet<HexTile> GetAllSeenResourceLocations()
        {
            HashSet<HexTile> result = new HashSet<HexTile>();

            foreach(var tiles in seenResources.Values)
            {
                result.UnionWith(tiles);
            }

            return result;
        }

        public HashSet<HexTile> GetAllSeenStorageLocations()
        {
            HashSet<HexTile> result = new HashSet<HexTile>();

            foreach (var tiles in seenStorages.Values)
            {
                result.UnionWith(tiles);
            }

            return result;
        }

        public HashSet<HexTile> GetSeenCombinedResourceLocations(ResourceType type)
        {
            //TODO: Do this better and more efficiently
            HashSet<HexTile> combined = new HashSet<HexTile>(seenStorages[type]);
            combined.UnionWith(new HashSet<HexTile>(seenResources[type]));
            return combined;
        }

        public void UpdateWithKnowledgeFromOther(PeepleMemory otherMemory, ResourceType? specificType = null)
        {
            if(specificType != null)
            {
                UpdateKnowledge(otherMemory, specificType.Value);
            }
            else
            {
                foreach(var type in seenStorages.Keys)
                {
                    UpdateKnowledge(otherMemory, type);
                }
            }
        }

        private void UpdateKnowledge(PeepleMemory memory, ResourceType type)
        {
            foreach(var tile in memory.seenStorages[type])
            {
                //Do we have knowledge of this tile?
                if(storageLastSeen.ContainsKey(tile))
                {
                    //Is their data more updated?
                    if (memory.storageLastSeen[tile] > storageLastSeen[tile])
                    {
                        //Update ours date to confirm
                        storageLastSeen[tile] = memory.storageLastSeen[tile];
                    }
                }
                else
                {
                    //Add this new data to our knowledge
                    seenStorages[type].Add(tile);
                    storageLastSeen.Add(tile, memory.storageLastSeen[tile]);
                }
            }

            foreach (var tile in memory.seenResources[type])
            {
                //Do we have knowledge of this tile?
                if (resourceLastSeen.ContainsKey(tile))
                {
                    //Is their data more updated?
                    if (memory.resourceLastSeen[tile] > resourceLastSeen[tile])
                    {
                        //Update ours date to confirm
                        resourceLastSeen[tile] = memory.resourceLastSeen[tile];
                    }
                }
                else
                {
                    //Add this new data to our knowledge
                    seenResources[type].Add(tile);
                    resourceLastSeen.Add(tile, memory.resourceLastSeen[tile]);
                }
            }
        }
    }
    private const float MEMORY_UPDATE_RATE = 2f;
    private float memoryTimer = 0;
    private PeepleMemory memory = new PeepleMemory();

    protected override PeepleWS GetCurrentWorldState()
    {
        if (Movement.GetTileOn().WorkArea)
        {
            peepleWorldState.location = PeepleLocation.InWorkArea;
        }

        HashSet<HexTile> seenExclusions = memory.GetSeenJobSites();
        HashSet<HexTile> seenRes = memory.GetAllSeenResourceLocations();
        int seenResourcesCount = seenRes.Count;
        foreach(var tile in seenRes)
        {
            if(seenExclusions.Contains(tile))
            {
                seenResourcesCount--;
            }
        }
        peepleWorldState.anyResourcesNeedStoring = seenResourcesCount > 0 || ResourceHolding != null;
        peepleWorldState.doesJobRequireResources = Job != null && Job.RequiresResources;
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
            (worldState) => { worldState.energy += 2; worldState.hunger++; if (worldState.energy > 100) worldState.energy = 100;  },
            TakeBreak);
        PrimitiveTask<PeepleWS> idleTask = new PrimitiveTask<PeepleWS>("Idle",
            (worldState) => { return true; },
            (worldState) => { },
            Idle);
        PrimitiveTask<PeepleWS> sleepTask = new PrimitiveTask<PeepleWS>("Sleep",
            (worldState) => { return true; },
            (worldState) => { worldState.energy += 5; if (worldState.energy > 100) worldState.energy = 100;  },
            Sleep);
        PrimitiveTask<PeepleWS> moveToHomeTask = new PrimitiveTask<PeepleWS>("MoveToHome",
            (worldState) => { return worldState.hasHome; },
            (worldState) => { worldState.location = PeepleLocation.Home; },
            MoveToHome);
        CompoundTask<PeepleWS> takeBreakIfTiredTask = new CompoundTask<PeepleWS>("TakeABreak",
            new Method<PeepleWS>((worldState) => { return worldState.energy < 10; }).AddSubTasks(
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
            (worldState) => {  worldState.hunger = 0; },
            EatFoodInHand);

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
        PrimitiveTask<PeepleWS> getResourcesForJobTask = new PrimitiveTask<PeepleWS>("GetResourcesForJob",
            (worldState) => { return worldState.doesJobRequireResources; },
            (worldState) => { worldState.hunger += 1; worldState.energy -= 1; if (worldState.energy < 0) worldState.energy = 0; },
            CollectResourcesForJob);
        PrimitiveTask<PeepleWS> moveResourcesToStorage = new PrimitiveTask<PeepleWS>("MoveResourcesToStorage",
            (worldState) => { return worldState.anyResourcesNeedStoring; },
            (worldState) => { },
            MoveResourcesToStorage);

        PrimitiveTask<PeepleWS> checkForBetterBedTask = new PrimitiveTask<PeepleWS>("CheckForBetterBed",
            (worldState) => { return true; },
            (worldState) => { },
            CheckForBetterBed);
        PrimitiveTask<PeepleWS> findFoodTask = new PrimitiveTask<PeepleWS>("FindFoodTask",
            (worldState) => { return true; },
            (worldState) => { },
            FindAndTakeFoodResource);

        Task htn = new CompoundTask<PeepleWS>("Peeple",
            new Method<PeepleWS>((ws) => { return ws.hunger > 90; }).AddSubTasks(
                findFoodTask,
                moveToHomeTask, //TODO: Change this to be nearest table? Or home table
                eatTask
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
                //TODO: Move to job board
                findJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours && ws.doesJobRequireResources; }).AddSubTasks(
                getResourcesForJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                moveToJobTask
            ),
            new Method<PeepleWS>((ws) => { return ws.isWorkingHours; }).AddSubTasks(
                doJobTask
            ),
            new Method<PeepleWS>().AddSubTasks(
                moveResourcesToStorage
            ),
            new Method<PeepleWS>().AddSubTasks(
                relaxTask,
                idleTask
            )
        );

        SetupHTN(htn);
    }

    protected override void Update()
    {
        base.Update();

        memoryTimer -= Time.deltaTime;
        if(memoryTimer <= 0)
        {
            memory.UpdateAtTile(this, Movement.GetTileOn());
            memoryTimer = MEMORY_UPDATE_RATE;
        }
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
            if(job is Placeable)
            {
                memory.FlushJobSiteMemory();
                Workable jobGotten = job;
                currentJob.OnWorkFinished += (success) => { memory.FlushJobSiteMemory(jobGotten); };
            }
            memory.UpdateAtTile(this, Movement.GetTileOn());
            peepleWorldState.hasJob = true;
            peepleWorldState.location = PeepleLocation.Anywhere;
        }
    }

    private IEnumerator<float> EatFoodInHand(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.EatingFood);

        if(ResourceHolding != null && ResourceHolding.Type == ResourceType.Food)
        {
            ResourceHandler.Instance.ConsumeResource(ResourceHolding);
            ResourceHeldPlacing();

            peepleWorldState.hunger = 0;

            yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);

            SetAIState(PeepleAIState.DoingNothing);

            onComplete(true);
        }
        else
        {
            onComplete(false);
        }
    }

    private IEnumerator<float> CheckForBetterBed(System.Action<bool> onComplete)
    {
        BedTracker.TryGetBetterBed(homeQuality, this);
        onComplete(true);

        yield break;
    }

    private IEnumerator<float> FindJob(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.DoingJob);
        PeepleJobHandler.Instance.FindJob(this);
        if(Job != null)
        {
            peepleWorldState.hasJob = true;
            onComplete(true);
        }
        else
        {
            onComplete(false);
        }

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

    private IEnumerator<float> FindAndTakeFoodResource(System.Action<bool> onComplete)
    {
        yield return Timing.WaitUntilDone(FindAndTakeResource(ResourceType.Food, onComplete, memory.GetSeenJobSites()));
    }
    private IEnumerator<float> FindAndTakeWoodResource(System.Action<bool> onComplete)
    {
        yield return Timing.WaitUntilDone(FindAndTakeResource(ResourceType.Wood, onComplete, memory.GetSeenJobSites()));
    }
    private IEnumerator<float> FindAndTakeStoneResource(System.Action<bool> onComplete)
    {
        yield return Timing.WaitUntilDone(FindAndTakeResource(ResourceType.Stone, onComplete, memory.GetSeenJobSites()));
    }

    private IEnumerator<float> FindAndTakeResource(ResourceType type, System.Action<bool> onComplete, HashSet<HexTile> exclusions = null)
    {
        AddLog("Starting to find resource");
        if (exclusions == null)
        {
            exclusions = new HashSet<HexTile>();
        }
        List<HexTile> resourceLocations = new List<HexTile>(memory.GetSeenResourceLocations(type));
        HexTile tileOn = Movement.GetTileOn();
        resourceLocations.Sort((x, y) => { return HexCoordinates.HexDistance(tileOn.Coordinates, x.Coordinates).CompareTo(HexCoordinates.HexDistance(tileOn.Coordinates, y.Coordinates)); });

        int impatience = 0;

        AddLog("We know of " + resourceLocations.Count + " locations with the " + type + " resource");
        while (resourceLocations.Count > 0)
        {
            if(exclusions.Contains(resourceLocations[0]))
            {
                AddLog("Location is on exclusion list. Not checking");
                resourceLocations.RemoveAt(0);
                continue;
            }
            //exclusions.Add(resourceLocations[0]);

            peepleWorldState.hunger++;
            peepleWorldState.energy--;

            AddLog("Checking to move");
            int safety = 0;
            while(HexCoordinates.HexDistance(Movement.GetTileOn().Coordinates, resourceLocations[0].Coordinates) > 2)
            {
                safety++;
                if(safety >= 10)
                {
                    AddLog("For some reason we couldnt arrive");
                    Debug.LogError("Tried to walk to tile 10 times and failed!");
                    onComplete(false);

                    yield break;
                }
                AddLog("Moving to a resource");
                Movement.SetGoal(resourceLocations[0].Neighbors[Random.Range(0, resourceLocations[0].Neighbors.Count)]);

                yield return Timing.WaitUntilFalse(() => { return Movement.IsMoving; });
            }
            AddLog("Finished Moving");

            if (HexCoordinates.HexDistance(Movement.GetTileOn().Coordinates, resourceLocations[0].Coordinates) <= 2)
            {
                AddLog("We're at the resource");
                ResourcePhysicalData phyData = ResourceHandler.Instance.PeekResourcesAtLocation(resourceLocations[0]);
                if (phyData.Resources.Count >= PeepleCarryingCapacity && phyData.ResourceType == type)
                {
                    AddLog("We found the resource! Taking it");
                    //Found resource
                    ResourceIndividual resourceRetrieved;
                    if(!ResourceHandler.Instance.RetrieveResourceFromTile(type, resourceLocations[0], out resourceRetrieved))
                    {
                        Debug.LogError("Issue retrieving resource from tile");
                    }
                    ResourceHolding = resourceRetrieved;

                    onComplete(true);
                    yield break;
                }
                else
                {
                    if(phyData.Resources.Count <= 0)
                    {
                        AddLog("This pile didn't have enough resources for us to take any!");
                    }
                    if(phyData.ResourceType != type)
                    {
                        AddLog("This piles resource was different than what we expected. We wanted " + type + " but this is a pile of " + phyData.ResourceType);
                    }
                    exclusions.Add(resourceLocations[0]);
                }
                //else
                //{
                //    onComplete(false);
                //    yield break;
                //}
            }

            AddLog("Failed to find resource, checking impatience");
            if (Random.Range(1, 101) <= impatience)
            {
                onComplete(false);
                yield break; //I quit! //TODO: Add stuff for quitting
            }
            else
            {
                impatience += 1; //TODO: Increase impatience based on personality
            }

            AddLog("Removing location from options");
            resourceLocations.RemoveAt(0);
            tileOn = Movement.GetTileOn();

            //Any Peeple nearby to ask
            if (impatience > 10)
            {
                AddLog("Asking other Peeple");
                Peeple[] otherPeeple = PeepleHandler.Instance.GetPeepleOnTiles(HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tileOn, 5));
                for (int i = 0; i < otherPeeple.Length; i++)
                {
                    if (otherPeeple[i] != this)
                    {
                        //Ask Peeple for updated information on resource
                        memory.UpdateWithKnowledgeFromOther(otherPeeple[i].memory, type);

                        //TODO: Add some amount of UI and interaction to this
                    }
                }
            }

            AddLog("Refreshing our knowledge");
            //Refresh our knowledge
            resourceLocations = new List<HexTile>(memory.GetSeenResourceLocations(type));
            resourceLocations.Sort((x, y) => { return HexCoordinates.HexDistance(tileOn.Coordinates, x.Coordinates).CompareTo(HexCoordinates.HexDistance(tileOn.Coordinates, y.Coordinates)); });
        }

        AddLog("After searching absolutely everything, we're stuck and exiting");
        onComplete(false);
    }

    private IEnumerator<float> MoveToHome(System.Action<bool> onComplete)
    {
        if (peepleWorldState.location == PeepleLocation.Home)
        {
            onComplete(true);
            yield break;
        }
        SetAIState(PeepleAIState.Moving);

        ResourceHolding = null;

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
            yield return Timing.WaitUntilDone(workable.DoWork(this));
            if (workable == null || workable.WorkFinished)
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

            yield return Timing.WaitUntilDone(Job.DoWork(this));
            peepleWorldState.hunger += 2;
            peepleWorldState.energy -= 1;

            yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
            onComplete(true);
        }
    }

    private IEnumerator<float> MoveResourcesToStorage(System.Action<bool> onComplete)
    {
        SetAIState(PeepleAIState.DoingNothing);
        peepleWorldState.hunger++;
        peepleWorldState.energy--;
        peepleWorldState.location = PeepleLocation.Anywhere;

        HashSet<HexTile> exclusions = memory.GetSeenJobSites();
        exclusions.UnionWith(memory.GetAllSeenStorageLocations());

        if(ResourceHolding == null)
        {
            List<HexTile> resourceLocations = new List<HexTile>(memory.GetAllSeenResourceLocations());
            HexTile tileOn = Movement.GetTileOn();
            resourceLocations.Sort((x, y) => { return HexCoordinates.HexDistance(tileOn.Coordinates, x.Coordinates).CompareTo(HexCoordinates.HexDistance(tileOn.Coordinates, y.Coordinates)); });

            foreach (var tile in resourceLocations)
            {
                if(exclusions.Contains(tile))
                {
                    continue;
                }
                Movement.SetGoal(tile.Neighbors[Random.Range(0, tile.Neighbors.Count)]); //TODO: Limit neighbors they can go to based on a number of factors such as if its blocked etc

                yield return Timing.WaitUntilFalse(() => { return Movement.IsMoving; });

                ResourcePhysicalData physicalData = ResourceHandler.Instance.PeekResourcesAtLocation(tile);
                if (physicalData.Resources.Count > 0)
                {
                    ResourceIndividual resourceRetrieved;
                    if (!ResourceHandler.Instance.RetrieveResourceFromTile(physicalData.ResourceType, tile, out resourceRetrieved))
                    {
                        Debug.LogError("Issue retrieving resource from tile");
                    }
                    ResourceHolding = resourceRetrieved;
                    break;
                }
            }
        }

        if(ResourceHolding != null)
        {
            List<HexTile> storageLocations = new List<HexTile>(memory.GetSeenStorageLocations(ResourceHolding.Type));

            for (int i = 0; i < storageLocations.Count; i++)
            {
                Movement.SetGoal(storageLocations[i].Neighbors[Random.Range(0, storageLocations[i].Neighbors.Count)]); //TODO: Limit neighbors they can go to based on a number of factors such as if its blocked etc

                yield return Timing.WaitUntilFalse(() => { return Movement.IsMoving; });

                //TODO: Check if tile is full
                ResourceHandler.Instance.PlaceResourceOnTile(ResourceHolding, storageLocations[i]);
                ResourceHeldPlacing();
                break;
            }

            onComplete(true);
        }
        else
        {
            onComplete(false);
        }
    }

    private IEnumerator<float> CollectResourcesForJob(System.Action<bool> onComplete)
    {
        AddLog("Collecting resources for job");
        SetAIState(PeepleAIState.DoingNothing);
        peepleWorldState.location = PeepleLocation.Anywhere;

        if (Job.RequiresResources)
        {
            var resourcesNeeded = Job.ResourcesNeeded;
            ResourceType neededResource = ResourceType.Flags;
            int amountNeeded = 0;
            bool last = false;
            int lastCounter = 0;
            foreach (var key in Job.ResourcesNeeded.Keys)
            {
                lastCounter++;

                amountNeeded = Job.ResourcesNeeded[key];
                if (amountNeeded > 0)
                {
                    if(lastCounter == Job.ResourcesNeeded.Count)
                    {
                        last = true;
                    }
                    neededResource = key;
                    break;
                }
            }
            AddLog("Resource determined to collect: " + neededResource);

            amountNeeded = Mathf.Min(amountNeeded, PeepleCarryingCapacity);

            if(neededResource == ResourceType.Flags)
            {
                Job.UpdateResourceStatus();
                onComplete(false);

                yield break;
            }

            while(true)
            {
                AddLog("Starting check");
                peepleWorldState.hunger++;
                peepleWorldState.energy--;

                bool trouble = false;
                yield return Timing.WaitUntilDone(FindAndTakeResource(neededResource, (success) =>
                {
                    if (!success)
                    {
                        trouble = true;
                    }
                }, memory.GetSeenJobSites()));

                AddLog("Finished finding resource");
                if (trouble)
                {
                    AddLog("There was an issue when finding resource");
                    if (last)
                    {
                        if(Job != null)
                        {
                            Job.MarkAsOutOfResources(this);
                        }
                        onComplete(false);

                        yield break;
                    }
                    else
                    {
                        int currCounter = 0;
                        if(Job == null)
                        {
                            onComplete(false);
                            yield break;
                        }
                        foreach (var key in Job.ResourcesNeeded.Keys)
                        {
                            currCounter++;
                            if(currCounter <= lastCounter)
                            {
                                continue;
                            }
                            lastCounter++;

                            amountNeeded = Job.ResourcesNeeded[key];
                            if (amountNeeded > 0)
                            {
                                if (lastCounter == Job.ResourcesNeeded.Count)
                                {
                                    last = true;
                                }
                                neededResource = key;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if(ResourceHolding == null)
            {
                AddLog("We're not holding anything. Breaking out");
                onComplete(false);

                yield break;
            }

            bool jobLost = false;
            if (Job != null)
            {
                if (HexCoordinates.HexDistance(Job.ResourcePiles[neededResource].Coordinates, Movement.GetTileOn().Coordinates) > 2)
                {
                    AddLog("Moving to a resource pile");
                    Movement.SetGoal(Job.ResourcePiles[neededResource].Neighbors[Random.Range(0, Job.ResourcePiles[neededResource].Neighbors.Count)]); //TODO: Limit neighbors they can go to based on a number of factors such as if its blocked etc

                    yield return Timing.WaitUntilFalse(() => { return Movement.IsMoving; });
                    AddLog("Arrived at resource pile");
                }
            }
            else
            {
                jobLost = true;
            }

            if (ResourceHolding == null)
            {
                AddLog("We're not holding anything, breaking out");
                onComplete(false);

                yield break;
            } 
            if (Job != null)
            {
                AddLog("Placing resource on tile");
                ResourceHandler.Instance.PlaceResourceOnTile(ResourceHolding, Job.ResourcePiles[neededResource]);
                Job.AddResource(ResourceHolding.Type, 1);

                ResourceHeldPlacing();
            }
            else
            {
                jobLost = true;
            }
            ResourceHolding = null;

            if(jobLost)
            {
                AddLog("We lost our job at some point, ending");
                onComplete(false);
                yield break;
            }
            else
            {
                onComplete(true);
                yield break;
            }
        }

        onComplete(false);

        yield break;
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

        while(peepleWorldState.energy < 90)
        {
            restingSymbol.SetActive(true);
            peepleWorldState.energy += 15;
            peepleWorldState.hunger++;
            yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        }
        restingSymbol.SetActive(false);

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
        ResourceHolding = null;

        SetAIState(PeepleAIState.Sleeping);
        peepleWorldState.energy += 5;
        if (peepleWorldState.energy > 100)
        {
            peepleWorldState.energy = 100;
        }

        yield return Timing.WaitForSeconds(PeepleHandler.STANDARD_ACTION_TICK);
        onComplete(true);
    }
}
