using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class HTN_Agent<T> : MonoBehaviour where T : struct
{
    private const float TIME_TO_CHECK_FOR_REPLAN = 1;

    protected Task lifeHTN;
    protected Queue<Task> currentTaskList = null;
    private List<int> currentMtr = null;
    private bool canContinueToNextTask = false;
    private float replanTimer = 0;
    private Coroutine runningCoroutine = null;

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

    private void CheckToRunAgain()
    {
        if(currentTaskList == null || currentTaskList.Count == 0)
        {
            HTN_Plan plan = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState());

            TryRunPlan(plan);
        }
    }

    private void TryRunPlan(HTN_Plan plan)
    {
        currentTaskList = plan.Plan;
        currentMtr = plan.MTR;

        if (currentTaskList != null && currentTaskList.Count > 0)
        {
            runningCoroutine = StartCoroutine(Run());
        }
        else
        {
            currentTaskList = null;
            currentMtr = null;
            StartCoroutine(DelayedCheckLater());
        }
    }

    private IEnumerator DelayedCheckLater()
    {
        yield return new WaitForSeconds(1);

        CheckToRunAgain();
    }

    private IEnumerator Run()
    {
        bool cancelAll = false;
        while(currentTaskList.Count > 0)
        {
            canContinueToNextTask = false;
            PrimitiveTask<T> current = currentTaskList.Dequeue() as PrimitiveTask<T>;
            yield return current.GetFinalRunResult()((didItGoWell) => 
            { 
                canContinueToNextTask = true; 
                if(!didItGoWell)
                {
                    cancelAll = true;
                }
            });
            while(!canContinueToNextTask)
            {
                yield return null;
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

            if(currentMtr != null)
            {
                HTN_Plan plan = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState(), currentMtr);

                if(plan.Plan != null && !currentMtr.SequenceEqual(plan.MTR))
                {
                    StopCoroutine(runningCoroutine);
                    TryRunPlan(plan);
                }
            }
        }
    }
}
