using UnityEngine;

namespace Camera
{
    [DisallowMultipleComponent]
    public class CameraFollowPosition : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If null, the script will automatically find the first GameObject tagged 'Player'.")]
        [SerializeField]
        private Transform target;

        [Header("Follow")] [Tooltip("Higher = faster follow. Typical range: 3–10")] [SerializeField]
        private float followSpeed = 6f;

        [Tooltip("Whether to use FixedUpdate (recommended if player moves in FixedUpdate).")] [SerializeField]
        private bool useFixedUpdate = false;

        private Vector3 offset;
        private bool initialized;

        private void Awake()
        {
            ResolveTarget();
        }

        private void Start()
        {
            InitializeOffset();
        }

        private void ResolveTarget()
        {
            if (target != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning($"{nameof(CameraFollowPosition)}: No GameObject tagged 'Player' found.");
        }

        private void InitializeOffset()
        {
            if (target == null) return;

            offset = transform.position - target.position;
            initialized = true;
        }

        private void Update()
        {
            if (!useFixedUpdate)
                Follow(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
                Follow(Time.fixedDeltaTime);
        }

        private void Follow(float deltaTime)
        {
            if (!initialized || target == null) return;

            Vector3 desiredPosition = target.position + offset;

            // Position-only smoothing (orientation untouched)
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-followSpeed * deltaTime)
            );
        }
    }
}