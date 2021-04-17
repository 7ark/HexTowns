using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace CodeRoadOne
{
    public class CRO_Camera : MonoBehaviour
    {
        #region Setup
        #region Getters
        public Camera GetCamera() { return m_Camera; }
        public Transform GetTarget() { return m_Target; }
        public Vector3 GetTargetedPosition() { return m_TargetPosition; }
        public Vector2 GetMapLimitLow() { return m_MapLimitLow; }
        public Vector2 GetMapLimitHigh() { return m_MapLimitHigh; }
        public float GetSmoothMoveSpeed() { return m_SmoothMoveSpeed; }
        public float GetSmoothRotationSpeed() { return m_SmoothRotationSpeed; }
        public float GetSmoothZoomSpeed() { return m_SmoothZoomSpeed; }
        public float GetSmoothAdjDistance() { return m_SmoothAdjDistance; }
        public bool IsFollowingTarget() { return m_Target != null; }
        public ProcessInput GetProcessInput() { return m_ProcessInput; }
        #endregion
        #region Setters
        public void SetCamera(Camera camera) { m_Camera = camera; }
        public void SetTarget(Transform target) { m_Target = target; }
        public void SetFreeCam(bool keepCurrentPosition)
        {
            if(keepCurrentPosition && m_Target)
            {
                SetTargetedPosition(m_Target.position);
            }
            m_Target = null;
        }
        public void SetTargetedPosition(Vector3 position)
        {
            m_TargetPosition.x = Mathf.Clamp(position.x, m_MapLimitLow.x, m_MapLimitHigh.x);
            m_TargetPosition.z = Mathf.Clamp(position.z, m_MapLimitLow.y, m_MapLimitHigh.y);
            m_TargetPosition.y = position.y;
        }
        public void SetMapLimitLow(Vector2 limitLow) { m_MapLimitLow = limitLow; }
        public void SetMapLimitHigh(Vector2 limitHigh) { m_MapLimitHigh = limitHigh; }
        public void SetSmoothMoveSpeed(float value) { m_SmoothMoveSpeed = Mathf.Clamp(value, 0.01f, 1.0f); }
        public void SetSmoothRotationSpeed(float value) { m_SmoothRotationSpeed = Mathf.Clamp(value, 0.01f, 1.0f); }
        public void SetSmoothZoomSpeed(float value) { m_SmoothZoomSpeed = Mathf.Clamp(value, 0.01f, 1.0f); }
        public void SetSmoothAdjDistance(float value) { m_SmoothAdjDistance = Mathf.Clamp(value, 0.01f, 1.0f); }
        public void SetProcessInput(ProcessInput processInput) { m_ProcessInput = processInput; }
        #endregion
        #endregion

        #region Distance
        #region Getters
        public Vector2 GetOrbitRadius() { return m_OrbitRadius; }
        public Vector2 GetOrbitHeight() { return m_OrbitHeight; }
        public Vector2 GetOrthographicSize() { return m_OrthographicSize; }
        public float GetDefaultOrientation() { return m_DefaultOrientation; }
        public float GetDefaultZoomLevel() { return m_DefaultZoomLevel; }
        #endregion
        #region Setters
        public void SetOrbitRadius(Vector2 radius) { m_OrbitRadius = radius; }
        public void SetOrbitHeight(Vector2 height) { m_OrbitHeight = height; }
        public void SetOrthographicSize(Vector2 size) { m_OrthographicSize.x = Mathf.Clamp(size.x, 0.01f, 1000.0f); m_OrthographicSize.y = Mathf.Clamp(size.y, 0.01f, 1000.0f); }
        public void SetDefaultOrientation(float orientation) { m_DefaultOrientation = Mathf.Clamp(orientation, -180.0f, 180.0f); }
        public void SetDefaultZoomLevel(float zoom) { m_DefaultZoomLevel = Mathf.Clamp01(zoom); }
        #endregion
        #endregion

        #region Control
        public enum TouchRotateGesture
        {
            Swipe,
            Rotate
        };
        #region Getters
        public CRO_CameraStandardInput GetStandardInput() { return m_StandardInput; }
        #endregion
        #region Setters
        public void SetStandardInput(CRO_CameraStandardInput standardInput) { m_StandardInput = standardInput; }
        #endregion
        #endregion

        #region Collision
        public enum CollisionType
        {
            Capsule,
            Box
        };
        #region Getters
        public bool GetUseCameraCollision() { return m_UseCameraCollision; }
        public LayerMask GetCollisionMask() { return m_CollisionMasks; }
        public float GetCapsuleRadius() { return m_CapsuleRadius; }
        public float GetCapsuleLength() { return m_CapsuleLength; }
        public float GetHeightCast() { return m_HeightCast; }
        public float GetPerUnitInHeightCloser() { return m_PerUnitInHeightCloser; }
        public float GetBoxHeight() { return m_BoxHeight; }
        public CollisionType GetCollisionTypeCheck() { return m_CollisionTypeCheck; }
        #endregion
        #region Setters
        public void SetUseCameraCollision(bool useCameraCollision) { m_UseCameraCollision = useCameraCollision; }
        public void SetCollisionMask(LayerMask collisionMask) { m_CollisionMasks = collisionMask; }
        public void SetCapsuleRadius(float capsuleRadius) { m_CapsuleRadius = capsuleRadius; }
        public void SetCapsuleLength(float capsuleLength) { m_CapsuleLength = capsuleLength; }
        public void SetHeightCast(float heightCast) { m_HeightCast = heightCast; }
        public void SetPerUnitInHeightCloser(float perUnitInHeightCloser) { m_PerUnitInHeightCloser = perUnitInHeightCloser; }
        public void SetBoxHeight(float boxHeight) { m_BoxHeight = boxHeight; }
        public void SetCollisionTypeCheck(CollisionType collisionTypeCheck) { m_CollisionTypeCheck = collisionTypeCheck; }
        #endregion
        #endregion

        #region Public Helper Methods
        //Get current camera orientation
        public Quaternion GetCameraOrientation() { return m_CameraOrientation; }
        //Get current camera forward
        public Vector3 GetCameraForward() { return m_CameraForward; }
        //Get current pivot position
        public Vector3 GetPivotPosition() { return m_PivotTransform.position; }

        //Get the targeted zoom level. This is where the camera level should get. Is not necessary where the camera zoom level is in this moment
        public float GetTargetedZoomLevel() { return m_TargetZoomLevel; }
        //Get the targeted rotation angle. Same as for the zoom, is where the camera should rotate now where is the camera now
        public float GetTargetedRotationAngle() { return m_TargetRotationAngle; }

        //Set where the camera should get in terms of zoom. It will smoothly interpolate towards that zoom level
        public void SetTargetedZoomLevel(float zoom)
        {
            zoom = Mathf.Clamp01(zoom);
            m_ComputeRadiusAndOffest |= !zoom.Equals(m_TargetZoomLevel);
            m_TargetZoomLevel = zoom;
        }
        //Set where the camera should rotate. It will smoothly interpolate towards that rotation angle
        public void SetTargetedRotationAngle(float angle) { m_TargetRotationAngle = angle; }
        //Set where the pivot of the camera should be.
        public void SetPivotPosition(Vector3 position) { m_PivotTransform.position = position; }

        //Computes an offset taking into account the angle of rotation around Y on a circle with a specific radius at specific height.
        public Vector3 GetOffset(float angle, float radius, float height)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);
            Vector3 offset;
            offset.x = radius * cosAngle;
            offset.z = radius * sinAngle;
            offset.y = height;

            return offset;
        }

        //Using the current camera and a specific position, we re construct a plane at the specified height (iY). Using the mouse position we will return where we intersect the constructed plane. This helps not to have collision on all objects and still be able to identify where we clicked.
        public Vector3 MouseToWorld(Vector3 iCameraPosition, float iY, Vector3 iMousePosition)
        {
            Vector3 lPlanePosition = iCameraPosition;
            lPlanePosition.y = iY;
            Plane lWorldPlane = new Plane(Vector3.up, lPlanePosition);
            Ray lRay = m_Camera.ScreenPointToRay(iMousePosition);
            float lDistance;
            if (lWorldPlane.Raycast(lRay, out lDistance))
            {
                return lRay.GetPoint(lDistance);
            }
            return Vector3.zero;
        }

        //Get the object under the mouse. It will return true in case there is valid information inside hitInfo.
        public bool GetObjectUnderMouse(out RaycastHit hitInfo)
        {
            m_RayScreenToWorld = m_Camera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(m_RayScreenToWorld, out hitInfo);
        }

        //In case you changed setting at runtime, you might need to call this function to readjust the camera position and zoom levels based on the new settings
        public void SettingsChanged()
        {
            ComputeCurrentRadiusAndHeight();
            ComputeCameraOffsetPosition();
        }

        //If you want the camera to jump to new settings and not smoothly interpolate between current position/orientation/zoom call this function
        public void SnapToDefault()
        {
#if UNITY_EDITOR
            //This is done to avoid a null when we call SnapToDefault from inside the editor CRO_Camera inspector or from CRO_Camera creator
            m_PivotTransform = transform;
            m_CameraTransform = m_Camera.transform;
#endif
            m_TargetZoomLevel = m_DefaultZoomLevel;
            m_TargetRotationAngle = m_DefaultOrientation;
            m_PivotTransform.rotation = Quaternion.identity;

            m_VelocityCameraPosition = Vector3.zero;
            m_VelocityZoom = 0;
            m_VelocityRotate = 0;
            m_TargetRadiusOffsetAdj = 0;

            m_CurrentRotationAngle = m_TargetRotationAngle;
            m_CurrentZoomLevel = m_TargetZoomLevel;
            m_CurrentRadiusOffsetAdj = 0;
            m_CurrentOrtographicSize = m_OrthographicSize.y;

            SettingsChanged();

            m_CameraTransform.localPosition = m_CameraOffsetPosition;
            m_CameraTransform.localRotation = Quaternion.LookRotation(-m_CameraOffsetPosition);
#if UNITY_EDITOR
            if (m_GhostCameraTransform != null)
#endif
            {
                m_GhostCameraTransform.localPosition = m_CameraOffsetPosition;
            }
            if (m_Target)
            {
                m_PivotTransform.position = m_Target.position;
            }
            else
            {
                m_PivotTransform.position = m_TargetPosition;
            }
            m_Camera.orthographicSize = m_CurrentOrtographicSize;
        }

        //This is a helper function used to compute the map limits. It is used by the editor to auto detect world bounds
        public void ComputeMapLimits(LayerMask layerMask)
        {
            Bounds mapBounds = new Bounds();
            bool firstTime = true;
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                int transformedLayer = 1 << go.layer;
                int maskedLayer = transformedLayer & layerMask;
                if (go.activeInHierarchy && (maskedLayer != 0))
                {
                    Renderer meshRenderer = go.GetComponent<Renderer>();
                    if (meshRenderer)
                    {
                        Bounds bounds = meshRenderer.bounds;
                        if (!firstTime)
                        {
                            mapBounds.Encapsulate(bounds);
                        }
                        else
                        {
                            firstTime = false;
                            mapBounds = bounds;
                        }
                    }
                    else
                    {
                        Terrain terrain = go.GetComponent<Terrain>();
                        if (terrain)
                        {
                            Bounds bounds = terrain.terrainData.bounds;
                            bounds.center += terrain.transform.position;
                            if (!firstTime)
                            {
                                mapBounds.Encapsulate(bounds);
                            }
                            else
                            {
                                firstTime = false;
                                mapBounds = bounds;
                            }
                        }
                    }
                }
            }
            m_MapLimitLow.x = mapBounds.center.x - mapBounds.extents.x;
            m_MapLimitLow.y = mapBounds.center.z - mapBounds.extents.z;
            m_MapLimitHigh.x = mapBounds.center.x + mapBounds.extents.x;
            m_MapLimitHigh.y = mapBounds.center.z + mapBounds.extents.z;
        }
        #endregion //Public Helper Methods


        private void Start()
        {
            m_PivotTransform = transform;
            m_CameraTransform = m_Camera.transform;

            //Create the ghost camera
            m_GhostCamera = new GameObject();
            m_GhostCamera.name = "Ghost_" + m_Camera.name;
            m_GhostCameraTransform = m_GhostCamera.transform;
            m_GhostCameraTransform.parent = m_PivotTransform;

            SnapToDefault();
            m_StandardInput.Start();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                m_ComputeRadiusAndOffest = false;
                ComputeCameraOrientation();
                if (m_ProcessInput == null || m_ProcessInput())
                {
                    m_StandardInput.Update(this);
                }

                m_CurrentCollisionOffset = 0;
                if (m_UseCameraCollision)
                {
                    Vector3 targetPosition = m_TargetPosition;
                    if (m_Target)
                    {
                        targetPosition = m_Target.position;
                    }

                    switch (m_CollisionTypeCheck)
                    {
                        case CollisionType.Capsule: CollisionCheckCapsule(targetPosition); break;
                        case CollisionType.Box: CollisionCheckBox(targetPosition); break;
                    }

                    m_TargetRadiusOffsetAdj = Mathf.Min(m_RadiusOffset - 1.0f, m_CurrentCollisionOffset * m_PerUnitInHeightCloser);
                    float radius = Mathf.SmoothDamp(m_CurrentRadiusOffsetAdj, m_TargetRadiusOffsetAdj, ref m_VelocityRadiusAdj, m_SmoothAdjDistance);
                    if (radius != m_CurrentRadiusOffsetAdj)
                    {
                        m_CurrentRadiusOffsetAdj = radius;
                        m_ComputeRadiusAndOffest = true;
                    }
                }
                UpdateState();
            }
        }

        #region Serializable fields
        #region Setup
        [SerializeField] private Camera m_Camera = null;
        [SerializeField] private Transform m_Target = null;
        [SerializeField] private Vector3 m_TargetPosition = Vector3.zero;
        [SerializeField] private Vector2 m_MapLimitLow = new Vector2(-100.0f, -100.0f);
        [SerializeField] private Vector2 m_MapLimitHigh = new Vector2(100.0f, 100.0f);
        [SerializeField] private float m_SmoothMoveSpeed = 0.4f;
        [SerializeField] private float m_SmoothRotationSpeed = 0.3f;
        [SerializeField] private float m_SmoothZoomSpeed = 0.9f;
        [SerializeField] private float m_SmoothAdjDistance = 0.9f;
        #endregion
        #region Distance
        [SerializeField] private Vector2 m_OrbitRadius = new Vector2(10, 12);
        [SerializeField] private Vector2 m_OrbitHeight = new Vector2(12, 25);
        [SerializeField] private Vector2 m_OrthographicSize = new Vector2(10, 13);
        [SerializeField] private float m_DefaultOrientation = -90;
        [SerializeField] private float m_DefaultZoomLevel = 1.0f;
        #endregion
        #region Control
        [SerializeField] private CRO_CameraStandardInput m_StandardInput = new CRO_CameraStandardInput();
        public delegate bool ProcessInput();
        [SerializeField] private ProcessInput m_ProcessInput;
        #endregion
        #region Collision
        [SerializeField] private bool m_UseCameraCollision = true;
        [SerializeField] private LayerMask m_CollisionMasks = ~0;
        [SerializeField] private float m_CapsuleRadius = 0.5f;
        [SerializeField] private float m_CapsuleLength = 0.5f;
        [SerializeField] private float m_HeightCast = 100.0f;
        [SerializeField] private float m_PerUnitInHeightCloser = 0.5f;
        [SerializeField] private float m_BoxHeight = 1.0f;
        [SerializeField] private CollisionType m_CollisionTypeCheck = CollisionType.Capsule;
        #endregion
        #endregion

        #region Private Helpers
        private void ComputeCurrentRadiusAndHeight()
        {
            m_RadiusOffset = Mathf.Lerp(m_OrbitRadius.x, m_OrbitRadius.y, m_CurrentZoomLevel);
            m_HeightOffset = Mathf.Lerp(m_OrbitHeight.x, m_OrbitHeight.y, m_CurrentZoomLevel);
        }

        private void ComputeCameraOffsetPosition()
        {
            float angleRad = m_CurrentRotationAngle * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);
            m_CameraOffsetPosition.x = (m_RadiusOffset - m_CurrentRadiusOffsetAdj) * cosAngle;
            m_CameraOffsetPosition.z = (m_RadiusOffset - m_CurrentRadiusOffsetAdj) * sinAngle;
            m_CameraOffsetPosition.y = m_HeightOffset;

            m_GhostCameraOffsetPosition.x = m_RadiusOffset * cosAngle;
            m_GhostCameraOffsetPosition.z = m_RadiusOffset * sinAngle;
            m_GhostCameraOffsetPosition.y = m_HeightOffset;
        }

        private void ComputeCameraOrientation()
        {
            m_CameraForward = Vector3.Scale(m_CameraTransform.forward, m_NormalizeXZPlane).normalized;
            m_CameraOrientation = Quaternion.LookRotation(m_CameraForward);
        }

        private void UpdateState()
        {
            float newRotation = Mathf.SmoothDampAngle(m_CurrentRotationAngle, m_TargetRotationAngle, ref m_VelocityRotate, m_SmoothRotationSpeed, float.MaxValue, Time.unscaledDeltaTime);
            float newZoom = Mathf.SmoothDamp(m_CurrentZoomLevel, m_TargetZoomLevel, ref m_VelocityZoom, m_SmoothZoomSpeed, float.MaxValue, Time.unscaledDeltaTime);
            bool computeNewRotation = false;
            if (newZoom != m_TargetZoomLevel)
            {
                m_CurrentZoomLevel = newZoom;
                m_CurrentOrtographicSize = m_OrthographicSize.x + (m_OrthographicSize.y - m_OrthographicSize.x) * m_CurrentZoomLevel;
                m_Camera.orthographicSize = m_CurrentOrtographicSize;
                m_ComputeRadiusAndOffest = true;
            }

            if (m_ComputeRadiusAndOffest)
            {
                computeNewRotation = true;
                ComputeCurrentRadiusAndHeight();
            }

            if (computeNewRotation || newRotation != m_TargetRotationAngle)
            {
                m_CurrentRotationAngle = newRotation;
                ComputeCameraOffsetPosition();
            }
            m_CameraTransform.localPosition = m_CameraOffsetPosition;
            m_GhostCameraTransform.localPosition = m_GhostCameraOffsetPosition;
            m_CameraTransform.localRotation = Quaternion.LookRotation(-m_CameraOffsetPosition);

            Vector3 targetPosition = m_TargetPosition;
            if (IsFollowingTarget())
            {
                targetPosition = m_Target.position;
            }
            else
            {
                //If we don't follow a point do not move the target position outside the map limit
                m_TargetPosition.x = Mathf.Clamp(m_TargetPosition.x, m_MapLimitLow.x, m_MapLimitHigh.x);
                m_TargetPosition.z = Mathf.Clamp(m_TargetPosition.z, m_MapLimitLow.y, m_MapLimitHigh.y);
            }

            //Clamp the destination point to world limit
            targetPosition.x = Mathf.Clamp(targetPosition.x, m_MapLimitLow.x, m_MapLimitHigh.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, m_MapLimitLow.y, m_MapLimitHigh.y);

            Vector3 destinationPostion = targetPosition;
            destinationPostion.y += m_CurrentCollisionOffset;

            m_PivotTransform.position = Vector3.SmoothDamp(m_PivotTransform.position, destinationPostion, ref m_VelocityCameraPosition, m_SmoothMoveSpeed);
        }

        private void CollisionCheckCapsule(Vector3 targetPosition)
        {
            RaycastHit hitInfo;
            Vector3 cameraForward = Vector3.Scale(m_CameraTransform.forward, m_NormalizeXZPlane).normalized;
            Vector3 positionGhost = m_GhostCameraTransform.position + Vector3.up * m_HeightCast;
            m_CapsuleCenterGhost1 = positionGhost - cameraForward * (m_CapsuleLength);
            m_CapsuleCenterGhost2 = positionGhost + cameraForward * (m_RadiusOffset);
            float distToTarget = (targetPosition - positionGhost).magnitude;
            //Check the minimum camera height
            if (Physics.CapsuleCast(m_CapsuleCenterGhost1, m_CapsuleCenterGhost2, m_CapsuleRadius, Vector3.down, out hitInfo, distToTarget, m_CollisionMasks))
            {
                m_CurrentCollisionOffset = Mathf.Max(m_CurrentCollisionOffset, hitInfo.point.y - targetPosition.y);
            }
            m_CapsuleCenterGhost1.y = m_CurrentCollisionOffset + targetPosition.y;
            m_CapsuleCenterGhost2.y = m_CapsuleCenterGhost1.y;
        }

        private void CollisionCheckBox(Vector3 targetPosition)
        {
            RaycastHit hitInfo;
            bool moveCamera = !m_GhostCameraOffsetPosition.Equals(m_CameraOffsetPosition);

            if(moveCamera)
            {
                m_CameraTransform.localPosition = m_GhostCameraOffsetPosition;
                m_CameraTransform.localRotation = Quaternion.LookRotation(-m_GhostCameraOffsetPosition);
            }

            //Compute corners for the box
            //Use only half of the view to test for intersections
            int width = m_Camera.pixelWidth, height = m_Camera.pixelHeight;
            float adjustedHeight = height * 0.5f;
            float planePosition = m_Camera.nearClipPlane;
            if(!m_Camera.orthographic)
            {
                planePosition = m_GhostCameraOffsetPosition.magnitude;
                adjustedHeight = height;
            }
            Vector3 corner0 = m_Camera.ScreenToWorldPoint(new Vector3(0, 0, planePosition));
            Vector3 corner1 = m_Camera.ScreenToWorldPoint(new Vector3(width, 0, planePosition));
            Vector3 corner2 = m_Camera.ScreenToWorldPoint(new Vector3(width, adjustedHeight, planePosition));
            corner0.y = targetPosition.y + m_HeightCast;
            corner1.y = corner0.y;
            corner2.y = corner0.y;
            Vector3 position = (corner0 + corner2) * 0.5f;

            Vector3 targetPosOnCamPlane = targetPosition;
            Vector3 camOnTargetPlane = m_CameraTransform.position;
            targetPosOnCamPlane.y = position.y;
            camOnTargetPlane.y = position.y;
            Vector3 direction = (targetPosOnCamPlane - camOnTargetPlane).normalized;

            float extendX = Vector3.Distance(corner0, corner1);
            float extendZ = Vector3.Distance(corner1, corner2);
            float extendY = 1.0f;
            m_BoxHalfExtend = new Vector3(extendX, extendY, extendZ) * 0.5f;
            m_BoxCenter = position;
            m_BoxOrientation = Quaternion.FromToRotation(Vector3.forward, direction);
            float distToTarget = (targetPosition - position).magnitude;
            if(Physics.BoxCast(m_BoxCenter, m_BoxHalfExtend, Vector3.down, out hitInfo, m_BoxOrientation, distToTarget, m_CollisionMasks))
            {
                m_CurrentCollisionOffset = Mathf.Max(m_CurrentCollisionOffset, hitInfo.point.y - targetPosition.y);
            }
            m_BoxCenter.y = m_CurrentCollisionOffset + targetPosition.y;
        }
        #endregion

        #region Private variables
        private Transform m_CameraTransform; //Main Camera transform
        private Transform m_PivotTransform; //Pivot transform (used to follow a target)
        private Transform m_GhostCameraTransform; //Transform of the ghost camera
        private GameObject m_GhostCamera; //This is the real camera position without the collision avoidance. It is used to stabilize the camera.
        private Quaternion m_CameraOrientation; //This is useful to find out where the camera is looking. Needed to compute movement by joystick or keyboard
        private Quaternion m_BoxOrientation;
        private Vector3 m_CameraOffsetPosition; //What is the offset of the camera (depends on orbit radius and orbit height)
        private Vector3 m_GhostCameraOffsetPosition;
        private Vector3 m_VelocityCameraPosition; //How fast should move the camera (used by smooth damp)
        private Vector3 m_CameraForward; //Where we are looking
        private Vector3 m_NormalizeXZPlane = new Vector3(1, 0, 1);
        private Vector3 m_CapsuleCenterGhost1;
        private Vector3 m_CapsuleCenterGhost2;
        private Vector3 m_BoxCenter;
        private Vector3 m_BoxHalfExtend;
        private Ray m_RayScreenToWorld;
        private float m_VelocityZoom; //How fast will zoom will get to target (used by smooth damp)
        private float m_VelocityRotate; //How fast will rotate to target (used by smooth damp)
        private float m_VelocityRadiusAdj; //How fast we will move the camera closer to the target
        private float m_TargetZoomLevel; //Zoom level is from 0 to 1
        private float m_TargetRotationAngle; //Where we want to get with the camera
        private float m_RadiusOffset; //It is calculated based on the ZoomLevel and is a linear interpolation between m_OrbitRadiusMax and m_OrbitRadiusMin
        private float m_HeightOffset; //It is calculated based on ZoomLevel and is a linear interpolation between m_OrbitHeightMax and m_OrbitHeightMin
        private float m_TargetRadiusOffsetAdj; //In case we need to move the camera more up we will get closer to the target. In this way we will start to look more down so we will be able to see the target
        private float m_CurrentRotationAngle; //What is the current rotation of the camera
        private float m_CurrentRadiusOffsetAdj; //What is the current radius offset adjustment
        private float m_CurrentZoomLevel; //Current zoom level
        private float m_CurrentOrtographicSize;
        private float m_CurrentCollisionOffset;
        private bool m_ComputeRadiusAndOffest;

#if UNITY_EDITOR
        #region Debug
        public bool m_ShowWorldMapBounds = false;
        public bool m_ShowCapsuleHit = false;
        public bool m_ShowCameraRadius = false;
        public bool m_AutoUpdateDefaults = false;
        #endregion

        #region Camera Custom Interface
        public LayerMask m_EditorMapLimitCollisionMasks = ~0;
        public int m_EditorCurrentIndex = 0;
        public bool m_EditorShowDebug = false;
        public bool m_EditorFollowTarget = false;
        #endregion //Camera Custom Interface
#endif
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (m_ShowWorldMapBounds)
            {
                Gizmos.color = Color.white;
                Vector3 p0 = new Vector3(m_MapLimitLow.x, 0, m_MapLimitLow.y);
                Vector3 p1 = new Vector3(m_MapLimitLow.x, 0, m_MapLimitHigh.y);
                Vector3 p2 = new Vector3(m_MapLimitHigh.x, 0, m_MapLimitLow.y);
                Vector3 p3 = new Vector3(m_MapLimitHigh.x, 0, m_MapLimitHigh.y);
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p3);
                Gizmos.DrawLine(p3, p2);
                Gizmos.DrawLine(p2, p0);
            }
            if(m_ShowCapsuleHit)
            {
                if (m_CollisionTypeCheck == CollisionType.Capsule)
                {
                    Vector3 right = Vector3.Cross((m_CapsuleCenterGhost2 - m_CapsuleCenterGhost1).normalized, Vector3.up) * m_CapsuleRadius;
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(m_CapsuleCenterGhost1 + right, m_CapsuleCenterGhost2 + right);
                    Gizmos.DrawLine(m_CapsuleCenterGhost1 - right, m_CapsuleCenterGhost2 - right);
                    Gizmos.DrawWireSphere(m_CapsuleCenterGhost1, m_CapsuleRadius);
                    Gizmos.DrawWireSphere(m_CapsuleCenterGhost2, m_CapsuleRadius);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(m_CapsuleCenterGhost1, m_CapsuleCenterGhost2);

                }
                else if (m_CollisionTypeCheck == CollisionType.Box)
                {
                    int width = m_Camera.pixelWidth, height = m_Camera.pixelHeight;
                    CRO_DebugBoxCollider.DrawBox(m_BoxCenter, m_BoxHalfExtend, m_BoxOrientation, Color.red);

                    //Compute the corner with the camera
                    Vector3 corner0 = m_Camera.ScreenToWorldPoint(new Vector3(width * 0.25f, height * 0.25f, m_Camera.nearClipPlane));
                    Gizmos.DrawWireSphere(corner0, 0.5f);
                    Matrix4x4 view = m_Camera.worldToCameraMatrix;
                    Matrix4x4 projection = m_Camera.projectionMatrix;
                }
            }
            if(m_ShowCameraRadius)
            {
#if UNITY_EDITOR
                m_PivotTransform = transform;
#endif
                Vector3 position = IsFollowingTarget() ? m_Target.position : m_TargetPosition;
                Vector3 posMin = position;
                Vector3 posMax = position;
                posMin.y += m_OrbitHeight.x;
                posMax.y += m_OrbitHeight.y;
                DrawCircleAtPointWithRadius(posMin, m_OrbitRadius.x, 20);
                DrawCircleAtPointWithRadius(posMax, m_OrbitRadius.y, 20);
                Vector3 pivotPos = m_PivotTransform.position;
                Vector3 posOnMinRadius = GetOffset(m_CurrentRotationAngle, m_OrbitRadius.x, m_OrbitHeight.x) + position;
                Vector3 posOnMaxRadius = GetOffset(m_CurrentRotationAngle, m_OrbitRadius.y, m_OrbitHeight.y) + position;
                Gizmos.DrawLine(posOnMinRadius, posOnMaxRadius);
            }
        }

        private static void DrawCircleAtPointWithRadius(Vector3 targetPosition, float radius, int numPoints)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(targetPosition, Quaternion.identity, radius * Vector3.one);

            Vector3 currentPoint = Vector3.forward;
            Quaternion rotation = Quaternion.AngleAxis(360.0f / (float)numPoints, Vector3.up);
            for (int i = 0; i < numPoints + 1; ++i)
            {
                Vector3 nextPoint = rotation * currentPoint;
                Gizmos.DrawLine(currentPoint, nextPoint);
                currentPoint = nextPoint;
            }
            Gizmos.matrix = previousMatrix;
        }

        public void Editor_DrawControls()
        {
            m_StandardInput.Editor_DrawControls();
        }

        public void Editor_AddOrUpdateInputData(bool overwriteExistingAxes)
        {
            m_StandardInput.Editor_AddOrUpdateInputData(overwriteExistingAxes);
        }
#endif
    }
}