using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
public class TrailTailLength : MonoBehaviour
{
    [Tooltip("How long the trail lasts, in seconds. Higher = longer trail.")]
    [Min(0f)] public float tailTime = 0.8f;

    private TrailRenderer trail;

    void OnEnable()  => Apply();
    void OnValidate() => Apply();
    void Reset()      => Apply();

    void Apply()
    {
        if (!trail) trail = GetComponent<TrailRenderer>();
        if (!trail) return;
        trail.time = tailTime;
    }
}