using UnityEngine;

/// Attach to each spawned Spirit instance (the Hub Prefab root).
/// Lightweight wander inside a rectangle, with gentle hover bob.
[RequireComponent(typeof(Transform))]
public class SpiritAgent : MonoBehaviour
{
    [Header("Wander")]
    public float moveSpeed = 1.4f;
    public float turnSpeed = 360f;     // deg/sec when steering
    public float idleTimeMin = 1.0f;
    public float idleTimeMax = 2.0f;
    public float roamRadiusPadding = 0.4f; // keep a small margin from bounds
    [Tooltip("Used if roamBounds was never assigned. Creates a local square area around spawn.")]
    [SerializeField] private float fallbackRoamRadius = 2.0f;

    [Header("Hover")]
    public float hoverAmplitude = 0.1f;
    public float hoverSpeed = 2.0f;

    [Header("Visual Facing")]
    public Transform visual;           // optional child sprite transform to face left/right
    public bool flipUsingScale = true;
    public Vector3 leftScale = new(1,1,1);
    public Vector3 rightScale = new(-1,1,1);

    // Assigned by spawner:
    [HideInInspector] public Bounds roamBounds;
    private Vector3 _basePos; // y baseline for hover
    private Vector3 _target;
    private float _idleTimer;
    private float _hoverT;

    /// <summary>
    /// Allows external systems (e.g., SpiritManager) to assign roaming bounds at runtime.
    /// </summary>
    public void SetRoamBounds(Bounds b)
    {
        roamBounds = b;
    }

    void Start()
    {
        _basePos = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        // Hover
        _hoverT += Time.deltaTime * hoverSpeed;
        var hoverY = Mathf.Sin(_hoverT) * hoverAmplitude;

        // Idle dwell
        if (_idleTimer > 0f)
        {
            _idleTimer -= Time.deltaTime;
            transform.position = new Vector3(transform.position.x, _basePos.y + hoverY, transform.position.z);
            return;
        }

        // Move toward target
        Vector3 to = _target - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        if (dist < 0.05f)
        {
            // reached → idle briefly then pick a new target in bounds
            _idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            PickNewTarget();
        }
        else
        {
            Vector3 dir = to / Mathf.Max(dist, 0.0001f);
            // Face / flip
            if (visual && flipUsingScale)
            {
                bool goingRight = dir.x > 0f;
                visual.localScale = goingRight ? rightScale : leftScale;
            }
            // Move
            transform.position += dir * moveSpeed * Time.deltaTime;
            // Optional rotate toward direction (not critical for sprites)
            if (dir.sqrMagnitude > 0.0001f)
            {
                var rot = Quaternion.LookRotation(new Vector3(dir.x,0f,dir.z), Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
            }
            // Apply hover baseline
            transform.position = new Vector3(transform.position.x, _basePos.y + hoverY, transform.position.z);
        }
    }

    void PickNewTarget()
    {
        // Choose a random point inside the roam bounds (XZ), padded
        var b = roamBounds;
        // Safety: if no bounds provided (zero-sized), use a local square around the base position
        if (b.size.sqrMagnitude < 0.0001f)
        {
            float r = Mathf.Max(0.1f, fallbackRoamRadius);
            var center = new Vector3(_basePos.x, transform.position.y, _basePos.z);
            b = new Bounds(center, new Vector3(r * 2f, 0.1f, r * 2f));
        }
        float padX = Mathf.Max(roamRadiusPadding, 0f);
        float padZ = Mathf.Max(roamRadiusPadding, 0f);

        float minX = b.min.x + padX;
        float maxX = b.max.x - padX;
        float minZ = b.min.z + padZ;
        float maxZ = b.max.z - padZ;

        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);

        // Keep Y baseline from where the spawner placed us
        _target = new Vector3(x, transform.position.y, z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Determine the effective bounds (use fallback if needed)
        var b = roamBounds;
        if (b.size.sqrMagnitude < 0.0001f)
        {
            float r = Mathf.Max(0.1f, fallbackRoamRadius);
            var center = Application.isPlaying ? _basePos : transform.position;
            b = new Bounds(new Vector3(center.x, transform.position.y, center.z), new Vector3(r * 2f, 0.1f, r * 2f));
        }

        // Draw XZ rectangle
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.4f);
        var min = b.min; var max = b.max;
        Vector3 a = new Vector3(min.x, transform.position.y, min.z);
        Vector3 c = new Vector3(max.x, transform.position.y, max.z);
        Vector3 b1 = new Vector3(a.x, a.y, c.z);
        Vector3 d = new Vector3(c.x, c.y, a.z);
        Gizmos.DrawLine(a, b1); Gizmos.DrawLine(b1, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
    }
#endif
}