using UnityEngine;

public class BillboardY : MonoBehaviour
{
    private Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Face the camera while locking vertical tilt (rotate around Y only)
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(fwd);
    }
}