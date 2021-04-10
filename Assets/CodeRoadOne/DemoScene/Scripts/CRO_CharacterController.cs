using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRoadOne
{
    public class CRO_CharacterController : MonoBehaviour
    {
        NavMeshAgent m_Agent;
        public Transform[] m_WayPoints;

        private void Start()
        {
            m_Agent = GetComponentInChildren<NavMeshAgent>();

            m_Agent.updateRotation = true;
            m_Agent.updatePosition = true;
            ChooseRandomDestination();
        }

        void Update()
        {
            if (m_Agent.remainingDistance <= m_Agent.stoppingDistance)
                ChooseRandomDestination();
        }

        void ChooseRandomDestination()
        {
            int destination = Random.Range(0, m_WayPoints.Length);
            m_Agent.SetDestination(m_WayPoints[destination].position);
        }
    }
}