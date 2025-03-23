using System.Collections;
using UnityEngine;

public class GuardController : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;
    public float detectionRadius = 2f;
    private bool movingTowardsB = true;

    void Start()
    {
        if (pointA == null)
        {
            GameObject pointAObj = new GameObject($"PointA_{gameObject.name}");
            pointAObj.transform.position = transform.position + transform.forward * -5f;
            pointA = pointAObj.transform;
        }

        if (pointB == null)
        {
            GameObject pointBObj = new GameObject($"PointB_{gameObject.name}");
            pointBObj.transform.position = transform.position + transform.forward * 5f;
            pointB = pointBObj.transform;
        }

        StartCoroutine(PatrolBetweenPoints());
    }
    
    void Update()
    {
        // Check for player within detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                GameStatus.instance.GameOver();
            }
        }
    }

    IEnumerator PatrolBetweenPoints()
    {
        while (true)
        {
            Transform targetPoint = movingTowardsB ? pointB : pointA;
            yield return StartCoroutine(MoveToPoint(targetPoint));
            movingTowardsB = !movingTowardsB;
        }
    }

    IEnumerator MoveToPoint(Transform point)
    {
        Vector3 direction = (point.position - transform.position).normalized;
        while (Vector3.Distance(transform.position, point.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, point.position, moveSpeed * Time.deltaTime);
            transform.forward = direction;
            yield return null;
        }
    }
}