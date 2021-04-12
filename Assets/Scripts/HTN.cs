using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Task
{
    private string taskName;
    public Task(string name)
    {
        taskName = name;
    }
}
public class PrimitiveTask<T> : Task
{
    private System.Action<T> taskOperator;
    private System.Predicate<T> taskConditionOperator;
    private System.Func<System.Action<bool>, IEnumerator> taskResultOperator;

    public PrimitiveTask(string name, System.Predicate<T> taskConditionOperator, System.Action<T> taskOperator, System.Func<System.Action<bool>, IEnumerator> taskResultOperator) : base(name)
    {
        this.taskOperator = taskOperator;
        this.taskConditionOperator = taskConditionOperator;
        this.taskResultOperator = taskResultOperator;
    }

    public bool IsConditionMet(T worldState)
    {
        return taskConditionOperator.Invoke(worldState);
    }

    public void ApplyOperatorEffects(T worldState)
    {
        taskOperator?.Invoke(worldState);
    }

    public System.Func<System.Action<bool>, IEnumerator> GetFinalRunResult()
    {
        return taskResultOperator;
    }
}
public class CompoundTask<T> : Task where T : struct
{
    private Method<T>[] methods;
    public CompoundTask(string name, params Method<T>[] methods) : base(name)
    {
        this.methods = methods;
    }

    public Method<T> GetSatisfiedMethod(ref PlannerState<T> state)
    {
        for (; state.methodIndex < methods.Length; state.methodIndex++)
        {
            if(methods[state.methodIndex].Check(state.worldState))
            {
                return methods[state.methodIndex];
            }
        }

        return null;
    }
}

public class Method<T>
{
    private System.Func<T, bool>[] methodChecks;
    private Task[] tasks;

    public Method(params System.Func<T, bool>[] methodChecks)
    {
        this.methodChecks = methodChecks;
    }
    public bool Check(T worldState)
    {
        for (int i = 0; i < methodChecks.Length; i++)
        {
            if (methodChecks[i].Invoke(worldState) == false)
            {
                return false;
            }
        }

        return true;
    }

    public Method<T> AddSubTasks(params Task[] tasks)
    {
        this.tasks = tasks;

        return this;
    }

    public Task[] GetSubTasks()
    {
        return tasks;
    }
}

public class HTN
{
    public Task Root { get; private set; }

    public void SetRoot(Task root)
    {
        Root = root;
    }
}

public class PlannerState<T> where T : struct
{
    public Queue<Task> finalPlan;
    public Stack<Task> tasksToProcess;
    public T worldState;
    public Task decompTask;
    public int methodIndex = 0;

    public PlannerState(T worldState)
    {
        this.worldState = worldState;
        finalPlan = new Queue<Task>();
        tasksToProcess = new Stack<Task>();
    }

    public PlannerState(PlannerState<T> state, Task compoundTask)
    {
        worldState = state.worldState;
        finalPlan = new Queue<Task>(state.finalPlan);
        tasksToProcess = new Stack<Task>(new Stack<Task>(state.tasksToProcess)); //Do this, cause of the nature of a stack itll reverse it otherwise
        methodIndex = state.methodIndex;
        decompTask = compoundTask;
    }
}
public class HTN_Planner<T> where T : struct
{

    public static Queue<Task> MakePlan(Task constructedHTNRoot, T currentWorldState)
    {
        Stack<PlannerState<T>> plannerStateStack = new Stack<PlannerState<T>>();

        PlannerState<T> currentState = new PlannerState<T>(currentWorldState); //Is this being copied?

        currentState.tasksToProcess.Push(constructedHTNRoot);
        while(currentState.tasksToProcess.Count > 0)
        {
            Task currentTask = currentState.tasksToProcess.Pop();
            if(currentTask is CompoundTask<T>)
            {
                Method<T> satisfiedMethod = (currentTask as CompoundTask<T>).GetSatisfiedMethod(ref currentState);

                if(satisfiedMethod != null)
                {
                    plannerStateStack.Push(new PlannerState<T>(currentState, currentTask));

                    Stack<Task> reverseStack = new Stack<Task>();
                    Task[] subTasks = satisfiedMethod.GetSubTasks();
                    foreach(Task subTask in subTasks)
                    {
                        reverseStack.Push(subTask);
                    }
                    while(reverseStack.Count > 0)
                    {
                        currentState.tasksToProcess.Push(reverseStack.Pop());
                    }

                    currentState.methodIndex = 0;
                }
                else
                {
                    if(!Restore())
                    {
                        return currentState.finalPlan;
                    }
                    //if(plannerStateStack.Count == 0)
                    //{
                    //    return null;
                    //}
                    //currentState = plannerStateStack.Pop();
                    //currentState.methodIndex++;
                    //currentState.tasksToProcess.Push(currentState.decompTask);
                }
            }
            else if(currentTask is PrimitiveTask<T>)
            {
                if((currentTask as PrimitiveTask<T>).IsConditionMet(currentState.worldState))
                {
                    (currentTask as PrimitiveTask<T>).ApplyOperatorEffects(currentState.worldState);
                    currentState.finalPlan.Enqueue(currentTask);
                }
                else
                {
                    if (!Restore())
                    {
                        return currentState.finalPlan;
                    }
                }
            }
        }

        return currentState.finalPlan;

        bool Restore()
        {
            if (plannerStateStack.Count == 0)
            {
                return false;
            }
            currentState = plannerStateStack.Pop();
            currentState.methodIndex++;
            currentState.tasksToProcess.Push(currentState.decompTask);

            return true;
        }
    }
}