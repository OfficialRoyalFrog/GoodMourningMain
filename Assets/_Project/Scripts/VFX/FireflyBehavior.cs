using UnityEngine;
using Game.Core.TimeSystem; // So we can use TimeManager.Instance

[DisallowMultipleComponent]
public class FireflyBehaviour : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveRadius = 1.5f;       // distance from start point
    [SerializeField] private float moveSpeed = 0.5f;        // base wander speed
    [SerializeField] private float verticalBobAmplitude = 0.2f; // subtle vertical sway
    [SerializeField] private float verticalBobSpeed = 2f;

    [Header("Glow Flicker")]
    [SerializeField] private Light pointLight;              // optional: small light for glow
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float flickerAmount = 0.4f;    // how much the light intensity fluctuates

    [Header("Visibility")]
    [SerializeField] private float nightFadeSpeed = 2f;     // how fast they fade in/out
    [SerializeField] private float nightThreshold = 0.5f;   // from TimeManager’s nightVolume.weight (if you hook it up)

    private Vector3 startPos;
    private float randomOffset;
    private Renderer rend;
    private TrailRenderer trail;
    private float visibleLerp = 0f;

    private void Awake()
    {
        startPos = transform.position;
        randomOffset = Random.value * 10f;
        rend = GetComponentInChildren<Renderer>();
        trail = GetComponentInChildren<TrailRenderer>();
    }

    private void Update()
    {
        // If your TimeManager or Night system isn’t set up yet, skip
        if (TimeManager.Instance == null) return;

        // Compute how “night” it is (1 = night, 0 = day)
        float nightFactor = 0f;
        float hourBasedNight = (TimeManager.Instance.Hour >= TimeManager.Instance.SunsetHour || TimeManager.Instance.Hour < TimeManager.Instance.SunriseHour) ? 1f : 0f;
        nightFactor = hourBasedNight > nightThreshold ? 1f : 0f;

        // Smooth fade for appearance
        visibleLerp = Mathf.MoveTowards(visibleLerp, nightFactor, Time.deltaTime * nightFadeSpeed);

        // Enable/disable render/trail visibility based on fade
        if (rend != null) rend.enabled = visibleLerp > 0.05f;
        if (trail != null) trail.emitting = visibleLerp > 0.05f;

        // Simple wandering movement (sine-based, organic)
        float t = Time.time * moveSpeed + randomOffset;
        Vector3 offset = new Vector3(
            Mathf.Sin(t * 0.7f) * moveRadius,
            Mathf.Sin(t * verticalBobSpeed) * verticalBobAmplitude,
            Mathf.Cos(t * 0.9f) * moveRadius
        );
        transform.position = startPos + offset;

        // Optional flicker if you added a small Point Light for extra glow
        if (pointLight != null)
        {
            float flicker = 1f + Mathf.Sin(Time.time * flickerSpeed + randomOffset) * flickerAmount;
            pointLight.intensity = flicker * visibleLerp;
        }
    }
}