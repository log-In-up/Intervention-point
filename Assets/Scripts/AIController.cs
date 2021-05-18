using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Physics;

namespace InterventionPoint
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    sealed class AIController : MonoBehaviour
    {
        #region Parameters
        [SerializeField] Transform[] patrollingPoints = null;
        [SerializeField] float idleTime = 5.0f, remainingDistanceToPoint = 0.5f, viewRadius = 30.0f;
        [SerializeField, Range(0.0f, 360.0f)] private float viewAngle = 60.0f;
        [SerializeField] LayerMask whatIsPlayer, whatIsObstacle;

        private const int startingIndex = 0, indexIncrementor = 1, zero = 0, two = 2;

        private enum AIState { Chaising, Dead, Idle, Patrolling }
        AIState currentState = AIState.Idle;
        private int currentPatrolIndex;

        private Coroutine idle = null;
        private NavMeshAgent navAgent = null;
        #endregion

        #region MonoBehaviour API    
        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            currentPatrolIndex = startingIndex;

            SwitchState(AIState.Idle);
        }

        private void Update()
        {
            Debug.Log(PlayerInFieldOfView(viewRadius, viewAngle, whatIsPlayer, whatIsObstacle));
            UpdateCurrentState();
        }
        #endregion

        #region Finite State Machine
        #region Chaising
        private void EnterChaisingState()
        {

        }

        private void UpdateChaisingState()
        {

        }

        private void ExitChaisingState()
        {

        }
        #endregion

        #region Dead
        private void EnterDeadState()
        {

        }

        private void UpdateDeadState()
        {

        }

        private void ExitDeadState()
        {

        }
        #endregion

        #region Idle
        private void EnterIdleState()
        {
            idle = StartCoroutine(SwitchStateByTime(idleTime, AIState.Patrolling));
        }

        private void UpdateIdleState()
        {
            
        }

        private void ExitIdleState()
        {
            if (idle != null)
            {
                StopCoroutine(idle);
            }
        }
        #endregion

        #region Patrolling
        private void EnterPatrollingState()
        {

        }

        private void UpdatePatrollingState()
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= remainingDistanceToPoint)
            {
                MoveToNextPoint();
            }
        }

        private void ExitPatrollingState()
        {

        }
        #endregion
        #endregion

        #region Custom methods
        private void MoveToNextPoint()
        {
            navAgent.destination = patrollingPoints[currentPatrolIndex].position;
            currentPatrolIndex = (currentPatrolIndex + indexIncrementor) % patrollingPoints.Length;
        }

        private void UpdateCurrentState()
        {
            switch (currentState)
            {
                case AIState.Chaising:
                    UpdateChaisingState();
                    break;
                case AIState.Dead:
                    UpdateDeadState();
                    break;
                case AIState.Idle:
                    UpdateIdleState();
                    break;
                case AIState.Patrolling:
                    UpdatePatrollingState();
                    break;
                default:
                    break;
            }
        }

        private void SwitchState(AIState state)
        {
            switch (currentState)
            {
                case AIState.Chaising:
                    ExitChaisingState();
                    break;
                case AIState.Dead:
                    ExitDeadState();
                    break;
                case AIState.Idle:
                    ExitIdleState();
                    break;
                case AIState.Patrolling:
                    ExitPatrollingState();
                    break;
            }

            switch (state)
            {
                case AIState.Chaising:
                    EnterChaisingState();
                    break;
                case AIState.Dead:
                    EnterDeadState();
                    break;
                case AIState.Idle:
                    EnterIdleState();
                    break;
                case AIState.Patrolling:
                    EnterPatrollingState();
                    break;
            }
            currentState = state;
        }

        private IEnumerator SwitchStateByTime(float time, AIState state)
        {
            yield return new WaitForSeconds(time);

            SwitchState(state);
        }


        private bool PlayerInFieldOfView(float radius, float angle, LayerMask target, LayerMask obstacle)
        {
            Collider[] colliders = OverlapSphere(transform.position, radius, target);

            for (int i = zero; i < colliders.Length; i++)
            {
                Vector3 directionToTarget = (colliders[i].transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, directionToTarget) < angle / two)
                {
                    float distance = Vector3.Distance(transform.position, colliders[i].transform.position);

                    return !Raycast(transform.position, directionToTarget, distance, obstacle);
                }
            }

            return false;
        }

        #endregion
    }
}
