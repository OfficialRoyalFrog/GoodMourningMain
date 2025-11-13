using UnityEngine;

/// <summary>
/// Keeps a rain particle system centered over the camera/player and enforces
/// consistent box volume settings so the rain appears to fall across the world,
/// not just at the emitter origin.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RainVolumeController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, 0f);

    [Header("Volume")]
    [SerializeField] private Vector3 boxSize = new Vector3(60f, 20f, 60f);
    [SerializeField] private float emissionRate = 1200f;
    [SerializeField] private float dropSpeed = 30f;
    [SerializeField] private float dropLifetime = 1.2f;
    [SerializeField] private float gravity = 1f;
    [SerializeField] private Vector3 windVelocity = new Vector3(2f, 0f, 0f);

    [Header("Variation")]
    [SerializeField, Range(0f, 2f)] private float noiseStrength = 0.4f;

    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (followTarget == null && Camera.main != null)
            followTarget = Camera.main.transform;
        ApplySettings();
    }

    private void OnValidate()
    {
        if (ps == null)
            ps = GetComponent<ParticleSystem>();
        ApplySettings();
    }

    private void LateUpdate()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    private void ApplySettings()
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startSpeed = dropSpeed;
        main.startLifetime = Mathf.Max(0.1f, dropLifetime);
        main.gravityModifier = gravity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;
        shape.randomDirectionAmount = 0f;
        shape.randomPositionAmount = 0f;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var velocity = ps.velocityOverLifetime;
        if (windVelocity.sqrMagnitude > 0.001f)
        {
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(windVelocity.x);
            velocity.y = new ParticleSystem.MinMaxCurve(windVelocity.y);
            velocity.z = new ParticleSystem.MinMaxCurve(windVelocity.z);
        }
        else
        {
            velocity.enabled = false;
        }

        var noise = ps.noise;
        noise.enabled = noiseStrength > 0f;
        if (noise.enabled)
        {
            noise.strength = noiseStrength;
            noise.frequency = 0.2f;
        }
    }
}
