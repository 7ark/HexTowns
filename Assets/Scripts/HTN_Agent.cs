using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;

public abstract class HTN_Agent<T> : MonoBehaviour where T : struct
{
    [SerializeField]
    private List<string> HTN_LOG = new List<string>();

    protected Task lifeHTN;
    protected Queue<Task> currentTaskList = null;
    private List<int> currentMtr = null;
    private bool canContinueToNextTask = false;
    private float replanTimer = 0;

    private bool currentCantBeCancelled = false;
    private CoroutineHandle runningCoroutine;
    private CoroutineHandle delayedCoroutine;
    private bool canBeCancelled = true;
    private float delayCheckAgainTimer = 0;

    protected void SetupHTN(Task htnRoot)
    {
        lifeHTN = htnRoot;

        CheckToRunAgain();
    }

    protected abstract T GetCurrentWorldState();

    public void AddToTasks(Task task)
    {
        if(currentTaskList != null)
        {
            currentTaskList.Enqueue(task);
        }
    }

    protected virtual void OnDisable()
    {
        KillCoroutines();
    }

    private void KillCoroutines()
    {
        if (runningCoroutine.IsValid)
        {
            Timing.KillCoroutines(runningCoroutine);
        }
        if (delayedCoroutine.IsValid)
        {
            Timing.KillCoroutines(delayedCoroutine);
        }
    }

    protected virtual void OnEnable()
    {
        CheckToRunAgain();
    }

    private void CheckToRunAgain()
    {
        if(lifeHTN != null && (currentTaskList == null || currentTaskList.Count == 0))
        {
            HTN_Plan plan = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState());

            TryRunPlan(plan);
        }
    }

    private void TryRunPlan(HTN_Plan plan)
    {
        currentTaskList = plan.Plan;
        currentMtr = plan.MTR;

        if (currentTaskList != null && currentTaskList.Count > 0 && Time.timeScale != 0)
        {
            HTN_LOG.Add("Running plan");
            currentCantBeCancelled = false;
            runningCoroutine = Timing.RunCoroutine(_Run(), gameObject);
        }
        else
        {
            HTN_LOG.Add("Plan not valid, waiting to try again");
            currentTaskList = null;
            currentMtr = null;
        }
    }


    public void MarkCurrentAsUncancellable()
    {
        currentCantBeCancelled = true;
    }

    private IEnumerator<float> _Run()
    {
        bool cancelAll = false;
        while(currentTaskList.Count > 0)
        {
            canContinueToNextTask = false;
            PrimitiveTask<T> current = currentTaskList.Dequeue() as PrimitiveTask<T>;
            canBeCancelled = current.CanBeCancelled;
            HTN_LOG.Add("Task Start: " + current.TaskName);
            //CoroutineHandle subTask = Timing.RunCoroutine(current.GetFinalRunResult()((didItGoWell) =>
            yield return Timing.WaitUntilDone(current.GetFinalRunResult()((didItGoWell) =>
            {
                canContinueToNextTask = true;
                if (!didItGoWell)
                {
                    cancelAll = true;
                }

                HTN_LOG.Add("Task " + current.TaskName + " Completed with: " + didItGoWell);
            }));//, runningCoroutine.Segment);
            //Timing.LinkCoroutines(runningCoroutine, subTask);
            //yield return Timing.WaitUntilDone(subTask);
            while(!canContinueToNextTask)
            {
                //if(!subTask.IsValid || !subTask.IsRunning)
                //{
                //    canContinueToNextTask = true;
                //    cancelAll = true;
                //}

                yield return Timing.WaitForOneFrame;
            }

            if(cancelAll)
            {
                HTN_LOG.Add("Current plan cancelled");
                break;
            }
        }
        currentTaskList = null;
        currentMtr = null;

        CheckToRunAgain();
    }

    private void SetReplanTimer()
    {
        replanTimer = Random.Range(1f, 2f);
    }

    protected virtual void Replanning()
    {

    }

    private void Update()
    {
        replanTimer -= Time.deltaTime;

        if(replanTimer <= 0)
        {
            SetReplanTimer();

            if(!currentCantBeCancelled && currentMtr != null)
            {
                HTN_Plan plan = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState(), currentMtr);

                if(plan.Plan != null && !currentMtr.SequenceEqual(plan.MTR) && canBeCancelled)
                {
                    HTN_LOG.Add("Replan succeeded, running replan");
                    Replanning();
                    KillCoroutines();
                    TryRunPlan(plan);
                }
            }
        }

        delayCheckAgainTimer -= Time.deltaTime;
        if(delayCheckAgainTimer <= 0)
        {
            delayCheckAgainTimer = 2f;

            if(!runningCoroutine.IsValid || !runningCoroutine.IsRunning)
            {
                CheckToRunAgain();
            }
        }
    }
}
