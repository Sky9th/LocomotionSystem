using UnityEngine;
using Random = UnityEngine.Random;

namespace RedDust.Character.Pathfinding
{
    public class PathfindingTester : MonoBehaviour
    {
        [Header("Bounds")]
        [SerializeField] private float xMin = 0f;
        [SerializeField] private float xMax = 20f;
        [SerializeField] private float zMin = -10f;
        [SerializeField] private float zMax = 10f;

        [Header("Timing")]
        [SerializeField] private float minDelay = 0.5f;
        [SerializeField] private float maxDelay = 3f;

        private PathfindingAgent agent;
        private float timer;
        private bool isWaiting;
        private Vector3 currentTarget;

        private void Awake()
        {
            agent = GetComponent<PathfindingAgent>();
        }

        private void Update()
        {
            if (agent == null) return;
            if (agent.HasPath && !agent.HasReachedDestination) return;

            if (!isWaiting)
            {
                isWaiting = true;
                timer = Random.Range(minDelay, maxDelay);
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            PickRandomDestination();
            isWaiting = false;
        }

        private void PickRandomDestination()
        {
            currentTarget = new Vector3(
                Random.Range(xMin, xMax),
                0f,
                Random.Range(zMin, zMax));

            agent.SetDestination(currentTarget);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTarget, 0.3f);
        }
    }
}
