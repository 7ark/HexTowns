using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeRoadOne;

namespace CodeRoadOne
{
    public class CRO_GameController : MonoBehaviour
    {
        public Transform[] m_Characters;
        public CRO_Camera m_CRO_Camera;
        public Text m_ButtonText;
        private bool m_FollowingCharacter;

        public void FollowTarget(Transform target)
        {
            m_CRO_Camera.SetTarget(target);
        }

        public void FollowRandomCharacter()
        {
            int character = Random.Range(0, m_Characters.Length);
            FollowTarget(m_Characters[character]);
            m_FollowingCharacter = true;
            m_ButtonText.text = "Free Cam";
        }

        public void FreeCamera()
        {
            m_CRO_Camera.SetFreeCam(true);
            m_FollowingCharacter = false;
            m_ButtonText.text = "Follow Character";
        }

        // Start is called before the first frame update
        void Start()
        {
            FollowRandomCharacter();
            //The next line will snap the camera to the targeted object
            m_CRO_Camera.SnapToDefault();
            //m_CRO_Camera.SetProcessInput(ProcessInput);
        }

        public void ToggleFollow()
        {
            if(m_FollowingCharacter)
            {
                FreeCamera();
            }
            else
            {
                FollowRandomCharacter();
            }
        }

        public bool ProcessInput()
        {
            return true;
        }

        public void OnClickInWorld()
        {
            RaycastHit info;
            if(m_CRO_Camera.GetObjectUnderMouse(out info))
            {
                Debug.Log("Clicked on: " + (info.collider.transform.parent ? info.collider.transform.parent.name : info.collider.name));
            }


        }
    }
}
