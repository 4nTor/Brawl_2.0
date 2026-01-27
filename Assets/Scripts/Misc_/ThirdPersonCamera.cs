using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float followSmoothSpeed = 15f;

    [Header("Rotation")]
    public float mouseSensitivity = 2.5f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public float rotationSmoothSpeed = 12f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float cameraDistance = 4f;

    [Header("Collision")]
    public float collisionRadius = 0.3f;
    public LayerMask collisionLayers;

    float yaw;
    float pitch;

    void Awake()
    {
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        FollowTarget();
        RotateCamera();
        PositionCamera();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // Snap camera immediately to avoid jump
        transform.position = target.position + pivotOffset;

        yaw = target.eulerAngles.y;
        pitch = 10f;
    }

    void FollowTarget()
    {
        Vector3 desiredPos = target.position + pivotOffset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSmoothSpeed * Time.deltaTime
        );
    }

    void RotateCamera()
    {
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    void PositionCamera()
    {
        Vector3 desiredLocalPos = new Vector3(0f, 0f, -cameraDistance);
        Vector3 desiredWorldPos = transform.TransformPoint(desiredLocalPos);
        Vector3 pivot = transform.position;

        if (Physics.SphereCast(
            pivot,
            collisionRadius,
            (desiredWorldPos - pivot).normalized,
            out RaycastHit hit,
            cameraDistance,
            collisionLayers
        ))
        {
            desiredWorldPos = hit.point + hit.normal * collisionRadius;
        }

        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            desiredWorldPos,
            20f * Time.deltaTime
        );

        cameraTransform.LookAt(pivot);
    }
}
