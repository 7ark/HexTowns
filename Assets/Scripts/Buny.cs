using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buny : Animal
{
    [SerializeField]
    private AnimationCurve bounceCurve;

    private Coroutine movementCoroutine;

    protected void Start()
    {
        Movement.OverrideMovementHandling += HandleMovement;

        SetupHTN(new CompoundTask<int>("Buny",
            new Method<int>().AddSubTasks(
                new PrimitiveTask<int>("WalkAround",
                    (ws) => { return true; },
                    null,
                    WalkSomewhereNearby
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
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
    }

    private IEnumerator WalkSomewhereNearby(System.Action<bool> onComplete)
    {
        HexTile tileOn = Movement.GetTileOn();
        List<HexTile> tileOptions = HexBoardChunkHandler.Instance.GetTileNeighborsInDistance(tileOn, 5);
        tileOptions.Shuffle();

        HexTile tileToMoveTo = null;
        for (int i = 0; i < tileOptions.Count; i++)
        {
            if(tileOptions[i].CantWalkThrough || tileOptions[i].BuildingOnTile != null || tileOptions[i].ParentBoard != homeBoard)
            {
                continue;
            }

            tileToMoveTo = tileOptions[i];
            break;
        }

        bool waitingToFinish = true;
        Movement.SetGoal(tileToMoveTo, arrivedComplete: (success) =>
        {
            waitingToFinish = false;
        });

        while(waitingToFinish)
        {
            yield return null;
        }

        //Rest, did so much bouncing
        yield return new WaitForSeconds(Random.Range(3f, 6f));

        onComplete?.Invoke(true);
    }

    private void HandleMovement(List<Vector3> path, System.Action onComplete)
    {
        if(movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        if(gameObject.activeSelf)
        {
            movementCoroutine = StartCoroutine(BounceMovement(path, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator BounceMovement(List<Vector3> path, System.Action onComplete)
    {
        const int bouncePoints = 5;
        for (int i = 0; i < path.Count - 1; i++)
        {
            List<Vector3> bouncePositions = new List<Vector3>();
            for (int j = 0; j <= bouncePoints; j++)
            {
                float delta = (float)j / (float)bouncePoints;
                bouncePositions.Add(Vector3.Lerp(path[i], path[i + 1], delta) + new Vector3(0, bounceCurve.Evaluate(delta)));
            }
            Quaternion rot = Quaternion.LookRotation(path[i + 1] - path[i]);
            iTween.RotateTo(gameObject, new Vector3(0, rot.eulerAngles.y), Movement.MovementSpeed - 0.2f);
            iTween.MoveTo(gameObject, iTween.Hash("path", bouncePositions.ToArray(), "time", Movement.MovementSpeed, "easetype", iTween.EaseType.easeInCirc, "delay", 0.1f));

            yield return new WaitForSeconds(Movement.MovementSpeed + 0.1f);
        }

        onComplete?.Invoke();
    }
}
