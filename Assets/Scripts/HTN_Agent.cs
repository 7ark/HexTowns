using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;

public abstract class HTN_Agent<T> : MonoBehaviour where T : struct
{
    private const float TIME_TO_CHECK_FOR_REPLAN = 1;

    protected Task lifeHTN;
    protected Queue<Task> currentTaskList = null;
    private List<int> currentMtr = null;
    private bool canContinueToNextTask = false;
    private float replanTimer = 0;

    private bool currentCantBeCancelled = false;
    private CoroutineHandle runningCoroutine;
    private CoroutineHandle delayedCoroutine;

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
        if(runningCoroutine.IsValid)
        {
            Timing.KillCoroutines(runningCoroutine);
        }
        if(delayedCoroutine.IsValid)
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
            currentCantBeCancelled = false;
            runningCoroutine = Timing.RunCoroutine(_Run());

        }
        else
        {
            currentTaskList = null;
            currentMtr = null;
            delayedCoroutine = Timing.RunCoroutine(_DelayedCheckLater());
        }
    }


    public void MarkCurrentAsUncancellable()
    {
        currentCantBeCancelled = true;
    }
    
    private IEnumerator<float> _DelayedCheckLater()
    {
        yield return Timing.WaitForSeconds(1);

        CheckToRunAgain();
    }

    private IEnumerator<float> _Run()
    {
        bool cancelAll = false;
        while(currentTaskList.Count > 0)
        {
            canContinueToNextTask = false;
            PrimitiveTask<T> current = currentTaskList.Dequeue() as PrimitiveTask<T>;
            yield return Timing.WaitUntilDone(current.GetFinalRunResult()((didItGoWell) => 
            { 
                canContinueToNextTask = true; 
                if(!didItGoWell)
                {
                    cancelAll = true;
                }
            }));
            while(!canContinueToNextTask)
            {
                yield return Timing.WaitForOneFrame;
            }

            if(cancelAll)
            {
                break;
            }
        }
        currentTaskList = null;
        currentMtr = null;

        CheckToRunAgain();
    }

    private void Update()
    {
        replanTimer += Time.deltaTime;

        if(replanTimer >= TIME_TO_CHECK_FOR_REPLAN)
        {
            replanTimer = 0;

            if(!currentCantBeCancelled && currentMtr != null)
            {
                HTN_Plan plan = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState(), currentMtr);

                if(plan.Plan != null && !currentMtr.SequenceEqual(plan.MTR))
                {
                    Timing.KillCoroutines(runningCoroutine);
                    TryRunPlan(plan);
                }
            }
        }
    }
}
