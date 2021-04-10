using UnityEngine;
using UnityEditor;

namespace CodeRoadOne
{
    public class CRO_CameraWizard : ScriptableWizard
    {
        public Camera m_Camera;
        public string m_CameraName = "MainCamera";
        public string m_PivotName = "CRO_Camera";
        public bool m_AddAxisToInputManager = true;
        public bool m_OverwriteAxesIfExists = false;

        private CRO_Camera m_CRO_Camera = null;
        private Camera m_OldCamera = null;
        private bool m_FindCamera = true;

        [MenuItem("GameObject/CodeRoadOne/CRO-Camera")]
        static void CreateWindow()
        {
            DisplayWizard<CRO_CameraWizard>("Create/Update CRO-Camera", "Create/Update");
        }

        private void OnWizardUpdate()
        {
            if(m_FindCamera)
            {
                m_Camera = FindFirstCameraInTheWorld();
                m_FindCamera = false;
            }
            helpString = "Create camera system";
            bool missingCameraName = m_CameraName == "";
            bool missingPivotName = m_PivotName == "";
            bool missingCamera = m_Camera == null;

            isValid = true;
            errorString = "";

            if (missingCameraName)
            {
                errorString += "Must set a name for the camera.\n";
                isValid = false;
            }
            if (missingPivotName)
            {
                errorString += "Must set a name for the pivot.\n";
                isValid = false;
            }
            if (missingCamera)
            {
                errorString += "Please select a camera, or a new one will be created";
            }
            else if (m_OldCamera != m_Camera)
            {
                m_OldCamera = m_Camera;
                m_CameraName = m_Camera.name;
                //Try to get the pivot name of the camera if exists
                if (m_Camera.transform.parent)
                {
                    m_PivotName = m_Camera.transform.parent.name;
                }
            }
        }

        private void OnWizardCreate()
        {
            if (!m_Camera)
            {
                GameObject go = new GameObject(m_CameraName);
                m_Camera = go.AddComponent<Camera>();
                Debug.Log("New camera was created with the name:" + m_CameraName);
            }
            else
            {
                Debug.Log("We will setup the camera rig using camera: " + m_CameraName);
            }
            if (m_Camera)
            {
                //Make sure the camera has the desired name
                m_Camera.name = m_CameraName;
                //Check to see if the camera has a pivot
                Transform parentTransform = m_Camera.transform.parent;
                GameObject parentGO = null;
                bool pivotFound = false;
                if (parentTransform)
                {
                    //Get the parent game object
                    parentGO = parentTransform.gameObject;
                    pivotFound = true;
                    Debug.Log("We found a pivot for this camera: " + m_CameraName + " with this name: " + parentTransform.name);
                }
                else
                {
                    //We don't have a pivot so we will create one
                    parentGO = new GameObject();
                    parentTransform = parentGO.transform;
                    parentTransform.position = Vector3.zero;
                    parentTransform.rotation = Quaternion.identity;
                    //Make the camera the child of this game object
                    m_Camera.transform.parent = parentTransform;
                    Debug.Log("We will create a new pivot for this camera: " + m_CameraName);
                }
                //Make sure it has the desired name
                parentGO.name = m_PivotName;
                //Make sure it has no other parents so is not moved by another system by mistake
                parentGO.transform.parent = null;

                //Try to see if the parent has the needed component
                m_CRO_Camera = parentGO.GetComponent<CRO_Camera>();
                if (!m_CRO_Camera)
                {
                    //If not add the component
                    m_CRO_Camera = parentGO.AddComponent<CRO_Camera>();
                    if(pivotFound)
                    {
                        Debug.LogWarning("We already found a pivot for this camera: " + m_CameraName + ", but this pivot has no CRO_Camera script attached. We will automatically add this, but be aware that this pivot will be moved by the camera setup so if this is not intentional, please move the camera to the root and recreate the rig. If you do this make sure that you remove the CRO_Camera from this object: " + m_PivotName);
                    }
                    else
                    {
                        Debug.Log("Added the CRO_Camera to the pivot: " + m_PivotName);
                    }
                }

                //Connect the components together
                m_CRO_Camera.SetCamera(m_Camera);
                m_CRO_Camera.SnapToDefault();
                if (m_AddAxisToInputManager)
                {
                    Debug.Log("Adding the axis to the input manager.");
                    m_CRO_Camera.Editor_AddOrUpdateInputData(m_OverwriteAxesIfExists);
                }
            }
            else
            {
                Debug.Log("Something was wrong and we didn't create or find the specified camera. The script will abort. Please check the camera parameter.");
                return;
            }
        }

        private Camera FindFirstCameraInTheWorld()
        {
            Camera[] allObjects = UnityEngine.Object.FindObjectsOfType<Camera>();
            if(allObjects.Length > 0)
            {
                return allObjects[0];
            }
            return null;
        }
    }
}