using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HTN_Agent<T> : MonoBehaviour where T : struct
{
    protected Task lifeHTN;
    protected Queue<Task> currentTaskList = null;
    private bool canContinueToNextTask = false;

    protected void SetupHTN(Task htnRoot)
    {
        lifeHTN = htnRoot;

        CheckToRunAgain();
    }

    protected abstract T GetCurrentWorldState();

    private void CheckToRunAgain()
    {
        if(currentTaskList == null)
        {
            currentTaskList = HTN_Planner<T>.MakePlan(lifeHTN, GetCurrentWorldState());

            if(currentTaskList != null && currentTaskList.Count > 0)
            {
                StartCoroutine(Run());
            }
            else
            {
                currentTaskList = null;
                StartCoroutine(DelayedCheckLater());
            }
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

        CheckToRunAgain();
    }


}
