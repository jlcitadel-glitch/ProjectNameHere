using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    [Header("Camera Target")]
    [SerializeField] Transform target;

    [Header("Camera Settings")]
    [SerializeField] float smoothSpeed = 0.125f;
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10f);

    private void Awake()
    {
        if (!target)
        {
            Debug.LogError("CameraFollowScript: Target transform is not assigned!");
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}