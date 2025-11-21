using UnityEngine;
using DG.Tweening;

namespace KD.World
{
    public class SummonSpotSpinner : MonoBehaviour
    {
        [SerializeField] float rotationsPerSecond = 0.5f;
        [SerializeField] bool clockwise = true;
        [SerializeField] Vector3 worldAxis = Vector3.up; // which world axis to spin around (e.g., 0,1,0 for Y)

        void Start()
        {
            float safeRps = Mathf.Max(rotationsPerSecond, 0.0001f);
            float duration = 1f / safeRps;
            float angle = clockwise ? 360f : -360f;

            // Normalize axis and convert to per-axis Euler degrees
            Vector3 axis = worldAxis.sqrMagnitude > 0.0001f ? worldAxis.normalized : Vector3.up;
            Vector3 eulerPerLoop = axis * angle; // e.g., axis (0,1,0) -> (0,360,0)

            transform
                .DORotate(eulerPerLoop, duration, RotateMode.WorldAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }
}