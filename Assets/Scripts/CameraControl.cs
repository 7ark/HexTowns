using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    [SerializeField]
    private float speed = 10;

    public static CameraControl Instance;
    public static Camera CameraInstance;

    private Vector3 movement = Vector3.zero;
    private Vector2 prevMousePosition = Vector3.zero;
    private Vector3? mouseRotationPoint = null;

    private void Awake()
    {
        Instance = this;
        CameraInstance = Camera.main;// GetComponentInChildren<Camera>();
    }

    public void RegisterInput(MasterInput inputMaster)
    {
        inputMaster.Player.MoveCamera.performed += MoveCamera_performed;
        inputMaster.Player.RotateCamera.performed += RotateCamera_performed;
    }

    private void RotateCamera_performed(InputAction.CallbackContext context)
    {
        //if(context.ReadValue<float>() == 1)
        //{
        //    RaycastHit hit;
        //    if (Physics.Raycast(CameraControl.CameraInstance.ScreenPointToRay(Mouse.current.position.ReadValue()), out hit))
        //    {
        //        mouseRotationPoint = hit.point;
        //    }
        //}
        //else
        //{
        //    mouseRotationPoint = null;
        //}

    }

    private void MoveCamera_performed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        movement = new Vector3(value.x, 0, value.y);
    }

    private void Update()
    {
        if(mouseRotationPoint != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = mousePos - prevMousePosition;

            //transform.rotation *= Quaternion.Euler(Vector3.up * (-mouseDelta.x / 5f));

            CameraInstance.transform.RotateAround(mouseRotationPoint.Value, new Vector3(mouseDelta.y, mouseDelta.x, 0), mouseDelta.magnitude);
        }

        transform.position += movement * Time.deltaTime * speed;

        prevMousePosition = Mouse.current.position.ReadValue();
    }
}
