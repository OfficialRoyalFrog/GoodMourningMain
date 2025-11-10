using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Core.TimeSystem;

[DisallowMultipleComponent]
public class FireflySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private FireflyBehaviour fireflyPrefab;   // your Firefly prefab

    [Header("Spawn Zones (BoxColliders)")]
    [SerializeField] private List<BoxCollider> zones = new();  // add your Zone_A, Zone_B, etc.

    [Header("Placement")]
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private LayerMask groundMask = ~0;        // set to your Ground layer if you have one
    [SerializeField] private float groundRayLen = 20f;
    [SerializeField] private float heightOffset = 0.4f;        // hover above ground
    [SerializeField] private Vector2 extraHeightJitter = new(0.1f, 0.6f);

    [Header("Counts & Rates")]
    [SerializeField] private int   maxCountAtNight = 35;       // peak population at night (per spawner)
    [SerializeField] private float spawnPerSecond  = 8f;       // how fast we fill up at night
    [SerializeField] private float cullPerSecond   = 10f;      // how fast we remove by day

    [Header("Night Factor (choose 1)")]
    [Tooltip("If assigned, we read this volume's weight (0..1) for day→night blend.")]
    [SerializeField] private Volume nightVolume;               // drag your GlobalVolume_Night (optional)
    [SerializeField, Range(0,180)] private int featherMinutes = 60; // fallback when volume is not assigned

    [Header("Camera Proximity")]
    [SerializeField] private Camera targetCamera;          // if null, auto uses Camera.main
    [SerializeField] private float spawnInRadius = 18f;    // full density inside this radius (m)
    [SerializeField] private float spawnOutRadius = 28f;   // density fades to 0 by this radius (m)
    [SerializeField] private float hardCullRadius = 36f;   // immediately despawn beyond this radius (m)
    [SerializeField] private bool  useFrustumCheck = true; // reduce offscreen spawning

    private readonly List<FireflyBehaviour> live = new();
    private float spawnAccumulator;

    void Update()
    {
        float night = GetNightFactor();

        // Proximity across zones
        float proximitySum = 0f;
        int activeZones = 0;
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (!z) continue;
            float p = ProximityFactorForZone(z);
            if (p > 0.001f) activeZones++;
            proximitySum += p;
        }
        float proximityFactor = Mathf.Clamp(proximitySum, 0f, Mathf.Max(1, activeZones));

        int target = Mathf.RoundToInt(maxCountAtNight * night * proximityFactor);

        // Spawn toward target
        if (live.Count < target)
        {
            spawnAccumulator += spawnPerSecond * Time.deltaTime;
            int n = Mathf.FloorToInt(spawnAccumulator);
            if (n > 0) spawnAccumulator -= n;
            for (int i = 0; i < n; i++) SpawnOne();
        }

        // Cull toward target
        if (live.Count > target)
        {
            int kill = Mathf.Min(live.Count - target, Mathf.CeilToInt(cullPerSecond * Time.deltaTime));
            for (int i = 0; i < kill; i++) DespawnOne();
        }

        // Safety: hard-cull anything far from the camera
        HardCullFarFireflies();
    }

    void SpawnOne()
    {
        if (!fireflyPrefab || zones == null || zones.Count == 0) return;

        // Try a few times to pick an active (nearby) zone
        for (int tries = 0; tries < 8; tries++)
        {
            var zone = zones[Random.Range(0, zones.Count)];
            if (!zone) continue;
            if (ProximityFactorForZone(zone) <= 0.001f) continue; // too far/offscreen

            Vector3 p = RandomPointInBox(zone);

            if (snapToGround)
            {
                Vector3 top = new Vector3(p.x, zone.bounds.max.y + 2f, p.z);
                if (Physics.Raycast(top, Vector3.down, out var hit, groundRayLen, groundMask))
                {
                    p = hit.point;
                }
            }

            p.y += heightOffset + Random.Range(extraHeightJitter.x, extraHeightJitter.y);

            var inst = Instantiate(fireflyPrefab, p, Quaternion.identity, transform);
            inst.name = "Firefly";
            live.Add(inst);
            return;
        }
    }

    void DespawnOne()
    {
        if (live.Count == 0) return;
        int i = Random.Range(0, live.Count);
        var f = live[i];
        if (f) Destroy(f.gameObject);
        live.RemoveAt(i);
    }

    Vector3 RandomPointInBox(BoxCollider box)
    {
        // pick local point in [-0.5..0.5] and transform to world
        Vector3 local = new Vector3(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
        Vector3 world = box.transform.TransformPoint(box.center + Vector3.Scale(local, box.size));
        return world;
    }

    float ProximityFactorForZone(BoxCollider zone)
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera) return 0f;

        var camPos = targetCamera.transform.position;
        var closest = zone.ClosestPoint(camPos);
        // Horizontal distance only for a top-down/2.5D feel
        closest.y = camPos.y;
        float dist = Vector3.Distance(closest, camPos);

        // Smooth ramp from spawnOutRadius (0) to spawnInRadius (1)
        float t = Mathf.InverseLerp(spawnOutRadius, spawnInRadius, dist);
        t = Mathf.SmoothStep(0f, 1f, t);

        if (useFrustumCheck)
        {
            Vector3 vp = targetCamera.WorldToViewportPoint(zone.bounds.center);
            bool onScreen = vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
            if (!onScreen) t *= 0.5f; // de-prioritize offscreen zones a bit
        }
        return t;
    }

    float GetNightFactor()
    {
        if (nightVolume) return Mathf.Clamp01(nightVolume.weight);
        if (TimeManager.Instance == null) return 0f;

        float minutes = TimeManager.Instance.CurrentHourFloat * 60f;
        float start   = TimeManager.Instance.SunsetHour      * 60f;
        float end     = TimeManager.Instance.SunriseHour     * 60f;

        return NightBlend(minutes, start, end, 1440f, Mathf.Max(1, featherMinutes));
    }

    static float NightBlend(float x, float start, float end, float wrap, float feather)
    {
        bool inside = (start < end) ? (x >= start && x < end) : (x >= start || x < end);
        float distToA = MinRingDistance(x, start, wrap);
        float distToB = MinRingDistance(x, end,   wrap);
        float d = Mathf.Min(distToA, distToB);
        float t = Mathf.Clamp01(d / Mathf.Max(1f, feather));
        return inside ? Mathf.SmoothStep(1f, 0f, t) : Mathf.SmoothStep(0f, 1f, t);
    }

    static float MinRingDistance(float a, float b, float wrap)
    {
        float d = Mathf.Abs(a - b);
        return Mathf.Min(d, wrap - d);
    }

#if UNITY_EDITOR
    void HardCullFarFireflies()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera) return;

        var camPos = targetCamera.transform.position;
        float r2 = hardCullRadius * hardCullRadius;

        for (int i = live.Count - 1; i >= 0; i--)
        {
            var f = live[i];
            if (!f) { live.RemoveAt(i); continue; }
            Vector3 p = f.transform.position; p.y = camPos.y;
            if ((p - camPos).sqrMagnitude > r2)
            {
                Destroy(f.gameObject);
                live.RemoveAt(i);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (!z) continue;
            Gizmos.matrix = z.transform.localToWorldMatrix;
            Gizmos.DrawCube(z.center, z.size);
        }
    }
#endif
#endif
}