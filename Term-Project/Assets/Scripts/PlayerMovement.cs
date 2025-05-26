using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent Agent;
    private Ray ray;
    private RaycastHit[] Hits = new RaycastHit[10];

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.RaycastNonAlloc(ray, Hits) > 0)
            {
                if (Agent != null && Agent.enabled && Agent.isActiveAndEnabled && Agent.isOnNavMesh)
                {
                    Agent.SetDestination(Hits[0].point);
                }
                else
                {
                    Debug.LogWarning("NavMeshAgent is not ready or not on NavMesh. Make sure the GameObject is positioned on a NavMesh.");
                }
            }
        }
    }
}