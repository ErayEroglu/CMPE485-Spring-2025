using System.Collections;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    public float upSpeed = 2f;
    public float downSpeed = 6f;
    public float moveDistance = 10f;
    public float waitTime = 1f;
    private Vector3 startPosition;
    private bool isWaiting = false;

    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(MoveTrap());
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player hit by trap!");
            if (GameStatus.instance != null)
                GameStatus.instance.GameOver();

            // Optionally destroy the player or reset its position
            // Destroy(collision.gameObject);
            // collision.gameObject.transform.position = startPosition;
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            isWaiting = true;
            StartCoroutine(WaitAndReset());
        }
    }

    IEnumerator WaitAndReset()
    {
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
    }

    IEnumerator MoveTrap()
    {
        while (true)
        {
            if (!isWaiting)
            {
                // Move up slowly
                float startTime = Time.time;
                Vector3 upPosition = startPosition + Vector3.up * moveDistance;

                while (Time.time - startTime < moveDistance / upSpeed && !isWaiting)
                {
                    float distCovered = (Time.time - startTime) * upSpeed;
                    float fracJourney = distCovered / moveDistance;
                    transform.position = Vector3.Lerp(startPosition, upPosition, fracJourney);
                    yield return null;
                }

                // Move down quickly
                startTime = Time.time;

                while (Time.time - startTime < moveDistance / downSpeed && !isWaiting)
                {
                    float distCovered = (Time.time - startTime) * downSpeed;
                    float fracJourney = distCovered / moveDistance;
                    transform.position = Vector3.Lerp(upPosition, startPosition, fracJourney);
                    yield return null;
                }
            }
            yield return null;
        }
    }
}