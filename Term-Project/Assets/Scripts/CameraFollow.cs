using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public bool autoFindPlayer = true;
    public Vector3 offset = new Vector3(0, 30, 0); // Y offset for top-down view
    public float followSpeed = 5f;
    
    private void Start()
    {
        if (autoFindPlayer && target == null)
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Camera automatically found and following player.");
            }
        }
        
        // Set initial position if target exists
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
    
    private void LateUpdate()
    {
        if (target != null)
        {
            // Calculate desired position (follow X and Z, maintain Y offset)
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y + offset.y, target.position.z + offset.z);
            
            // Smoothly move camera to desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
} 