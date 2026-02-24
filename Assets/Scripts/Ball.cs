using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour
{
    [SerializeField] private int influenceRadiusCells = 3; // check a 5x5 around the ball
    [SerializeField] private float maxAccel = 30f; // clamp to avoid insanity
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        var bm = BlockManager.Instance;
        if (bm == null) return;

        Vector3 totalAccel = Vector3.zero;

        foreach (var cell in bm.GetCellsInRadius(transform.position, influenceRadiusCells))
        {
            if (cell == null) continue;
            if (cell.Type == BlockType.None) continue;

            // Distance in XZ plane
            float d = cell.GetDistanceXZ(transform.position);

            // Normalize distance: 0 at center, 1 at edge of influence (in world units)
            float maxDist = bm.CellSize * influenceRadiusCells; // expose CellSize or store locally
            float t = Mathf.Clamp01(d / Mathf.Max(0.0001f, maxDist));

            float w = falloff.Evaluate(t); // 1 near, 0 far
            if (w <= 0f) continue;

            // Direction semantics by type
            Vector3 dir = Vector3.zero;
            float strength = 0f;

            switch (cell.Type)
            {
                case BlockType.Arrow:
                case BlockType.DoubleArrow:
                case BlockType.Boost:
                    dir = cell.GetForceDirectionXZ();
                    strength = cell.GetBaseStrength();
                    totalAccel += dir * (strength * w);
                    break;

                case BlockType.Sink:
                    // pull toward cell center
                    dir = (cell.transform.position - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        totalAccel += dir.normalized * (Mathf.Abs(cell.GetBaseStrength()) * w);
                    break;

                case BlockType.Source:
                    // push away from cell center
                    dir = (transform.position - cell.transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        totalAccel += dir.normalized * (cell.GetBaseStrength() * w);
                    break;
            }
        }

        totalAccel = Vector3.ClampMagnitude(totalAccel, maxAccel);

        // Apply as acceleration (mass independent)
        rb.AddForce(totalAccel, ForceMode.Acceleration);
    }
}