using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class BossSmokeParticles : MonoBehaviour
{
    [Header("텍스처 (검은 실루엣 이미지 넣은 머티리얼, Sprites-Default 셰이더 추천)")]
    public Material particleMaterial;

    [Header("범위 (보스 몸통 크기에 맞게 조절)")]
    public Vector2 areaSize = new Vector2(3f, 1.8f);

    [Header("입자 개수/크기")]
    public int particleCount = 18;
    public Vector2 sizeRange = new Vector2(0.6f, 1.4f);
    public Vector2 lifetimeRange = new Vector2(2.5f, 3.5f);

    [Header("출렁임/일렁임")]
    public float driftSpeed = 0.05f;
    public float waveStrength = 0.3f;
    public float waveFrequency = 0.4f;

    void Awake()
    {
        SetUp();
    }

    void SetUp()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = particleCount * 2;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = particleCount / ((lifetimeRange.x + lifetimeRange.y) * 0.5f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(areaSize.x, areaSize.y, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);
        vel.y = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = waveStrength;
        noise.frequency = waveFrequency;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.85f),
            new Keyframe(0.5f, 1.1f),
            new Keyframe(1f, 0.85f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 새로 생기고 사라질 때 뚝 끊기지 않게 아주 짧게만 페이드 (가장자리 자체는 그대로 딱딱함)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(1f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (particleMaterial != null) renderer.material = particleMaterial;
    }
}
