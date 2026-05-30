using Pathfinding;
using UnityEngine;

namespace RedDust.Character.Pathfinding
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AIPath))]
	public sealed class PathfindingAgent : MonoBehaviour
	{
		private IAstarAI ai;

		public IAstarAI Ai => ai;
		public Vector3 DesiredVelocity => ai != null ? ai.desiredVelocity : Vector3.zero;
		public bool IsSteering => ai != null && ai.hasPath && !ai.reachedDestination;

		private void Awake()
		{
			ai = GetComponent<IAstarAI>();
			ai.updatePosition = false;
			ai.updateRotation = false;
		}

		private void Start()
		{
			ai.Teleport(transform.position, false);
		}

		public void SyncPosition()
		{
			if (ai != null)
				ai.FinalizeMovement(transform.position, transform.rotation);
		}
	}
}
