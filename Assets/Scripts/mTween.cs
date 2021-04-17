
using System.Collections.Generic;
using MEC;
using UnityEngine;

public static class mTween
{
    public static IEnumerator<float> _RotateTo(GameObject go, Vector3 rot, float time) {
        var startRot = go.transform.rotation;
        var endRot = Quaternion.Euler(rot);
        var timer = 0f;
        while (timer < time) {
            go.transform.rotation = Quaternion.Lerp(startRot, endRot,timer / time);
            yield return Timing.WaitForOneFrame;
            timer += Timing.DeltaTime;
        }
        go.transform.rotation = endRot;
    }
}