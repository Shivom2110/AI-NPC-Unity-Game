using UnityEngine;

public class DarkSoulsCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform cameraPivot;

    [Header("Sensitivity")]
    public float sensitivityX = 2f;
    public float sensitivityY = 1.5f;

    [Header("Pitch Limits")]
    public float minPitch = -20f;
    public float maxPitch  =  35f;

    [Header("Distance")]
    public float defaultDistance = 3f;
    public float cameraRadius    = 0.2f;
    public LayerMask collisionLayers;

    [Header("Follow Speed")]
    public float followSpeed = 15f;

    private float yaw;
    private float pitch;

    void Start()
    {
        yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void LateUpdate()
    {
        if (player == null) return;

        FollowPlayer();
        HandleRotation();
        HandleCollision();
    }

    void FollowPlayer()
    {
        transform.position = Vector3.Lerp(
            transform.position, player.position, followSpeed * Time.deltaTime);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation        = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleCollision()
    {
        Transform cam      = Camera.main.transform;
        Vector3 desiredPos = cameraPivot.TransformPoint(new Vector3(0f, 0f, -defaultDistance));
        Vector3 dir        = (desiredPos - cameraPivot.position).normalized;

        if (Physics.SphereCast(
            cameraPivot.position, cameraRadius,
            dir, out RaycastHit hit,
            defaultDistance, collisionLayers))
        {
            cam.localPosition = Vector3.Lerp(
                cam.localPosition,
                new Vector3(0f, 0f, -(hit.distance - cameraRadius)),
                10f * Time.deltaTime);
        }
        else
        {
            cam.localPosition = Vector3.Lerp(
                cam.localPosition,
                new Vector3(0f, 0f, -defaultDistance),
                10f * Time.deltaTime);
        }
    }
}
