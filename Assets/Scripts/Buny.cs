using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;

public class Buny : Animal
{
    [SerializeField]
    private AnimationCurve bounceCurve;

    private CoroutineHandle movementCoroutine;

    protected void Start()
    {
        Movement.OverrideMovementHandling += HandleMovement;

        SetupHTN(new CompoundTask<int>("Buny",
            new Method<int>().AddSubTasks(
                new PrimitiveTask<int>("WalkAround",
                    (ws) => { return true; },
                    null,
                    _WalkSomewhereNearby
                    )
                )
            )
        );
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        HexTile tile = Movement.GetTileOn();
        if(tile != null)
        {
            Movement.SetGoal(tile, true);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (movementCoroutine.IsValid) {
            Timing.KillCoroutines(movementCoroutine);
        }
    }

    private IEnumerator<float> _WalkSomewhereNearby(System.Action<bool> onComplete)
    {
        HexTile tileOn = Movement.GetTileOn();
        var tileOptions = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tileOn, 5);

        HexTile tileToMoveTo = null;
        foreach (var option in tileOptions.Shuffle()) {
            if(option.CantWalkThrough || option.BuildingOnTile != null || option.ParentBoard != homeBoard)
            {
                continue;
            }

            tileToMoveTo = option;
            break;
        }
        
        HTN_LOG.Add($"Moving to tile {tileToMoveTo?.Coordinates}");

        bool waitingToFinish = true;
        Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            waitingToFinish = false;
        });

        while(waitingToFinish)
        {
            yield return Timing.WaitForOneFrame;
        }

        //Rest, did so much bouncing
        yield return Timing.WaitForSeconds(Random.Range(3f, 6f));

        onComplete?.Invoke(true);
    }

    public override ResourceWorkable MarkToKill(bool startWork)
    {
        ResourceWorkable workable = base.MarkToKill(startWork);
        if (movementCoroutine.IsValid)
        {
            Timing.KillCoroutines(movementCoroutine);
        }

        return workable;
    }

    private void HandleMovement(List<Vector3> path, System.Action onComplete)
    {
        if(toKillWorkable != null)
        {
            return;
        }

        if(movementCoroutine.IsValid)
        {
            Timing.KillCoroutines(movementCoroutine);
        }
        if(gameObject.activeSelf)
        {
            movementCoroutine = Timing.RunCoroutine(_BounceMovement(path, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator<float> _BounceMovement(List<Vector3> path, System.Action onComplete)
    {
        const int bouncePoints = 5;
        for (int i = 0; i < path.Count - 1; i++)
        {
            var bouncePositions = new Vector3[bouncePoints + 1];
            for (int j = 0; j <= bouncePoints; j++)
            {
                float delta = (float)j / (float)bouncePoints;
                bouncePositions[j] = Vector3.Lerp(path[i], path[i + 1], delta) + new Vector3(0, bounceCurve.Evaluate(delta));
            }
            Vector3 diff = path[i + 1] - path[i];
            Quaternion rot = diff == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(diff);
            Timing.RunCoroutine(mTween._RotateTo(gameObject, new Vector3(0, rot.eulerAngles.y), Movement.MovementSpeed - 0.2f), gameObject);
            iTween.MoveTo(gameObject, iTween.Hash("path", bouncePositions, "time", Movement.MovementSpeed, "easetype", iTween.EaseType.easeInCirc, "delay", 0.1f));
            yield return Timing.WaitForSeconds(Movement.MovementSpeed + 0.1f);
        }

        onComplete?.Invoke();
    }
}
