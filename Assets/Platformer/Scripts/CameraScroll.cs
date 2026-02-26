using UnityEngine;
using UnityEngine.InputSystem;

public class CameraScroll : MonoBehaviour
{
   [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Dead Zone (world units)")]
    [SerializeField] private float deadZoneX = 1.5f;  // player can move this far before camera moves
    [SerializeField] private float deadZoneY = 1.0f;

    [Header("Mario-Style Rules")]
    [SerializeField] private bool lockBackwardScroll = true;  // camera x never decreases

    [Header("Bounds (optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds; // x,y
    [SerializeField] private Vector2 maxBounds; // x,y

    private Vector3 _velocity;
    private float _minCameraX; // used to prevent scrolling backward

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("RetroMarioCamera: Target not assigned.");
            enabled = false;
            return;
        }

        Vector3 startPos = target.position + offset;
        transform.position = startPos;

        _minCameraX = transform.position.x;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 camPos = transform.position;
        Vector3 desired = camPos;

        // Where the camera "wants" to be (target + offset)
        Vector3 targetPos = target.position + offset;

        // --- Dead zone X ---
        float dx = targetPos.x - camPos.x;
        if (Mathf.Abs(dx) > deadZoneX)
        {
            // Move desired X only enough to bring target back to edge of dead zone
            desired.x = targetPos.x - Mathf.Sign(dx) * deadZoneX;
        }

        // --- Dead zone Y ---
        float dy = targetPos.y - camPos.y;
        if (Mathf.Abs(dy) > deadZoneY)
        {
            desired.y = targetPos.y - Mathf.Sign(dy) * deadZoneY;
        }

        // Lock backward scrolling (classic Mario feel)
        if (lockBackwardScroll)
        {
            desired.x = Mathf.Max(desired.x, _minCameraX);
        }

        // Smooth follow
        Vector3 smoothed = Vector3.SmoothDamp(camPos, desired, ref _velocity, smoothTime);

        // Apply bounds (optional)
        if (useBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minBounds.x, maxBounds.x);
            smoothed.y = Mathf.Clamp(smoothed.y, minBounds.y, maxBounds.y);
        }

        // Keep camera z from offset (useful if target z changes)
        smoothed.z = offset.z + target.position.z;

        transform.position = smoothed;

        // Update forward limit after moving
        if (lockBackwardScroll)
            _minCameraX = transform.position.x;
    }
}
