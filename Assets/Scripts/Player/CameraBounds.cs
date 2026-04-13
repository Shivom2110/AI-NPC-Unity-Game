using UnityEngine;

/// <summary>
/// Attach to CameraRig. Define a box in the Inspector — the camera
/// will never move outside it. Resize the green box in Scene view.
/// </summary>
public class CameraBounds : MonoBehaviour
{
    [Header("Bounds Center and Size")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize   = new Vector3(30f, 10f, 30f);

    [Header("Settings")]
    [Tooltip("How fast camera snaps back inside bounds")]
    public float correctionSpeed = 20f;

    private Bounds bounds;

    void LateUpdate()
    {
        bounds = new Bounds(boundsCenter, boundsSize);

        Transform cam = Camera.main.transform;
        if (cam == null) return;

        Vector3 pos = cam.position;

        // Clamp camera world position inside the bounds
        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
        pos.z = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);

        cam.position = Vector3.Lerp(cam.position, pos, correctionSpeed * Time.deltaTime);
    }

    // Draw the bounds box in Scene view so you can see and resize it
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(boundsCenter, boundsSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}
