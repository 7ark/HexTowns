using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodeRoadOne
{
    [System.Serializable]
    public class CRO_CameraStandardInput
    {
        #region Getters and Setters

        #region Panning
        public string PanKeyboardAxisHorizontal { get { return m_PanKeyboardAxisHorizontal; } set { m_PanKeyboardAxisHorizontal = value; } }
        public string PanKeyboardAxisVertical { get { return m_PanKeyboardAxisVertical; } set { m_PanKeyboardAxisVertical = value; } }
        public string PanMouseHoldButton { get { return m_PanMouseHoldButton; } set { m_PanMouseHoldButton = value; } }
        public string PanJoystickAxisHorizontal { get { return m_PanJoystickAxisHorizontal; } set { m_PanJoystickAxisHorizontal = value; } }
        public string PanJoystickAxisVertical { get { return m_PanJoystickAxisVertical; } set { m_PanJoystickAxisVertical = value; } }

        public int PanHorizontalBorderSize { get { return m_PanHorizontalBorderSize; } set { m_PanHorizontalBorderSize = value; } }
        public int PanVerticalBorderSize { get { return m_PanVerticalBorderSize; } set { m_PanVerticalBorderSize = value; } }
        public float PanKeyboardMoveSpeed { get { return m_PanKeyboardMoveSpeed; } set { m_PanKeyboardMoveSpeed = value; } }
        public float PanJoystickMoveSpeed { get { return m_PanJoystickMoveSpeed; } set { m_PanJoystickMoveSpeed = value; } }
        public float PanBorderMoveSpeed { get { return m_PanBorderMoveSpeed; } set { m_PanBorderMoveSpeed = value; } }
        public float DragDetectThreshold { get { return m_DragDetectThreshold; } set { m_DragDetectThreshold = value; } }

        public bool PanWithKeyboard { get { return m_PanWithKeyboard; } set { m_PanWithKeyboard = value; } }
        public bool PanWithMouse { get { return m_PanWithMouse; } set { m_PanWithMouse = value; } }
        public bool PanWithJoystick { get { return m_PanWithJoystick; } set { m_PanWithJoystick = value; } }
        public bool PanWithScreenBorder { get { return m_PanWithScreenBorder; } set { m_PanWithScreenBorder = value; } }
        public bool PanWithTouch { get { return m_PanWithTouch; } set { m_PanWithTouch = value; } }
        #endregion //Panning

        #region Rotate
        public string RotateKeyboardXAxis { get { return m_RotateKeyboardXAxis; } set { m_RotateKeyboardXAxis = value; } }
        public string RotateKeyboardYAxis { get { return m_RotateKeyboardYAxis; } set { m_RotateKeyboardYAxis = value; } }
        public string RotateMouseHoldButton { get { return m_RotateMouseHoldButton; } set { m_RotateMouseHoldButton = value; } }
        public string RotateMouseXAxis { get { return m_RotateMouseXAxis; } set { m_RotateMouseXAxis = value; } }
        public string RotateMouseYAxis { get { return m_RotateMouseYAxis; } set { m_RotateMouseYAxis = value; } }
        public string RotateJoystickXAxis { get { return m_RotateJoystickXAxis; } set { m_RotateJoystickXAxis = value; } }
        public string RotateJoystickYAxis { get { return m_RotateJoystickYAxis; } set { m_RotateJoystickYAxis = value; } }

        public float RotateKeyboardSpeed { get { return m_RotateKeyboardSpeed; } set { m_RotateKeyboardSpeed = value; } }
        public float RotateMouseSpeed { get { return m_RotateMouseSpeed; } set { m_RotateMouseSpeed = value; } }
        public float RotateJoystickSpeed { get { return m_RotateJoystickSpeed; } set { m_RotateJoystickSpeed = value; } }
        public float RotateTouchSpeed { get { return m_RotateTouchSpeed; } set { m_RotateTouchSpeed = value; } }
        public float RotateDetectionThreshold { get { return m_RotateDetectionThreshold; } set { m_RotateDetectionThreshold = value; } }
        public float MinimumRoateAngle { get { return m_MinimumRoateAngle; } set { m_MinimumRoateAngle = value; } }

        public CRO_Camera.TouchRotateGesture TouchRotateGesture { get { return m_TouchRotateGesture; } set { m_TouchRotateGesture = value; } }

        public bool RotateWithKeyboard { get { return m_RotateWithKeyboard; } set { m_RotateWithKeyboard = value; } }
        public bool RotateWithMouse { get { return m_RotateWithMouse; } set { m_RotateWithMouse = value; } }
        public bool RotateWithJoystick { get { return m_RotateWithJoystick; } set { m_RotateWithJoystick = value; } }
        public bool RotateWithTouch { get { return m_RotateWithTouch; } set { m_RotateWithTouch = value; } }
        #endregion //Rotate

        #region Zoom
        public string ZoomKeyboardAxis { get { return m_ZoomKeyboardAxis; } set { m_ZoomKeyboardAxis = value; } }
        public string ZoomMouseAxis { get { return m_ZoomMouseWheelAxis; } set { m_ZoomMouseWheelAxis = value; } }
        public string ZoomJoystickAxis { get  { return m_ZoomJoystickAxis; } set { m_ZoomJoystickAxis = value; } }

        public float ZoomKeyboardSpeed { get  { return m_ZoomKeyboardSpeed; } set { m_ZoomKeyboardSpeed = value; } }
        public float ZoomMouseSpeed { get  { return m_ZoomMouseSpeed; } set { m_ZoomMouseSpeed = value; } }
        public float ZoomJoystickSpeed { get  { return m_ZoomJoystickSpeed; } set { m_ZoomJoystickSpeed = value; } }
        public float ZoomTouchSpeed { get  { return m_ZoomTouchSpeed; } set { m_ZoomTouchSpeed = value; } }
        public float ZoomDetectionThreshold { get  { return m_ZoomDetectionThreshold; } set { m_ZoomDetectionThreshold = value; } }

        public bool ZoomWithKeyboard { get  { return m_ZoomWithKeyboard; } set { m_ZoomWithKeyboard = value; } }
        public bool ZoomWithMouseWheel { get  { return m_ZoomWithMouseWheel; } set { m_ZoomWithMouseWheel = value; } }
        public bool ZoomWithJoystick { get  { return m_ZoomWithJoystick; } set { m_ZoomWithJoystick = value; } }
        public bool ZoomWithTouch { get  { return m_ZoomWithTouch; } set { m_ZoomWithTouch = value; } }
        #endregion //Zoom

        #region Clicking
        public string ClickKeyboardButton { get { return m_ClickKeyboardButton; } set { m_ClickKeyboardButton = value; } }
        public string ClickMouseButton { get { return m_ClickMouseButton; } set { m_ClickMouseButton = value; } }
        public string ClickJoystickButton { get { return m_ClickJoystickButton; } set { m_ClickJoystickButton = value; } }
        public UnityEvent ClickEvent { get { return m_ClickEvent; } set { m_ClickEvent = value; } }
        public float ClickDetectionTime { get { return m_ClickDetectionTime; } set { m_ClickDetectionTime = value; } }
        #endregion //Clicking

        #endregion //Getters and Setters

        #region Public Methods
        public void Start()
        {
            ClearOneFingerControls();
            ClearTwoFingerControls();
            ClearDragging();
            m_CheckForTouchInput = true;
            m_CheckForMouseInput = true;
            m_CheckForKeyboardInput = true;
            m_OnClickTouch = false;
            m_OnClickKeyboard = false;
            m_OnClickMouse = false;
            m_OnClickJoystick = false;

            //Disable missing inputs
            m_PanWithKeyboard = m_PanWithKeyboard && CheckForAxis(m_PanKeyboardAxisHorizontal) && CheckForAxis(m_PanKeyboardAxisVertical);
            m_PanWithMouse = m_PanWithMouse && CheckForAxis(m_PanMouseHoldButton);
            m_PanWithJoystick = m_PanWithJoystick && CheckForAxis(m_PanJoystickAxisHorizontal) && CheckForAxis(m_PanJoystickAxisVertical);
            m_RotateWithKeyboard = m_RotateWithKeyboard && CheckForAxis(m_RotateKeyboardYAxis);
            m_RotateWithMouse = m_RotateWithMouse && CheckForAxis(m_RotateMouseHoldButton) && CheckForAxis(m_RotateMouseYAxis);
            m_RotateWithJoystick = m_RotateWithJoystick && CheckForAxis(m_RotateJoystickYAxis);
            m_ZoomWithKeyboard = m_ZoomWithKeyboard && CheckForAxis(m_ZoomKeyboardAxis);
            m_ZoomWithJoystick = m_ZoomWithJoystick && CheckForAxis(m_ZoomJoystickAxis);
            m_ClickWithKeyboard = m_ClickWithKeyboard && CheckForAxis(m_ClickKeyboardButton);
            m_ClickWithMouse = m_ClickWithMouse && CheckForAxis(m_ClickMouseButton);
            m_ClickWithJoystick = m_ClickWithJoystick && CheckForAxis(m_ClickJoystickButton);
            m_ZoomWithMouseWheel = m_ZoomWithMouseWheel && CheckForAxis(m_ZoomMouseWheelAxis);
            Input.simulateMouseWithTouches = false;
            Input.multiTouchEnabled = true;
        }

        public bool CheckForAxis(string axisName)
        {
            try
            {
                Input.GetAxis(axisName);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public void Update(CRO_Camera croCamera)
        {
            HandleInputTouch(croCamera);
            HandleInputDesktop(croCamera);
        }
        #endregion //Public Methods

        #region Handle Controls
        private Vector3 m_PanKeyDownPosition; //Where we pressed first time the pan key (used to check for distances to see if we need to enter in dragging mode)
        private Vector3 m_TouchPosition0, m_TouchPosition1; //Two fingers touch position
        private Vector3 m_StartDragWorldSpace, m_CurrentPositionInWorldSpace, m_CameraPositionOnStartDragging;

        private float m_OnClickTimeTouch;
        private float m_OnClickTimeKeyboard;
        private float m_OnClickTimeMouse;
        private float m_OnClickTimeJoystick;

        private bool m_OnDragging;
        private bool m_PanKeyDown;
        private bool m_FirstTime2Fingers;
        private bool m_FirstTime1Finger;
        private bool m_OnDraggingWithTouch;
        private bool m_OnZoomingWithTouch;
        private bool m_OnRotatingWithTouch;
        private bool m_CheckForTouchInput;
        private bool m_CheckForMouseInput;
        private bool m_CheckForKeyboardInput;
        private bool m_OnClickTouch;
        private bool m_OnClickKeyboard;
        private bool m_OnClickMouse;
        private bool m_OnClickJoystick;

        private void HandleDraggingWithTouch(CRO_Camera croCamera)
        {
            if(m_ClickWithTouch && !m_OnClickTouch)
            {
                m_OnClickTimeTouch = Time.time + m_ClickDetectionTime;
                Debug.Log("m_OnClickTouch");
                m_OnClickTouch = true;
            }
            if (!croCamera.IsFollowingTarget() && m_PanWithTouch)
            {
                Vector3 touchPosition = Input.GetTouch(0).position;
                if (m_OnDraggingWithTouch)
                {
                    HandleDragging(croCamera, touchPosition);
                }
                else if (m_FirstTime1Finger)
                {
                    m_FirstTime1Finger = false;
                    m_PanKeyDownPosition = touchPosition;
                }
                else if (Vector3.Distance(touchPosition, m_PanKeyDownPosition) > m_DragDetectThreshold)
                {
                    m_OnDraggingWithTouch = true;
                    m_CameraPositionOnStartDragging = croCamera.GetPivotPosition();
                    m_StartDragWorldSpace = croCamera.MouseToWorld(m_CameraPositionOnStartDragging, croCamera.GetTargetedPosition().y, touchPosition);
                }
            }
        }

        private void HandleRotateAndScaleWithTouch(CRO_Camera croCamera)
        {
            if (m_RotateWithTouch || m_ZoomWithTouch)
            {
                float magnitudePrev = 0, magnitudeCurrent = 0;
                float totalMagnitude;

                if (m_FirstTime2Fingers)
                {
                    m_TouchPosition0 = Input.GetTouch(0).position;
                    m_TouchPosition1 = Input.GetTouch(1).position;
                    m_FirstTime2Fingers = false;
                }
                else
                {
                    Vector3 currentPosition0 = Input.GetTouch(0).position;
                    Vector3 currentPosition1 = Input.GetTouch(1).position;

                    #region Touch zoom logic
                    if (m_ZoomWithTouch && !m_OnRotatingWithTouch)
                    {
                        //Compute the magnitude
                        magnitudePrev = (m_TouchPosition0 - m_TouchPosition1).magnitude;
                        magnitudeCurrent = (currentPosition0 - currentPosition1).magnitude;
                        totalMagnitude = magnitudeCurrent - magnitudePrev;

                        if (m_OnZoomingWithTouch)
                        {
                            m_TouchPosition0 = currentPosition0;
                            m_TouchPosition1 = currentPosition1;
                            croCamera.SetTargetedZoomLevel(croCamera.GetTargetedZoomLevel() - totalMagnitude * m_ZoomTouchSpeed);
                        }
                        else if (Mathf.Abs(totalMagnitude) > m_ZoomDetectionThreshold)
                        {
                            m_OnZoomingWithTouch = true;
                        }
                    }
                    #endregion //Touch zoom logic

                    #region Touch rotate logic
                    if (m_RotateWithTouch && !m_OnZoomingWithTouch)
                    {
                        if (m_TouchRotateGesture == CRO_Camera.TouchRotateGesture.Swipe)
                        {
                            float startDistance = (m_TouchPosition1 - m_TouchPosition0).magnitude;
                            float currentDistance = (currentPosition1 - currentPosition0).magnitude;

                            if(Mathf.Abs(currentDistance - startDistance) < 5)
                            {
                                float distance = currentPosition1.x - m_TouchPosition1.x;
                                if (m_OnRotatingWithTouch)
                                {
                                    m_TouchPosition0 = currentPosition0;
                                    m_TouchPosition1 = currentPosition1;
                                    //Detect direction of swipe (left or right)
                                    croCamera.SetTargetedRotationAngle(croCamera.GetTargetedRotationAngle() + distance * 0.5f);
                                }
                                else if(Mathf.Abs(distance) > m_RotateDetectionThreshold)
                                {
                                    m_OnRotatingWithTouch = true;
                                }
                            }
                            else
                            {
                                m_OnRotatingWithTouch = false;
                            }
                        }
                        else //if (m_TouchRotateGesture == CRO_Camera.TouchRotateGesture.Rotate)
                        {
                            Vector3 startVector = m_TouchPosition1 - m_TouchPosition0;
                            Vector3 currentVector = currentPosition1 - currentPosition0;
                            float angleOffset = Vector3.Angle(startVector, currentVector);

                            if (m_OnRotatingWithTouch && (angleOffset > m_MinimumRoateAngle))
                            {
                                m_TouchPosition0 = currentPosition0;
                                m_TouchPosition1 = currentPosition1;

                                Vector3 lr = Vector3.Cross(startVector, currentVector); //z > 0 left, z < 0 right
                                float computedOffset = angleOffset * m_RotateTouchSpeed;
                                computedOffset = lr.z > 0 ? -computedOffset : computedOffset;
                                croCamera.SetTargetedRotationAngle(croCamera.GetTargetedRotationAngle() + computedOffset);
                            }
                            else if (angleOffset > m_RotateDetectionThreshold)
                            {
                                m_OnRotatingWithTouch = true;
                            }
                        }
                    }
                    #endregion //Touch rotate logic
                }
            }
        }

        //Handle mobile input
        private void HandleInputTouch(CRO_Camera croCamera)
        {
            if(!m_CheckForTouchInput)
            {
                m_CheckForTouchInput = Input.touchCount == 0;
                return;
            }
            else if(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && EventSystem.current && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                //We don't want to process anything here as the user pressed on an UI element
                ClearOneFingerControls();
                ClearTwoFingerControls();
                m_CheckForTouchInput = false;
                return;
            }
            switch (Input.touchCount)
            {
                case 0:
                    if(m_ClickWithTouch && m_OnClickTouch && m_OnClickTimeTouch > Time.time && m_ClickEvent != null)
                    {
                        m_ClickEvent.Invoke();
                    }
                    ClearOneFingerControls();
                    ClearTwoFingerControls();
                    break;
                case 1:
                    ClearTwoFingerControls();
                    HandleDraggingWithTouch(croCamera);
                    break;
                case 2:
                    ClearOneFingerControls();
                    HandleRotateAndScaleWithTouch(croCamera);
                    break;
                default: //more than 2 fingers do not do anything
                    ClearOneFingerControls();
                    ClearTwoFingerControls();
                    break;
            }
        }

        public void RefreshCheckForMouse()
        {
            m_CheckForMouseInput = true;
        }

        //Handle Desktop/Console input
        private void HandleInputDesktop(CRO_Camera croCamera)
        {
            //If mouse button 0 is down and the mouse is over an Unity UI control
            //we need to disable the input. Probably here we need to check the default
            //click button not button 0
            if(!m_CheckForMouseInput)
            {
                if(Input.GetMouseButtonUp(0))
                {
                    m_CheckForMouseInput = true;
                }
            }
            else if(Input.GetMouseButtonDown(0) && EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            {
                m_CheckForMouseInput = false;
            }

            GameObject currentSelected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if(currentSelected)
            {
                InputField inputField = currentSelected.GetComponent<InputField>();
                m_CheckForKeyboardInput = inputField == null;
            }
            else
            {
                m_CheckForKeyboardInput = true;
            }

            #region Panning
            if (!croCamera.IsFollowingTarget())
            {
                Vector3 translate = Vector3.zero;
                if (m_CheckForKeyboardInput && m_PanWithKeyboard)
                {
                    translate.x += Input.GetAxis(m_PanKeyboardAxisHorizontal) * m_PanKeyboardMoveSpeed;
                    translate.z += Input.GetAxis(m_PanKeyboardAxisVertical) * m_PanKeyboardMoveSpeed;
                }
                if (m_CheckForMouseInput && m_PanWithMouse)
                {
                    Vector3 mousePosition = Input.mousePosition;

                    if (Input.GetAxis(m_PanMouseHoldButton) != 0)
                    {
                        if (m_OnDragging)
                        {
                            HandleDragging(croCamera, mousePosition);
                        }
                        else if (!m_PanKeyDown)
                        {
                            m_PanKeyDown = true;
                            m_PanKeyDownPosition = mousePosition;
                        }
                        else if (Vector3.Distance(mousePosition, m_PanKeyDownPosition) > m_DragDetectThreshold)
                        {
                            m_OnDragging = true;
                            m_CameraPositionOnStartDragging = croCamera.GetPivotPosition();
                            m_StartDragWorldSpace = croCamera.MouseToWorld(m_CameraPositionOnStartDragging, croCamera.GetTargetedPosition().y, mousePosition);
                        }
                    }
                    else
                    {
                        ClearDragging();
                    }
                }
                if (m_PanWithJoystick)
                {
                    translate.x += Input.GetAxis(m_PanJoystickAxisHorizontal) * m_PanJoystickMoveSpeed;
                    translate.z += Input.GetAxis(m_PanJoystickAxisVertical) * m_PanJoystickMoveSpeed;
                }
                if (m_PanWithScreenBorder)
                {
                    Rect leftRect = new Rect(0, 0, m_PanHorizontalBorderSize, Screen.height);
                    Rect rightRect = new Rect(Screen.width - m_PanHorizontalBorderSize, 0, m_PanHorizontalBorderSize, Screen.height);
                    Rect upRect = new Rect(0, Screen.height - m_PanVerticalBorderSize, Screen.width, m_PanVerticalBorderSize);
                    Rect downRect = new Rect(0, 0, Screen.width, m_PanVerticalBorderSize);

                    translate.x += (leftRect.Contains(Input.mousePosition) ? -1 : rightRect.Contains(Input.mousePosition) ? 1 : 0) * m_PanBorderMoveSpeed;
                    translate.z += (upRect.Contains(Input.mousePosition) ? 1 : downRect.Contains(Input.mousePosition) ? -1 : 0) * m_PanBorderMoveSpeed;
                }
                croCamera.SetTargetedPosition(croCamera.GetTargetedPosition() + croCamera.GetCameraOrientation() * translate * Time.deltaTime);
                if (m_OnDragging)
                {
                    HandleDragging(croCamera, Input.mousePosition);
                }
            }
            else
            {
                ClearOneFingerControls();
            }
            #endregion //Panning

            #region Rotating
            float totalRotationAngle = 0;
            if (m_CheckForKeyboardInput && m_RotateWithKeyboard)
            {
                totalRotationAngle -= Input.GetAxis(m_RotateKeyboardYAxis) * m_RotateKeyboardSpeed;
            }
            if (m_CheckForMouseInput && m_RotateWithMouse && Input.GetAxis(m_RotateMouseHoldButton) != 0)
            {
                totalRotationAngle -= Input.GetAxis(m_RotateMouseYAxis) * m_RotateMouseSpeed;
            }
            if (m_RotateWithJoystick)
            {
                totalRotationAngle -= Input.GetAxis(m_RotateJoystickYAxis) * m_RotateJoystickSpeed;
            }
            if(!totalRotationAngle.Equals(0))
            {
                croCamera.SetTargetedRotationAngle(croCamera.GetTargetedRotationAngle() + totalRotationAngle);
            }
            #endregion //Rotating

            #region Zoom
            float zoom = 0;
            if (m_CheckForKeyboardInput && m_ZoomWithKeyboard)
            {
                zoom += Input.GetAxis(m_ZoomKeyboardAxis) * m_ZoomKeyboardSpeed;
            }
            if (m_ZoomWithMouseWheel)
            {
                zoom += Input.GetAxis(m_ZoomMouseWheelAxis) * m_ZoomMouseSpeed;
            }
            if (m_ZoomWithJoystick)
            {
                zoom += Input.GetAxis(m_ZoomJoystickAxis) * m_ZoomJoystickSpeed;
            }

            croCamera.SetTargetedZoomLevel(croCamera.GetTargetedZoomLevel() - zoom);
            #endregion //Zoom

            #region Clicking
            if(m_CheckForKeyboardInput && m_ClickWithKeyboard)
            {
                if(Input.GetAxis(m_ClickKeyboardButton) != 0)
                {
                    if (!m_OnClickKeyboard)
                    {
                        m_OnClickKeyboard = true;
                        m_OnClickTimeKeyboard = Time.time + m_ClickDetectionTime;
                    }
                }
                else
                {
                    if(m_OnClickKeyboard && m_OnClickTimeKeyboard > Time.time && m_ClickEvent != null)
                    {
                        m_ClickEvent.Invoke();
                    }
                    m_OnClickKeyboard = false;
                }
            }
            if(m_CheckForMouseInput && m_ClickWithMouse)
            {
                if (Input.GetAxis(m_ClickMouseButton) != 0)
                {
                    if (!m_OnClickMouse)
                    {
                        m_OnClickMouse = true;
                        m_OnClickTimeMouse = Time.time + m_ClickDetectionTime;
                    }
                }
                else
                {
                    if (m_OnClickMouse && m_OnClickTimeMouse > Time.time && m_ClickEvent != null)
                    {
                        m_ClickEvent.Invoke();
                    }
                    m_OnClickMouse = false;
                }
            }
            if (m_ClickWithJoystick)
            {
                if (Input.GetAxis(m_ClickJoystickButton) != 0)
                {
                    if (!m_OnClickJoystick)
                    {
                        m_OnClickJoystick = true;
                        m_OnClickTimeJoystick = Time.time + m_ClickDetectionTime;
                    }
                }
                else
                {
                    if (m_OnClickJoystick && m_OnClickTimeJoystick > Time.time && m_ClickEvent != null)
                    {
                        m_ClickEvent.Invoke();
                    }
                    m_OnClickJoystick = false;
                }
            }
            #endregion
        }

        private void ClearOneFingerControls()
        {
            m_OnClickTouch = false;
            m_FirstTime1Finger = true;
            m_OnDraggingWithTouch = false;
        }

        private void ClearTwoFingerControls()
        {
            m_FirstTime2Fingers = true;
            m_OnZoomingWithTouch = false;
            m_OnRotatingWithTouch = false;
        }

        private void ClearDragging()
        {
            m_OnDragging = false;
            m_PanKeyDown = false;
        }

        private void HandleDragging(CRO_Camera croCamera, Vector3 newDragPosiotion)
        {
            m_CurrentPositionInWorldSpace = croCamera.MouseToWorld(m_CameraPositionOnStartDragging, croCamera.GetTargetedPosition().y, newDragPosiotion);
            m_CameraPositionOnStartDragging -= (m_CurrentPositionInWorldSpace - m_StartDragWorldSpace);

            Vector3 pivotPosition = croCamera.GetPivotPosition();
            m_CameraPositionOnStartDragging.y = pivotPosition.y;
            Vector3 dif = m_CameraPositionOnStartDragging - pivotPosition;
            croCamera.SetPivotPosition(m_CameraPositionOnStartDragging);
            croCamera.SetTargetedPosition(croCamera.GetTargetedPosition() + dif);
        }
        #endregion //Handle Controls

        #region Private Serializable Variables
        [SerializeField] private UnityEvent m_ClickEvent;
        [SerializeField] private string m_PanKeyboardAxisHorizontal = "CRO_PanKeyboardHorizontal";
        [SerializeField] private string m_PanKeyboardAxisVertical = "CRO_PanKeyboardVertical";
        [SerializeField] private string m_PanMouseHoldButton = "CRO_PanMouseHoldButton";
        [SerializeField] private string m_PanJoystickAxisHorizontal = "CRO_PanJoystickHorizontal";
        [SerializeField] private string m_PanJoystickAxisVertical = "CRO_PanJoystickVertical";
        [SerializeField] private string m_RotateKeyboardXAxis = "CRO_RotateKeyboardOnXAxis";
        [SerializeField] private string m_RotateKeyboardYAxis = "CRO_RotateKeyboardOnYAxis";
        [SerializeField] private string m_RotateMouseHoldButton = "CRO_RotateMouseHoldButton";
        [SerializeField] private string m_RotateMouseXAxis = "CRO_RotateMouseOnXAxis";
        [SerializeField] private string m_RotateMouseYAxis = "CRO_RotateMouseOnYAxis";
        [SerializeField] private string m_RotateJoystickXAxis = "CRO_RotateJoystickOnXAxis";
        [SerializeField] private string m_RotateJoystickYAxis = "CRO_RotateJoystickOnYAxis";
        [SerializeField] private string m_ZoomKeyboardAxis = "CRO_ZoomKeyboardAxis";
        [SerializeField] private string m_ZoomMouseWheelAxis = "CRO_ZoomMouseWheelAxis";
        [SerializeField] private string m_ZoomJoystickAxis = "CRO_ZoomJoystickAxis";
        [SerializeField] private string m_ClickKeyboardButton = "CRO_ClickKeyboardButton";
        [SerializeField] private string m_ClickMouseButton = "CRO_ClickMouseButton";
        [SerializeField] private string m_ClickJoystickButton = "CRO_ClickJoystickButton";

        [SerializeField] private int m_PanHorizontalBorderSize = 25;
        [SerializeField] private int m_PanVerticalBorderSize = 25;
        [SerializeField] private float m_PanKeyboardMoveSpeed = 10.0f;
        [SerializeField] private float m_PanJoystickMoveSpeed = 10.0f;
        [SerializeField] private float m_PanBorderMoveSpeed = 10.0f;
        [SerializeField] private float m_DragDetectThreshold = 10.0f;
        [SerializeField] private float m_RotateKeyboardSpeed = 2.0f;
        [SerializeField] private float m_RotateMouseSpeed = 2.0f;
        [SerializeField] private float m_RotateJoystickSpeed = 2.0f;
        [SerializeField] private float m_RotateTouchSpeed = 2.0f;
        [SerializeField] private float m_RotateDetectionThreshold = 5.0f;
        [SerializeField] private float m_MinimumRoateAngle = 1.0f;
        [SerializeField] private float m_ZoomKeyboardSpeed = 0.1f;
        [SerializeField] private float m_ZoomMouseSpeed = 0.1f;
        [SerializeField] private float m_ZoomJoystickSpeed = 0.1f;
        [SerializeField] private float m_ZoomTouchSpeed = 0.001f;
        [SerializeField] private float m_ZoomDetectionThreshold = 20.0f;
        [SerializeField] private float m_ClickDetectionTime = 0.3f;
        [SerializeField] private CRO_Camera.TouchRotateGesture m_TouchRotateGesture = CRO_Camera.TouchRotateGesture.Swipe;

        [SerializeField] private bool m_PanWithKeyboard = true;
        [SerializeField] private bool m_PanWithMouse = true;
        [SerializeField] private bool m_PanWithJoystick = true;
        [SerializeField] private bool m_PanWithScreenBorder = true;
        [SerializeField] private bool m_PanWithTouch = true;
        [SerializeField] private bool m_RotateWithKeyboard = true;
        [SerializeField] private bool m_RotateWithMouse = true;
        [SerializeField] private bool m_RotateWithJoystick = true;
        [SerializeField] private bool m_RotateWithTouch = true;
        [SerializeField] private bool m_ZoomWithKeyboard = true;
        [SerializeField] private bool m_ZoomWithMouseWheel = true;
        [SerializeField] private bool m_ZoomWithJoystick = true;
        [SerializeField] private bool m_ZoomWithTouch = true;
        [SerializeField] private bool m_ClickWithKeyboard = false;
        [SerializeField] private bool m_ClickWithMouse = true;
        [SerializeField] private bool m_ClickWithJoystick = false;
        [SerializeField] private bool m_ClickWithTouch = true;
        #endregion //Private Serializable Variables

        #region Unity Editor region
#if UNITY_EDITOR
        public void OnEnable(SerializedObject serializedObject)
        {
            m_OnClickProperty = serializedObject.FindProperty("m_StandardInput.m_ClickEvent");
        }

        private SerializedProperty m_OnClickProperty;

        private static string s_DefineThisInInput = "You need to define this in 'Edit' -> 'Project Settings' -> 'Input' or using CRO Camera wizard. If this is not found at runtime the action will be disabled.";
        private static GUIContent m_KeyboardPanField = new GUIContent("Keyboard: ", "Pan with keyboard input. To use this you must not follow a target.");
        private static GUIContent m_KeyboardPanHAxisField = new GUIContent("Horizontal axis name: ", s_DefineThisInInput);
        private static GUIContent m_KeyboardPanVAxisField = new GUIContent("Vertical axis name: ", s_DefineThisInInput);
        private static GUIContent m_KeyboardPanMoveSpeedField = new GUIContent("Movement speed: ", "Specify how fast the camera will move when using the keyboard.");
        private static GUIContent m_MousePanField = new GUIContent("Mouse: ", "Use the mouse to pan. To use this you must not follow a target.");
        private static GUIContent m_MousePanHoldAxisField = new GUIContent("Hold axis name: ", s_DefineThisInInput);
        private static GUIContent m_JoystickPanField = new GUIContent("Joystick: ", "Use the joystick to pan. To use this you must not follow a target.");
        private static GUIContent m_JoystickPanHAxisField = new GUIContent("Horizontal axis name: ", s_DefineThisInInput);
        private static GUIContent m_JoystickPanVAxisField = new GUIContent("Vertical axis name: ", s_DefineThisInInput);
        private static GUIContent m_JoystickPanSpeedField = new GUIContent("Movement speed: ", "Specify how fast the camera will move when using the joystick.");
        private static GUIContent m_ScreenEdgePanField = new GUIContent("Screen edge input: ", "Move the camera when the mouse is close to the edge of the screen. To use this you must not follow a target.");
        private static GUIContent m_ScreenEdgeHBorderField = new GUIContent("Horizontal border: ", "Specify the thickness of the border on the left and on the right");
        private static GUIContent m_ScreenEdgeVBorderField = new GUIContent("Vertical border: ", "Specify the thickness of the border on the top and on the bottom");
        private static GUIContent m_ScreenEdgeMoveSpeed = new GUIContent("Movement speed: ", "Specify how fast the camera will move when the mouse is inside the defined border.");
        private static GUIContent m_PanTouchField = new GUIContent("Pan using touch: ", "Use touch to pan");
        private static GUIContent m_DragDetectionField = new GUIContent("Threshold: ", "Threshold is defining the distance in pixels you must move the mouse on desktop or finger on touch screen, before we consider that you are actually trying to drag.");
        private static GUIContent m_KeyboardRotateField = new GUIContent("Keyboard: ", "Use the keyboard to rotate around the target.");
        private static GUIContent m_KeyboardRotateYAxisField = new GUIContent("Y axis name: ", s_DefineThisInInput);
        private static GUIContent m_KeyboardRotateSpeedField = new GUIContent("Rotate speed: ", "Specify how fast the camera should rotate when using the keyboard.");
        private static GUIContent m_MouseRotateField = new GUIContent("Mouse: ", "Use the mouse to rotate around the target.");
        private static GUIContent m_MouseRotateYAxisField = new GUIContent("Y axis name: ", s_DefineThisInInput);
        private static GUIContent m_MouseRotateHoldButtonField = new GUIContent("Rotate when holding: ", s_DefineThisInInput);
        private static GUIContent m_MouseRotateSpeedField = new GUIContent("Rotate speed: ", "Specify how fast the camera should rotate when using the mouse.");
        private static GUIContent m_JoystickRotateField = new GUIContent("Joystick: ", "Use the joystick to rotate around the target.");
        private static GUIContent m_JoystickRotateYAxisField = new GUIContent("Y axis name: ", s_DefineThisInInput);
        private static GUIContent m_JoystickRotateSpeedField = new GUIContent("Rotate speed: ", "Specify how fast the camera should rotate when using the joystick.");
        private static GUIContent m_TouchRotateField = new GUIContent("Rotate using touch: ", "Use touch to rotate around the target.");
        private static GUIContent m_RotateGestureField = new GUIContent("Rotate gesture:", "What type of of gesture is wanted to be used for rotate.");
        private static GUIContent m_TouchRotateSpeedField = new GUIContent("Rotate speed: ", "Specify how fast the camera should rotate when using the touch controls.");
        private static GUIContent m_RotateDetectField = new GUIContent("Threshold: ", "Define the angle threshold that will indicate that you are trying to rotate");
        private static GUIContent m_KeyboardZoomField = new GUIContent("Keyboard: ", "Use the keyboard to zoom to target.");
        private static GUIContent m_KeyboardZoomAxisField = new GUIContent("Zoom axis name: ", s_DefineThisInInput);
        private static GUIContent m_MouseZoomAxisField = new GUIContent("Zoom axis name: ", s_DefineThisInInput);
        private static GUIContent m_KeyboardZoomSpeedField = new GUIContent("Zoom speed: ", "Specify how fast the camera should zoom when using the keyboard.");
        private static GUIContent m_MouseZoomField = new GUIContent("Mouse Wheel", "Use the mouse to zoom to target.");
        private static GUIContent m_MouseZoomSpeedField = new GUIContent("Zoom speed: ", "Specify how fast the camera should zoom when using the mouse wheel.");
        private static GUIContent m_JoystickZoomField = new GUIContent("Joystick: ", "Use the joystick to zoom to target.");
        private static GUIContent m_JoystickZoomAxisField = new GUIContent("Zoom axis name: ", s_DefineThisInInput);
        private static GUIContent m_JoystickZoomSpeedField = new GUIContent("Zoom speed: ", "Specify how fast the camera should zoom when using the joystick.");
        private static GUIContent m_TouchZoomField = new GUIContent("Zoom using touch: ", "Use touch to zoom to target.");
        private static GUIContent m_TouchZoomSpeedField = new GUIContent("Zoom speed: ", "Specify how fast the camera should zoom when using the touch controls.");
        private static GUIContent m_ZoomDetectionField = new GUIContent("Threshold: ", "Need to specify how many pixels you need to get your fingers closer or apart before we detect that you are trying to zoom.");
        private static GUIContent m_ClickKeyboardButtonField = new GUIContent("Click Keyboard axis: ", s_DefineThisInInput);
        private static GUIContent m_ClickMouseButtonField = new GUIContent("Click Mouse axis: ", s_DefineThisInInput);
        private static GUIContent m_ClickJoystickButtonField = new GUIContent("Click Joystick axis: ", s_DefineThisInInput);
        private static GUIContent m_ClickKeyboardField = new GUIContent("Click using keyboard: ", "Use keyboard to click");
        private static GUIContent m_ClickMouseField = new GUIContent("Click using mouse: ", "Use mouse to click");
        private static GUIContent m_ClickJoystickField = new GUIContent("Click using joystick: ", "Use joystick to click");
        private static GUIContent m_ClickTouchField = new GUIContent("Click using touch: ", "Use touch to click");
        private static GUIContent m_ClickDetectionField = new GUIContent("Click detection time: ", "Max time between button down and button up that will count as a click.");
        private static GUIContent m_OnClickField = new GUIContent("On Click: ", "What event to trigger when click is detected.");

        private void ShowMissingAxisErrorMessage(string axisName)
        {
            if (!CheckForAxis(axisName))
            {
                EditorGUILayout.HelpBox("We didn't find the name: '" + axisName + "' in the input manager ('Edit' -> 'Project Settings' -> 'Input'). Please add it there or the control will be disabled at runtime.", MessageType.Error);
            }
        }

        public void Editor_DrawControls()
        {
            GUILayout.Label("Control", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.HelpBox("This control is using Unity standard input controller. The input controller can be found in 'Edit' -> 'Project Settings' -> 'Input', also if you want to use default settings you can use 'GameObject' -> 'CodeRoadOne' -> 'CRO-Camera' wizard to update the inputs.", MessageType.Info);

            #region Panning
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.indentLevel++;
                m_EditorShowPanning = EditorGUILayout.Foldout(m_EditorShowPanning, "Panning", true);
                EditorGUI.indentLevel--;
                if (m_EditorShowPanning)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_KeyboardPanField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_PanWithKeyboard = EditorGUILayout.Toggle(m_PanWithKeyboard);
                        EditorGUILayout.EndHorizontal();
                        if (m_PanWithKeyboard)
                        {
                            m_PanKeyboardAxisHorizontal = EditorGUILayout.TextField(m_KeyboardPanHAxisField, m_PanKeyboardAxisHorizontal);
                            m_PanKeyboardAxisVertical = EditorGUILayout.TextField(m_KeyboardPanVAxisField, m_PanKeyboardAxisVertical);
                            m_PanKeyboardMoveSpeed = EditorGUILayout.FloatField(m_KeyboardPanMoveSpeedField, m_PanKeyboardMoveSpeed);
                            ShowMissingAxisErrorMessage(m_PanKeyboardAxisHorizontal);
                            ShowMissingAxisErrorMessage(m_PanKeyboardAxisVertical);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_MousePanField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_PanWithMouse = EditorGUILayout.Toggle(m_PanWithMouse);
                        EditorGUILayout.EndHorizontal();
                        if (m_PanWithMouse)
                        {
                            m_PanMouseHoldButton = EditorGUILayout.TextField(m_MousePanHoldAxisField, m_PanMouseHoldButton);
                            ShowMissingAxisErrorMessage(m_PanMouseHoldButton);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_JoystickPanField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_PanWithJoystick = EditorGUILayout.Toggle(m_PanWithJoystick);
                        EditorGUILayout.EndHorizontal();
                        if (m_PanWithJoystick)
                        {
                            m_PanJoystickAxisHorizontal = EditorGUILayout.TextField(m_JoystickPanHAxisField, m_PanJoystickAxisHorizontal);
                            m_PanJoystickAxisVertical = EditorGUILayout.TextField(m_JoystickPanVAxisField, m_PanJoystickAxisVertical);
                            m_PanJoystickMoveSpeed = EditorGUILayout.FloatField(m_JoystickPanSpeedField, m_PanJoystickMoveSpeed);
                            ShowMissingAxisErrorMessage(m_PanJoystickAxisHorizontal);
                            ShowMissingAxisErrorMessage(m_PanJoystickAxisVertical);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ScreenEdgePanField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_PanWithScreenBorder = EditorGUILayout.Toggle(m_PanWithScreenBorder);
                        EditorGUILayout.EndHorizontal();
                        if (m_PanWithScreenBorder)
                        {
                            m_PanHorizontalBorderSize = EditorGUILayout.IntField(m_ScreenEdgeHBorderField, m_PanHorizontalBorderSize);
                            m_PanVerticalBorderSize = EditorGUILayout.IntField(m_ScreenEdgeVBorderField, m_PanVerticalBorderSize);
                            m_PanBorderMoveSpeed = EditorGUILayout.FloatField(m_ScreenEdgeMoveSpeed, m_PanBorderMoveSpeed);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_PanTouchField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_PanWithTouch = EditorGUILayout.Toggle(m_PanWithTouch);
                        EditorGUILayout.EndHorizontal();
                    }
                    if (m_PanWithTouch)
                    {
                        EditorGUILayout.HelpBox("Panning with touch will work on IOS, ANDROID and any other platform that support touch controls.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Panning will NOT work on IOS, ANDROID and any other platform that support touch controls, if you don't enable the above control.", MessageType.Warning);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        GUILayout.Label("Dragging detection", EditorStyles.boldLabel);
                        m_DragDetectThreshold = EditorGUILayout.FloatField(m_DragDetectionField, m_DragDetectThreshold);
                        EditorGUILayout.HelpBox("The above control is used for touch controls and mouse panning.", MessageType.Warning);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
            #endregion //Panning

            #region Rotating
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.indentLevel++;
                m_EditorShowRotating = EditorGUILayout.Foldout(m_EditorShowRotating, "Rotating", true);
                EditorGUI.indentLevel--;
                if (m_EditorShowRotating)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_KeyboardRotateField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_RotateWithKeyboard = EditorGUILayout.Toggle(m_RotateWithKeyboard);
                        EditorGUILayout.EndHorizontal();
                        if (m_RotateWithKeyboard)
                        {
                            m_RotateKeyboardYAxis = EditorGUILayout.TextField(m_KeyboardRotateYAxisField, m_RotateKeyboardYAxis);
                            m_RotateKeyboardSpeed = EditorGUILayout.FloatField(m_KeyboardRotateSpeedField, m_RotateKeyboardSpeed);
                            ShowMissingAxisErrorMessage(m_RotateKeyboardYAxis);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_MouseRotateField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_RotateWithMouse = EditorGUILayout.Toggle(m_RotateWithMouse);
                        EditorGUILayout.EndHorizontal();
                        if (m_RotateWithMouse)
                        {
                            m_RotateMouseYAxis = EditorGUILayout.TextField(m_MouseRotateYAxisField, m_RotateMouseYAxis);
                            m_RotateMouseHoldButton = EditorGUILayout.TextField(m_MouseRotateHoldButtonField, m_RotateMouseHoldButton);
                            m_RotateMouseSpeed = EditorGUILayout.FloatField(m_MouseRotateSpeedField, m_RotateMouseSpeed);
                            ShowMissingAxisErrorMessage(m_RotateMouseYAxis);
                            ShowMissingAxisErrorMessage(m_RotateMouseHoldButton);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_JoystickRotateField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_RotateWithJoystick = EditorGUILayout.Toggle(m_RotateWithJoystick);
                        EditorGUILayout.EndHorizontal();
                        if (m_RotateWithJoystick)
                        {
                            m_RotateJoystickYAxis = EditorGUILayout.TextField(m_JoystickRotateYAxisField, m_RotateJoystickYAxis);
                            m_RotateJoystickSpeed = EditorGUILayout.FloatField(m_JoystickRotateSpeedField, m_RotateJoystickSpeed);
                            ShowMissingAxisErrorMessage(m_RotateJoystickYAxis);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_TouchRotateField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_RotateWithTouch = EditorGUILayout.Toggle(m_RotateWithTouch);
                        EditorGUILayout.EndHorizontal();
                        if (m_RotateWithTouch)
                        {
                            m_TouchRotateGesture = (CRO_Camera.TouchRotateGesture)EditorGUILayout.EnumPopup(m_RotateGestureField, m_TouchRotateGesture);
                            m_RotateTouchSpeed = EditorGUILayout.FloatField(m_TouchRotateSpeedField, m_RotateTouchSpeed);
                            GUILayout.Label("Rotate detection");
                            m_RotateDetectionThreshold = EditorGUILayout.FloatField(m_RotateDetectField, m_RotateDetectionThreshold);
                        }
                    }
                    if (m_RotateWithTouch)
                    {
                        EditorGUILayout.HelpBox("Rotating with touch will work on IOS, ANDROID and any other platform that support touch controls.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Rotating will NOT work on IOS, ANDROID and any other platform that support touch controls, if you don't enable the above control.", MessageType.Warning);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
            #endregion //Rotating

            #region Zooming
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.indentLevel++;
                m_EditorShowZoom = EditorGUILayout.Foldout(m_EditorShowZoom, "Zooming", true);
                EditorGUI.indentLevel--;
                if (m_EditorShowZoom)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_KeyboardZoomField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ZoomWithKeyboard = EditorGUILayout.Toggle(m_ZoomWithKeyboard);
                        EditorGUILayout.EndHorizontal();
                        if (m_ZoomWithKeyboard)
                        {
                            m_ZoomKeyboardAxis = EditorGUILayout.TextField(m_KeyboardZoomAxisField, m_ZoomKeyboardAxis);
                            m_ZoomKeyboardSpeed = EditorGUILayout.FloatField(m_KeyboardZoomSpeedField, m_ZoomKeyboardSpeed);
                            ShowMissingAxisErrorMessage(m_ZoomKeyboardAxis);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_MouseZoomField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ZoomWithMouseWheel = EditorGUILayout.Toggle(m_ZoomWithMouseWheel);
                        EditorGUILayout.EndHorizontal();
                        if (m_ZoomWithMouseWheel)
                        {
                            m_ZoomMouseWheelAxis = EditorGUILayout.TextField(m_MouseZoomAxisField, m_ZoomMouseWheelAxis);
                            m_ZoomMouseSpeed = EditorGUILayout.FloatField(m_MouseZoomSpeedField, m_ZoomMouseSpeed);
                            ShowMissingAxisErrorMessage(m_ZoomMouseWheelAxis);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_JoystickZoomField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ZoomWithJoystick = EditorGUILayout.Toggle(m_ZoomWithJoystick);
                        EditorGUILayout.EndHorizontal();
                        if (m_ZoomWithJoystick)
                        {
                            m_ZoomJoystickAxis = EditorGUILayout.TextField(m_JoystickZoomAxisField, m_ZoomJoystickAxis);
                            m_ZoomJoystickSpeed = EditorGUILayout.FloatField(m_JoystickZoomSpeedField, m_ZoomJoystickSpeed);
                            ShowMissingAxisErrorMessage(m_ZoomJoystickAxis);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_TouchZoomField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ZoomWithTouch = EditorGUILayout.Toggle(m_ZoomWithTouch);
                        EditorGUILayout.EndHorizontal();
                        if (m_ZoomWithTouch)
                        {
                            m_ZoomTouchSpeed = EditorGUILayout.FloatField(m_TouchZoomSpeedField, m_ZoomTouchSpeed);
                            GUILayout.Label("Zoom detection");
                            m_ZoomDetectionThreshold = EditorGUILayout.FloatField(m_ZoomDetectionField, m_ZoomDetectionThreshold);
                        }
                        if (m_ZoomWithTouch)
                        {
                            EditorGUILayout.HelpBox("Zooming with touch will work on IOS, ANDROID and any other platform that support touch controls.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Zooming will NOT work on IOS, ANDROID and any other platform that support touch controls, if you don't enable the above control.", MessageType.Warning);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
            #endregion //Zooming

            #region Clicking
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.indentLevel++;
                m_EditorShowClick = EditorGUILayout.Foldout(m_EditorShowClick, "Clicking", true);
                EditorGUI.indentLevel--;
                if(m_EditorShowClick)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ClickKeyboardField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ClickWithKeyboard = EditorGUILayout.Toggle(m_ClickWithKeyboard);
                        EditorGUILayout.EndHorizontal();
                        if (m_ClickWithKeyboard)
                        {
                            m_ClickKeyboardButton = EditorGUILayout.TextField(m_ClickKeyboardButtonField, m_ClickKeyboardButton);
                            ShowMissingAxisErrorMessage(m_ClickKeyboardButton);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ClickMouseField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ClickWithMouse = EditorGUILayout.Toggle(m_ClickWithMouse);
                        EditorGUILayout.EndHorizontal();
                        if (m_ClickWithMouse)
                        {
                            m_ClickMouseButton = EditorGUILayout.TextField(m_ClickMouseButtonField, m_ClickMouseButton);
                            ShowMissingAxisErrorMessage(m_ClickMouseButton);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ClickJoystickField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ClickWithJoystick = EditorGUILayout.Toggle(m_ClickWithJoystick);
                        EditorGUILayout.EndHorizontal();
                        if (m_ClickWithJoystick)
                        {
                            m_ClickJoystickButton = EditorGUILayout.TextField(m_ClickJoystickButtonField, m_ClickJoystickButton);
                            ShowMissingAxisErrorMessage(m_ClickJoystickButton);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ClickTouchField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_ClickWithTouch = EditorGUILayout.Toggle(m_ClickWithTouch);
                        EditorGUILayout.EndHorizontal();
                        if (m_ClickWithTouch)
                        {
                            EditorGUILayout.HelpBox("Zooming with touch will work on IOS, ANDROID and any other platform that support touch controls.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Zooming will NOT work on IOS, ANDROID and any other platform that support touch controls, if you don't enable the above control.", MessageType.Warning);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        GUILayout.Label("Click detection", EditorStyles.boldLabel);
                        m_ClickDetectionTime = EditorGUILayout.FloatField(m_ClickDetectionField, m_ClickDetectionTime);
                        if (m_OnClickProperty != null)
                        {
                            EditorGUILayout.PropertyField(m_OnClickProperty, m_OnClickField);
                        }
                        EditorGUILayout.HelpBox("The above control is used by all controls.", MessageType.Warning);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
            #endregion //Clicking
        }


        private static bool IsAxisDefined(SerializedProperty axesProperty, string axisName)
        {
            SerializedProperty child = axesProperty.Copy();
            child.Next(true);
            child.Next(true);

            while (child.Next(false))
            {
                SerializedProperty axis = child.Copy();
                axis.Next(true);
                if (axis.stringValue == axisName)
                {
                    return true;
                }
            }

            return false;
        }

        private static SerializedProperty GetAxis(SerializedProperty axesProperty, string axisName)
        {
            SerializedProperty child = axesProperty.Copy();
            child.Next(true);
            child.Next(true);

            while (child.Next(false))
            {
                SerializedProperty axis = child.Copy();
                axis.Next(true);
                if (axis.stringValue == axisName)
                {
                    return child;
                }
            }

            return null;
        }

        private static SerializedProperty GetChildProperty(SerializedProperty parent, string name)
        {
            SerializedProperty child = parent.Copy();
            child.Next(true);
            do
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            while (child.Next(false));
            return null;
        }

        public void Editor_AddOrUpdateInputData(bool overwriteExistingAxes)
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

            //Add/Update pan keyboard axis
            AddOrUpdate(m_PanKeyboardAxisHorizontal, "Keyboard panning Left", "Keyboard panning Right", "left", "right", "a", "d", 3, 0.05f, 3, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_PanKeyboardAxisVertical, "Keyboard panning Up", "Keyboard panning Down", "down", "up", "s", "w", 3, 0.05f, 3, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_PanMouseHoldButton, "Panning when holding", "", "", "left ctrl", "", "mouse 2", 1000, 0.0001f, 1000, false, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_PanJoystickAxisHorizontal, "Joystick panning Left", "Joystick panning Right", "", "", "", "", 3, 0.05f, 3, true, false, 2, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_PanJoystickAxisVertical, "Joystick panning Up", "Joystick panning Down", "", "", "", "", 3, 0.05f, 3, true, true, 2, 1, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_RotateKeyboardYAxis, "Keyboard rotate Left", "Keyboard rotate Right", "e", "r", "", "", 3, 0.1f, 3, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_RotateMouseYAxis, "Mouse rotation", "", "", "", "", "", 0, 0, 0.1f, false, false, 1, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_RotateMouseHoldButton, "Rotate when holding", "", "", "left alt", "", "mouse 1", 1000, 0.0001f, 1000, false, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_RotateJoystickYAxis, "Joystick rotation", "", "", "", "", "", 3, 0.1f, 3, true, false, 2, 2, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ZoomKeyboardAxis, "Keyboard zoom out", "Keyboard zoom in", "[-]", "[+]", "page down", "page up", 3, 0.05f, 3, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ZoomMouseWheelAxis, "Mouse wheel +", "Mouse wheel -", "", "", "", "", 0, 0, 1, false, false, 1, 2, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ZoomJoystickAxis, "Joystick zoom out", "Joystick zoom in", "joystick button 4", "joystick button 5", "", "", 3, 0.1f, 3, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ClickKeyboardButton, "Keyboard click", "", "", "space", "", "", 1000, 0.0001f, 1000, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ClickMouseButton, "Mouse click", "", "", "mouse 0", "", "", 1000, 0.0001f, 1000, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);
            AddOrUpdate(m_ClickJoystickButton, "Joystick click", "", "", "joystick button 0", "", "", 1000, 0.0001f, 1000, true, false, 0, 0, 0, axesProperty, overwriteExistingAxes);

            serializedObject.ApplyModifiedProperties();
        }

        private void AddOrUpdate(string name, string descriptiveName, string descriptiveNegativeName, string negativeButton, string positiveButton, string altNegativeButton, string altPositiveButton, float gravity, float dead, float sensitivity, bool snap, bool invert, int axisType, int axis, int joyNum, SerializedProperty axesProperty, bool overwrite)
        {
            SerializedProperty axisProperty = GetAxis(axesProperty, name);
            bool newAdded = false;
            if (axisProperty == null)
            {
                Debug.Log("Axis " + name + " not found, we will add it to input manager.");
                axesProperty.arraySize++;
                axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
                newAdded = true;
            }
            ApplyAxisProperty(name, descriptiveName, descriptiveNegativeName, negativeButton, positiveButton, altNegativeButton, altPositiveButton, gravity, dead, sensitivity, snap, invert, axisType, axis, joyNum, axisProperty, newAdded, overwrite);
        }

        private static void ApplyAxisProperty(string name, string descriptiveName, string descriptiveNegativeName, string negativeButton, string positiveButton, string altNegativeButton, string altPositiveButton, float gravity, float dead, float sensitivity, bool snap, bool invert, int axisType, int axis, int joyNum, SerializedProperty axisProperty, bool newAdded, bool overwrite)
        {
            bool paramsDifferent = false;
            if(!newAdded)
            {
                //Check to see if the parameters are different
                if (GetChildProperty(axisProperty, "m_Name").stringValue != name ||
                    GetChildProperty(axisProperty, "descriptiveName").stringValue != descriptiveName ||
                    GetChildProperty(axisProperty, "descriptiveNegativeName").stringValue != descriptiveNegativeName ||
                    GetChildProperty(axisProperty, "negativeButton").stringValue != negativeButton ||
                    GetChildProperty(axisProperty, "positiveButton").stringValue != positiveButton ||
                    GetChildProperty(axisProperty, "altNegativeButton").stringValue != altNegativeButton ||
                    GetChildProperty(axisProperty, "altPositiveButton").stringValue != altPositiveButton ||
                    !GetChildProperty(axisProperty, "gravity").floatValue.Equals(gravity) ||
                    !GetChildProperty(axisProperty, "dead").floatValue.Equals(dead) ||
                    !GetChildProperty(axisProperty, "sensitivity").floatValue.Equals(sensitivity) ||
                    GetChildProperty(axisProperty, "snap").boolValue != snap ||
                    GetChildProperty(axisProperty, "invert").boolValue != invert ||
                    GetChildProperty(axisProperty, "type").intValue != axisType ||
                    GetChildProperty(axisProperty, "axis").intValue != axis ||
                    GetChildProperty(axisProperty, "joyNum").intValue != joyNum)
                {
                    paramsDifferent = true;
                    if (!overwrite)
                    {
                        Debug.LogWarning("The axis " + name + " already exists in Input Manager, but contains different values. As you didn't choose to overwrite the data nothing will happen. If this is not intentional use the overwrite or change the name for the axis.");
                    }
                    else
                    {
                        Debug.LogWarning("The axis " + name + " already exists in Input Manager and it will be reseted to the default parameters. If this is not intentional you can undo the last operation.");
                    }
                }
                else
                {
                    Debug.Log("Axis: " + name + " already exists in Input Manager and is already setup with the default values. Nothing will happen.");
                    return;
                }
            }

            if (!paramsDifferent || overwrite)
            {
                GetChildProperty(axisProperty, "m_Name").stringValue = name;
                GetChildProperty(axisProperty, "descriptiveName").stringValue = descriptiveName;
                GetChildProperty(axisProperty, "descriptiveNegativeName").stringValue = descriptiveNegativeName;
                GetChildProperty(axisProperty, "negativeButton").stringValue = negativeButton;
                GetChildProperty(axisProperty, "positiveButton").stringValue = positiveButton;
                GetChildProperty(axisProperty, "altNegativeButton").stringValue = altNegativeButton;
                GetChildProperty(axisProperty, "altPositiveButton").stringValue = altPositiveButton;
                GetChildProperty(axisProperty, "gravity").floatValue = gravity;
                GetChildProperty(axisProperty, "dead").floatValue = dead;
                GetChildProperty(axisProperty, "sensitivity").floatValue = sensitivity;
                GetChildProperty(axisProperty, "snap").boolValue = snap;
                GetChildProperty(axisProperty, "invert").boolValue = invert;
                GetChildProperty(axisProperty, "type").intValue = axisType;
                GetChildProperty(axisProperty, "axis").intValue = axis;
                GetChildProperty(axisProperty, "joyNum").intValue = joyNum;
            }
        }

        public bool m_EditorShowPanning = false;
        public bool m_EditorShowRotating = false;
        public bool m_EditorShowZoom = false;
        public bool m_EditorShowClick = false;
#endif
        #endregion //Unity Editor region
    }
}