using UnityEngine;

/// <summary>
/// Forces this transform onto a stepped grid (e.g. multiples of cube size).
/// Attach to your BlockCell root (the object that should be grid-aligned).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BlockCellSnap : MonoBehaviour
{
    [Header("Grid")]
    [Min(0.0001f)]
    public float step = 1f;                 // e.g. cube size in world units

    public Vector3 origin = Vector3.zero;   // grid origin offset (optional)

    [Header("Axes")]
    public bool snapX = true;
    public bool snapY = true;
    public bool snapZ = true;

    [Header("When to Snap")]
    public bool snapInEditMode = true;
    public bool snapInPlayMode = false;

    // Prevents repeated snapping loops when the editor modifies transforms.
    private bool _isSnapping;

    private void OnValidate()
    {
        // Keep it robust if step is edited to something invalid.
        if (step < 0.0001f) step = 0.0001f;

        if (!Application.isPlaying && snapInEditMode)
            SnapNow();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (snapInPlayMode) SnapNow();
        }
        else
        {
            if (snapInEditMode) SnapNow();
        }
    }

    /// <summary>Call this manually if you want to snap on demand.</summary>
    [ContextMenu("Snap Now")]
    public void SnapNow()
    {
        if (_isSnapping) return;
        _isSnapping = true;

        try
        {
            Vector3 p = transform.position;

            // Convert into grid space relative to origin, snap, then convert back.
            Vector3 gp = p - origin;

            if (snapX) gp.x = SnapScalar(gp.x, step);
            if (snapY) gp.y = SnapScalar(gp.y, step);
            if (snapZ) gp.z = SnapScalar(gp.z, step);

            Vector3 snapped = gp + origin;

            // Only assign if changed (avoids dirtying scenes unnecessarily).
            if ((snapped - p).sqrMagnitude > 0.0000001f)
                transform.position = snapped;
        }
        finally
        {
            _isSnapping = false;
        }
    }

    private static float SnapScalar(float value, float stepSize)
    {
        // Round to nearest step interval.
        // If you want floor/ceil behavior instead, swap Mathf.Round for Floor/Ceil.
        return Mathf.Round(value / stepSize) * stepSize;
    }
}