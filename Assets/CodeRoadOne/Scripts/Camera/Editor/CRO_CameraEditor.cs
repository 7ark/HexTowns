using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CodeRoadOne
{
    [CustomEditor(typeof(CRO_Camera))]
    public class CRO_CameraEditor : Editor
    {
        private CRO_Camera m_CRO_Camera;

        #region GUI Content
        private static GUIContent m_CameraField = new GUIContent("Camera:", "The camera that is going to be moved, it must be the child of this object.");
        private static GUIContent m_FollowTargetField = new GUIContent("Follow target", "Specify if the camera will follow an object or look at a position.");
        private static GUIContent m_FollowField = new GUIContent("Follow:", "Who to follow. This must be the transform to that object.");
        private static GUIContent m_LookAtField = new GUIContent("Look at:", "Where to look.");
        private static GUIContent m_SmoothingRotationField = new GUIContent("Rotation:", "How fast we will get to the desired view angle.");
        private static GUIContent m_SmoothingMoveField = new GUIContent("Move:", "How fast we will get to the desired position.");
        private static GUIContent m_SmoothingZoomField = new GUIContent("Zoom:", "How fast we will get to the desired zoom level.");
        private static GUIContent m_SmoothingViewAngleField = new GUIContent("View angle:", "How fast we will get to the desired orientation in case the camera is moving up because of a collision with the environment.");
        private static GUIContent m_CameraLimitField = new GUIContent("Camera limit", "In this section you can specify in what area the camera is allowed to move, the collision bounds. You can manually set it up or let it autodetect the world bounds.");
        private static GUIContent m_CameraLimitMinField = new GUIContent("Minimum", "Minimum coordinates where the camera can reach");
        private static GUIContent m_CameraLimitMinXField = new GUIContent("X: ", "The minimum coordinate on X");
        private static GUIContent m_CameraLimitMinZField = new GUIContent("Z: ", "The minimum coordinate on Z");
        private static GUIContent m_CameraLimitMaxField = new GUIContent("Maximum", "Maximum coordinates where the camera can reach");
        private static GUIContent m_CameraLimitMaxXField = new GUIContent("X: ", "The maximum coordinate on X");
        private static GUIContent m_CameraLimitMaxZField = new GUIContent("Z: ", "The maximum coordinate on Z");
        private static GUIContent m_AutoDetectLayerField = new GUIContent("Layer mask:", "Specify witch layers to use when computing the map limit.");
        private static GUIContent m_AutoDetectLimitButton = new GUIContent("Auto detect limits", "Clicking here will set the minimum and maximum size of the map based on the objects that are part of the selected layer mask.");
        private static GUIContent m_OrbitRadiusMinField = new GUIContent("Min:", "The distance in the local x-z plane to the target at minimum zoom");
        private static GUIContent m_OrbitRadiusMaxField = new GUIContent("Max:", "The distance in the local x-z plane to the target at maximum zoom");
        private static GUIContent m_OrbitHeightMinField = new GUIContent("Min:", "The height we want the camera to be above the target at minimum zoom");
        private static GUIContent m_OrbitHeightMaxField = new GUIContent("Max:", "The height we want the camera to be above the target at maximum zoom");
        private static GUIContent m_OrthographicSizeMinField = new GUIContent("Min:", "Orthographic camera size at minimum zoom. This is valid only for orthographic cameras.");
        private static GUIContent m_OrthographicSizeMaxField = new GUIContent("Max:", "Orthographic camera size at maximum zoom. This is valid only for orthographic cameras.");
        private static GUIContent m_DefaultZoomLevelField = new GUIContent("Zoom level:", "The startup zoom level for the camera.");
        private static GUIContent m_DefaultOrientationField = new GUIContent("Orientation:", "From what angle on the radius we will look at the target.");
        private static GUIContent m_CameraCollisionField = new GUIContent("Use camera collision:", "Specify if the camera should detect objects and move above them. The objects in question must have an collider.");
        private static GUIContent m_CameraCollisionLayerMaskField = new GUIContent("Collision mask:", "Specify witch layers to use when testing for collision. We recommend to put all static collisions on a separate layer.");
        private static GUIContent m_CollisionTypeField = new GUIContent("Collision type:", "What type of collision we want to use for physics cast.");
        private static GUIContent m_CapsuleRadiusField = new GUIContent("Radius:", "The capsule radius when we check to see if we need to move the camera.");
        private static GUIContent m_CapsuleRadiusLengthField = new GUIContent("Length:", "The capsule length behind the camera. This is used that you don't go inside objects when you change directions fast.");
        private static GUIContent m_BoxHeightField = new GUIContent("Height:", "The box height that we use to check for collisions. This should be small in general.");
        private static GUIContent m_HeightCastField = new GUIContent("Height cast:", "From how far above the camera we start the capsule check.");
        private static GUIContent m_PerUnitInHeightCloserField = new GUIContent("Optimise angle:", "With how many units we get closer to the target if the camera is moved up with one unit.");
        private static GUIContent m_ApplyButton = new GUIContent("Apply", "Clicking here we will take the information from the target, distances and defaults and adjust the camera to look at that position. It could be a target or a position in the world.");
        private static GUIContent m_ShowWorldBoudsField = new GUIContent("Show world bounds: ", "Show world map bounds.");
        private static GUIContent m_ShowCollisionCheckField = new GUIContent("Show collision check: ", "Show where the current collision is intersecting the world.");
        private static GUIContent m_ShowCameraRadiusField = new GUIContent("Show camera distances: ", "Show the positions (min and max) for the camera radius and height.");
        private static GUIContent m_AutoUpdateCameraField = new GUIContent("Auto update camera position: ", "Autoupdate the position of the camera when the defaults parameters (zoom and orientation) are changing.");
        private Vector3 m_OldPosition;
        private Vector2 m_OldRadius;
        private Vector2 m_OldHeight;
        private Vector2 m_OldOrthographicSize;
        private Vector2 m_OldDefaults;
        #endregion

        CRO_CameraEditor()
        {
        }

        private void Awake()
        {
            m_CRO_Camera = target as CRO_Camera;
            UpdateOldValues();
        }

        private void OnEnable()
        {
            m_CRO_Camera.GetStandardInput().OnEnable(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(m_CRO_Camera, "CRO_Camera");
            DrawTabs();
            if (!Application.isPlaying && m_CRO_Camera.m_AutoUpdateDefaults && AreOldDataDifferent())
            {
                m_CRO_Camera.SnapToDefault();
                UpdateOldValues();
                EditorUtility.SetDirty(m_CRO_Camera.GetCamera());
            }
        }

        private void UpdateOldValues()
        {
            m_OldPosition = m_CRO_Camera.IsFollowingTarget() ? m_CRO_Camera.GetTarget().position : m_CRO_Camera.GetTargetedPosition();
            m_OldRadius = m_CRO_Camera.GetOrbitRadius();
            m_OldHeight = m_CRO_Camera.GetOrbitHeight();
            m_OldOrthographicSize = m_CRO_Camera.GetOrthographicSize();
            m_OldDefaults.x = m_CRO_Camera.GetDefaultZoomLevel();
            m_OldDefaults.y = m_CRO_Camera.GetDefaultOrientation();
        }

        private bool AreOldDataDifferent()
        {
            return (
                !m_OldPosition.Equals(m_CRO_Camera.IsFollowingTarget() ? m_CRO_Camera.GetTarget().position : m_CRO_Camera.GetTargetedPosition()) ||
                !m_OldRadius.Equals(m_CRO_Camera.GetOrbitRadius()) ||
                !m_OldHeight.Equals(m_CRO_Camera.GetOrbitHeight()) ||
                !m_OldOrthographicSize.Equals(m_CRO_Camera.GetOrthographicSize()) ||
                !m_OldDefaults.x.Equals(m_CRO_Camera.GetDefaultZoomLevel()) ||
                !m_OldDefaults.y.Equals(m_CRO_Camera.GetDefaultOrientation()));
        }

        private void DrawSetup()
        {
            GUILayout.Label("Setup", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                m_CRO_Camera.SetCamera(EditorGUILayout.ObjectField(m_CameraField, m_CRO_Camera.GetCamera(), typeof(Camera), true) as Camera);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(m_FollowTargetField, EditorStyles.boldLabel, GUILayout.Width(100));
                    m_CRO_Camera.m_EditorFollowTarget = EditorGUILayout.Toggle(m_CRO_Camera.m_EditorFollowTarget);
                }
                EditorGUILayout.EndHorizontal();
                if (Application.isPlaying)
                {
                    m_CRO_Camera.m_EditorFollowTarget = m_CRO_Camera.GetTarget() != null;
                }
                if (m_CRO_Camera.m_EditorFollowTarget)
                {
                    m_CRO_Camera.SetTarget(EditorGUILayout.ObjectField(m_FollowField, m_CRO_Camera.GetTarget(), typeof(Transform), true) as Transform);
                }
                else
                {
                    m_CRO_Camera.SetTarget(null);
                    m_CRO_Camera.SetTargetedPosition(EditorGUILayout.Vector3Field(m_LookAtField, m_CRO_Camera.GetTargetedPosition()));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Smoothing", EditorStyles.miniBoldLabel);
                m_CRO_Camera.SetSmoothRotationSpeed(EditorGUILayout.Slider(m_SmoothingRotationField, m_CRO_Camera.GetSmoothRotationSpeed(), 0.01f, 1.0f));
                m_CRO_Camera.SetSmoothMoveSpeed(EditorGUILayout.Slider(m_SmoothingMoveField, m_CRO_Camera.GetSmoothMoveSpeed(), 0.01f, 1.0f));
                m_CRO_Camera.SetSmoothZoomSpeed(EditorGUILayout.Slider(m_SmoothingZoomField, m_CRO_Camera.GetSmoothZoomSpeed(), 0.01f, 1.0f));
                m_CRO_Camera.SetSmoothAdjDistance(EditorGUILayout.Slider(m_SmoothingViewAngleField, m_CRO_Camera.GetSmoothAdjDistance(), 0.01f, 1.0f));
            }
            EditorGUILayout.EndVertical();

            #region Camera Limit
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(m_CameraLimitField, EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField(m_CameraLimitMinField, EditorStyles.miniBoldLabel);
                    Vector2 mapLimitLow = m_CRO_Camera.GetMapLimitLow();
                    mapLimitLow.x = EditorGUILayout.FloatField(m_CameraLimitMinXField, mapLimitLow.x);
                    mapLimitLow.y = EditorGUILayout.FloatField(m_CameraLimitMinZField, mapLimitLow.y);
                    m_CRO_Camera.SetMapLimitLow(mapLimitLow);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField(m_CameraLimitMaxField, EditorStyles.miniBoldLabel);
                    Vector2 mapLimitHigh = m_CRO_Camera.GetMapLimitHigh();
                    mapLimitHigh.x = EditorGUILayout.FloatField(m_CameraLimitMaxXField, mapLimitHigh.x);
                    mapLimitHigh.y = EditorGUILayout.FloatField(m_CameraLimitMaxZField, mapLimitHigh.y);
                    m_CRO_Camera.SetMapLimitHigh(mapLimitHigh);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Auto detect", EditorStyles.miniBoldLabel);
                    m_CRO_Camera.m_EditorMapLimitCollisionMasks = LayerMaskField(m_AutoDetectLayerField, m_CRO_Camera.m_EditorMapLimitCollisionMasks);

                    if (GUILayout.Button(m_AutoDetectLimitButton))
                    {
                        m_CRO_Camera.ComputeMapLimits(m_CRO_Camera.m_EditorMapLimitCollisionMasks);
                        EditorUtility.SetDirty(m_CRO_Camera.GetCamera());
                    }
                }
                EditorGUILayout.EndVertical();

            }
            EditorGUILayout.EndVertical();
            #endregion //Camera Limit
        }

        private void DrawDistance()
        {
            EditorGUILayout.LabelField("Distance", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Orbit radius", EditorStyles.miniBoldLabel);
                Vector2 radius = m_CRO_Camera.GetOrbitRadius();
                radius.x = EditorGUILayout.Slider(m_OrbitRadiusMinField, radius.x, 0.01f, 1000.0f);
                radius.y = EditorGUILayout.Slider(m_OrbitRadiusMaxField, radius.y, 0.01f, 1000.0f);
                m_CRO_Camera.SetOrbitRadius(radius);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Orbit Height", EditorStyles.miniBoldLabel);
                Vector2 height = m_CRO_Camera.GetOrbitHeight();
                height.x = EditorGUILayout.Slider(m_OrbitHeightMinField, height.x, 0.01f, 1000.0f);
                height.y = EditorGUILayout.Slider(m_OrbitHeightMaxField, height.y, 0.01f, 1000.0f);
                m_CRO_Camera.SetOrbitHeight(height);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Orthographic size", EditorStyles.miniBoldLabel);
                Vector2 size = m_CRO_Camera.GetOrthographicSize();
                size.x = EditorGUILayout.Slider(m_OrthographicSizeMinField, size.x, 0.01f, 50.0f);
                size.y = EditorGUILayout.Slider(m_OrthographicSizeMaxField, size.y, 0.01f, 50.0f);
                m_CRO_Camera.SetOrthographicSize(size);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Defaults", EditorStyles.miniBoldLabel);
                m_CRO_Camera.SetDefaultZoomLevel(EditorGUILayout.Slider(m_DefaultZoomLevelField, m_CRO_Camera.GetDefaultZoomLevel(), 0.0f, 1.0f));
                m_CRO_Camera.SetDefaultOrientation(EditorGUILayout.Slider(m_DefaultOrientationField, m_CRO_Camera.GetDefaultOrientation(), -180.0f, 180.0f));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("The above fields control how close the camera will be to the target.\nYou have a min and max value. These values define the shortest and longest distance from camera to target.\nYou can think of it as zoom level: close and far.", MessageType.None);
        }

        private void DrawCollision()
        {
            GUILayout.Label("Collision", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(m_CameraCollisionField, EditorStyles.boldLabel, GUILayout.Width(150));
                    m_CRO_Camera.SetUseCameraCollision(EditorGUILayout.Toggle(m_CRO_Camera.GetUseCameraCollision()));
                }
                EditorGUILayout.EndHorizontal();
                if (m_CRO_Camera.GetUseCameraCollision())
                {
                    m_CRO_Camera.SetCollisionMask(LayerMaskField(m_CameraCollisionLayerMaskField, m_CRO_Camera.GetCollisionMask()));

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        m_CRO_Camera.SetCollisionTypeCheck((CRO_Camera.CollisionType)EditorGUILayout.EnumPopup(m_CollisionTypeField, m_CRO_Camera.GetCollisionTypeCheck()));
                        if (m_CRO_Camera.GetCollisionTypeCheck() == CRO_Camera.CollisionType.Capsule)
                        {
                            m_CRO_Camera.SetCapsuleRadius(EditorGUILayout.FloatField(m_CapsuleRadiusField, m_CRO_Camera.GetCapsuleRadius()));
                            m_CRO_Camera.SetCapsuleLength(EditorGUILayout.FloatField(m_CapsuleRadiusLengthField, m_CRO_Camera.GetCapsuleLength()));
                            if (m_CRO_Camera.GetCamera().orthographic)
                            {
                                EditorGUILayout.HelpBox("You are using capsule collision for an orthographic camera. This will not work as expected (not to intersect any object). You should switch to box collision.", MessageType.Warning);
                            }
                        }
                        else if(m_CRO_Camera.GetCollisionTypeCheck() == CRO_Camera.CollisionType.Box)
                        {
                            m_CRO_Camera.SetBoxHeight(EditorGUILayout.Slider(m_BoxHeightField, m_CRO_Camera.GetBoxHeight(), 0.01f, 5.0f));
                            if (!m_CRO_Camera.GetCamera().orthographic)
                            {
                                EditorGUILayout.HelpBox("The box collision is designed for orthographic camera. We will try to automatically adjust parameters to make sense, but in case the camera doesn't detect that it can not see the target we recommend to switch to capsule test.", MessageType.Warning);
                            }
                        }
                        else
                        {
                            Debug.Log("Add here the new collision type data");
                        }
                    }
                    EditorGUILayout.EndVertical();

                    m_CRO_Camera.SetHeightCast(EditorGUILayout.FloatField(m_HeightCastField, m_CRO_Camera.GetHeightCast()));

                    m_CRO_Camera.SetPerUnitInHeightCloser(EditorGUILayout.Slider(m_PerUnitInHeightCloserField, m_CRO_Camera.GetPerUnitInHeightCloser(), 0.01f, 50.0f));
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.HelpBox("If collision is enabled we always assume that the camera is above all objects. If you need something to follow inside a tunnel this is not going to work with Capsule or Box collision type. If you need something else do not hesitate to write to us for suggestions.", MessageType.None);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (m_CRO_Camera.m_EditorCurrentIndex == 0) GUI.color = Color.gray;
                    else GUI.color = Color.white;
                    if (GUILayout.Button("Setup", EditorStyles.miniButtonLeft))
                    {
                        m_CRO_Camera.m_EditorCurrentIndex = 0;
                    }
                    if (m_CRO_Camera.m_EditorCurrentIndex == 1) GUI.color = Color.gray;
                    else GUI.color = Color.white;
                    if (GUILayout.Button("Distance", EditorStyles.miniButtonMid))
                    {
                        m_CRO_Camera.m_EditorCurrentIndex = 1;
                    }
                    if (m_CRO_Camera.m_EditorCurrentIndex == 2) GUI.color = Color.gray;
                    else GUI.color = Color.white;
                    if (GUILayout.Button("Control", EditorStyles.miniButtonMid))
                    {
                        m_CRO_Camera.m_EditorCurrentIndex = 2;
                    }
                    if (m_CRO_Camera.m_EditorCurrentIndex == 3) GUI.color = Color.gray;
                    else GUI.color = Color.white;
                    if (GUILayout.Button("Collision", EditorStyles.miniButtonRight))
                    {
                        m_CRO_Camera.m_EditorCurrentIndex = 3;
                    }
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();
                //We don't save any tab change as counting to see if some properties of the camera has changed.
                GUI.changed = false;
                switch (m_CRO_Camera.m_EditorCurrentIndex)
                {
                    case 0: DrawSetup(); break;
                    case 1: DrawDistance(); break;
                    case 2: m_CRO_Camera.Editor_DrawControls(); break;
                    case 3: DrawCollision(); break;
                }
            }
            EditorGUILayout.EndVertical();
            //If something has changed we mark the CRO_Camera as dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(m_CRO_Camera);
            }
            if (GUILayout.Button(m_ApplyButton))
            {
                m_CRO_Camera.SnapToDefault();
                EditorUtility.SetDirty(m_CRO_Camera.GetCamera());
            }

            ShowDebugMenu();
        }

        void ShowDebugMenu()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.indentLevel++;
                m_CRO_Camera.m_EditorShowDebug = EditorGUILayout.Foldout(m_CRO_Camera.m_EditorShowDebug, "Debug", true);
                EditorGUI.indentLevel--;
                if (m_CRO_Camera.m_EditorShowDebug)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(m_ShowWorldBoudsField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                    m_CRO_Camera.m_ShowWorldMapBounds = EditorGUILayout.Toggle(m_CRO_Camera.m_ShowWorldMapBounds);
                    EditorGUILayout.EndHorizontal();

                    if (m_CRO_Camera.GetUseCameraCollision())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(m_ShowCollisionCheckField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                        m_CRO_Camera.m_ShowCapsuleHit = EditorGUILayout.Toggle(m_CRO_Camera.m_ShowCapsuleHit);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(m_ShowCameraRadiusField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                    m_CRO_Camera.m_ShowCameraRadius = EditorGUILayout.Toggle(m_CRO_Camera.m_ShowCameraRadius);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(m_AutoUpdateCameraField, EditorStyles.boldLabel, GUILayout.Width(200.0f));
                    bool oldAutoUpdate = m_CRO_Camera.m_AutoUpdateDefaults;
                    m_CRO_Camera.m_AutoUpdateDefaults = EditorGUILayout.Toggle(m_CRO_Camera.m_AutoUpdateDefaults);
                    if(m_CRO_Camera.m_AutoUpdateDefaults && !oldAutoUpdate)
                    {
                        //When we enable autoupdate, force snap and update old values
                        m_CRO_Camera.SnapToDefault();
                        UpdateOldValues();
                        EditorUtility.SetDirty(m_CRO_Camera.GetCamera());
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }

        #region Helpers
        static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }

            int maxMask = (1 << layerNumbers.Count) - 1;
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            if (maskWithoutEmpty == maxMask)
            {
                maskWithoutEmpty = ~0;
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }
        #endregion
    }
}