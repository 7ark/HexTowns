using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;

public abstract class HTN_Agent<T> : MonoBehaviour where T : struct
{
    [SerializeField]
    protected List<string> HTN_LOG = new List<string>();

    protected Task lifeHTN;
    protected Queue<Task> currentTaskList = null;
    private List<int> currentMtr = null;
    private float replanTimer = 0;

    private bool currentCantBeCancelled = false;
    private CoroutineHandle planHandle;
    private CoroutineHandle currentTaskHandle;
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
        KillCoroutines(); //todo pause?
    }

    private void KillCoroutines()
    {
        if (planHandle.IsValid) {
            HTN_LOG.Add("Killing Coroutines");
            Timing.KillCoroutines(planHandle);
            Timing.KillCoroutines(currentTaskHandle);
        }
    }

    protected virtual void OnEnable()
    {
        CheckToRunAgain();
    }

    private void CheckToRunAgain()
    {
        HTN_LOG.Add("Check to run again");
        if(lifeHTN != null && (currentTaskList == null || currentTaskList.Count == 0))
        {
            if (planHandle.IsValid) {
                HTN_LOG.Add("Running again, but current is valid?");
            }
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
            planHandle = Timing.RunCoroutine(_Run(), gameObject, gameObject.name);
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
        if(currentTaskList == null)
        {
            yield break;
        }

        if (!planHandle.IsValid) {
            planHandle = Timing.CurrentCoroutine;
        }
        
        bool cancelAll = false;
        while(currentTaskList.Count > 0)
        {
            PrimitiveTask<T> current = currentTaskList.Dequeue() as PrimitiveTask<T>;
            canBeCancelled = current.CanBeCancelled;
            HTN_LOG.Add("Task Start: " + current.TaskName);
            var taskOperator = current.GetFinalRunResult()(success => {
                if (!success) {
                    cancelAll = true;
                }

                HTN_LOG.Add("Task " + current.TaskName + " Completed with: " + success);
            });
            currentTaskHandle = Timing.RunCoroutine(taskOperator, gameObject, gameObject.name);
            if (!currentTaskHandle.IsValid) {
                Debug.LogError("SubTask Not Valid");
            }
            
            yield return Timing.WaitUntilDone(currentTaskHandle);

            if(cancelAll)
            {
                HTN_LOG.Add("Current plan cancelled");
                break;
            }
        }
        currentTaskList = null;
        currentMtr = null;
        planHandle = default;
        delayCheckAgainTimer = 0;
    }

    private void SetReplanTimer()
    {
        replanTimer = Random.Range(1f, 2f);
    }

    protected virtual void Replanning()
    {

    }

    protected virtual void Update()
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
        
        if(!planHandle.IsValid || !planHandle.IsRunning)
        {
            delayCheckAgainTimer -= Time.deltaTime;
            
            if (delayCheckAgainTimer <= 0) {
                delayCheckAgainTimer = 2f;
                CheckToRunAgain();
            }

        }
    }
}
