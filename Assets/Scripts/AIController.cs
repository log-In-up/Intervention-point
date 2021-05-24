using System.Collections;
using UnityEngine.AI;
using UnityEngine;
using static UnityEngine.Physics;
using static UnityEngine.Vector3;

namespace InterventionPoint
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    sealed class AIController : MonoBehaviour
    {
        #region Parameters
        [SerializeField] Transform[] patrollingPoints = null;
        [SerializeField] float idleTime = 5.0f, remainingDistanceToPoint = 0.5f, viewRadius = 30.0f;
        [SerializeField, Range(0.0f, 180.0f)] private float horizontalViewAngle = 60.0f, verticalViewAngle = 50.0f;
        [SerializeField] LayerMask whatIsPlayer, whatIsObstacle;

        private const int startingIndex = 0, zero = 0, two = 2;

        private enum AIState { Chaising, Dead, Idle, Patrolling }
        AIState currentState = AIState.Idle;
        private int currentPatrolIndex;

        private Coroutine idle = null;
        private Vector3 lastPlayerPosition;
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
            Debug.Log(currentState);
            UpdateCurrentState();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, transform.forward * viewRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 angleAxis = Quaternion.AngleAxis(horizontalViewAngle / two, transform.up) * transform.forward * viewRadius;
            Vector3 angleAxis1 = Quaternion.AngleAxis(-horizontalViewAngle / two, transform.up) * transform.forward * viewRadius;
            Vector3 angleAxis2 = Quaternion.AngleAxis(verticalViewAngle / two, transform.right) * transform.forward * viewRadius;
            Vector3 angleAxis3 = Quaternion.AngleAxis(-verticalViewAngle / two, transform.right) * transform.forward * viewRadius;

            Gizmos.DrawRay(transform.position, angleAxis);
            Gizmos.DrawRay(transform.position, angleAxis1);
            Gizmos.DrawRay(transform.position, angleAxis3);
            Gizmos.DrawRay(transform.position, angleAxis2);
        }
        #endregion

        #region Finite State Machine
        #region Chaising
        private void EnterChaisingState()
        {

        }

        private void UpdateChaisingState()
        {
            if (!PlayerInFieldOfView(viewRadius, horizontalViewAngle, verticalViewAngle, whatIsPlayer, whatIsObstacle))
            {
                if (!navAgent.pathPending)
                {
                    if (navAgent.remainingDistance <= remainingDistanceToPoint)
                    {
                        navAgent.SetDestination(lastPlayerPosition);
                    }
                    else
                    {
                        SwitchState(AIState.Idle);
                    }
                }
            }

            transform.LookAt(lastPlayerPosition);
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
            if (PlayerInFieldOfView(viewRadius, horizontalViewAngle, verticalViewAngle, whatIsPlayer, whatIsObstacle))
            {
                SwitchState(AIState.Chaising);
            }
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
            if (PlayerInFieldOfView(viewRadius, horizontalViewAngle, verticalViewAngle, whatIsPlayer, whatIsObstacle))
            {
                SwitchState(AIState.Chaising);
            }

            MoveToNextPoint();
        }

        private void ExitPatrollingState()
        {
            navAgent.destination = transform.position;
        }
        #endregion
        #endregion

        #region Custom methods
        private void MoveToNextPoint()
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= remainingDistanceToPoint)
            {
                navAgent.destination = patrollingPoints[currentPatrolIndex].position;
                currentPatrolIndex = ++currentPatrolIndex % patrollingPoints.Length;
            }
        }

        private bool PlayerInFieldOfView(float radius, float horizontalAngle, float verticalAngle, LayerMask target, LayerMask obstacle)
        {
            Collider[] colliders = OverlapSphere(transform.position, radius, target);

            for (int i = zero; i < colliders.Length; i++)
            {
                Vector3 targetPosition = colliders[i].transform.position;
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;

                float angleY = Angle(transform.forward, new Vector3(zero, directionToTarget.y, directionToTarget.z));
                float angleX = Angle(transform.forward, new Vector3(directionToTarget.x, zero, directionToTarget.z));

                if (angleY < horizontalAngle && angleX < verticalAngle)
                {
                    float distance = Distance(transform.position, colliders[i].transform.position);

                    if (!Raycast(transform.position, directionToTarget, distance, obstacle))
                    {
                        lastPlayerPosition = targetPosition;
                        return true;
                    }
                }
            }
            return false;
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
            }
        }
        #endregion
    }
}
