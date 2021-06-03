using MEC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskWorkable : Workable
{
    private List<PrimitiveTask<Peeple.PeepleWS>> tasksToDo = new List<PrimitiveTask<Peeple.PeepleWS>>();
    private bool doingWork = false;

    public TaskWorkable(params System.Func<System.Action<bool>, IEnumerator<float>>[] tasks)
    {
        for (int i = 0; i < tasks.Length; i++)
        {
            tasksToDo.Add(new PrimitiveTask<Peeple.PeepleWS>("Workable Task 1", null, null, tasks[i], false));
        }

        tasksToDo.Add(new PrimitiveTask<Peeple.PeepleWS>("Workable Task Completion", null, null, MarkCompletion, false)); //TODO: Need to figure out a scenario for if the task doesn't complete from failing, also allow it to be cancelled
    }
    private IEnumerator<float> MarkCompletion(System.Action<bool> onComplete)
    {
        WorkCompleted(true);

        yield return MEC.Timing.WaitForOneFrame;
    }
    public override IEnumerator<float> DoWork(Peeple specificPeepleWorking = null)
    {
        if(doingWork)
        {
            yield break;
        }
        for (int i = 0; i < tasksToDo.Count; i++)
        {
            if(specificPeepleWorking != null)
            {
                if(!currentWorkers.Contains(specificPeepleWorking))
                {
                    currentWorkers.Add(specificPeepleWorking);
                }
                specificPeepleWorking.AddToTasks(tasksToDo[i]);
            }
            else
            {
                currentWorkers[0].AddToTasks(tasksToDo[i]);
            }
        }

        doingWork = true;
    }

    //public static TaskWorkable CreateMoveResourcesTask(HexTile from, HexTile to, ResourceType resource, int resourceAmount, bool startWorking = true)
    //{
    //    TaskWorkable moveResourceWork = null;
    //    moveResourceWork = new TaskWorkable(MoveToNewLocation)
    //    {
    //        TilesAssociated = new HashSet<HexTile>() { from }
    //    };
    //
    //    if(startWorking)
    //    {
    //        moveResourceWork.BeginWorking();
    //    }
    //
    //    return moveResourceWork;
    //
    //    IEnumerator<float> MoveToNewLocation(System.Action<bool> onComplete)
    //    {
    //        ResourceHandler.Instance.PickupResources(resource, resourceAmount, from);
    //
    //        moveResourceWork.GetWorker().Movement.SetGoal(to.Neighbors[Random.Range(0, to.Neighbors.Count)]); //TODO: Limit neighbors they can go to based on a number of factors such as if its blocked etc
    //
    //        yield return Timing.WaitUntilFalse(() => { return moveResourceWork.GetWorker().Movement.IsMoving; });
    //
    //        ResourceHandler.Instance.GainResource(resource, resourceAmount, to, false);
    //
    //        onComplete(true);
    //    }
    //}
}
