using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    public void SetCameraInstance(Camera camera)
    {
        cam = camera;
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
               cam.transform.rotation * Vector3.up);
    }
}
