using UnityEngine;

/// <summary>
/// <see cref="DirectionalWindZone"/> 의 현재 바람 속도를 읽어, 눈(ParticleSystem)이 바람 방향으로
/// 흩날리도록 파티클의 Velocity over Lifetime X 를 매 프레임 갱신합니다.
/// 바람이 램프로 서서히 강해지면 눈도 서서히 그 방향으로 쏠리고, 무풍이면 다시 수직으로 떨어집니다.
///
/// 사용법: Ex2 씬의 "Snow" 오브젝트(ParticleSystem)에 이 컴포넌트를 붙이고
/// <see cref="windZone"/> 슬롯에 Wind 오브젝트(DirectionalWindZone)를 연결하세요.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WindParticleVisualizer : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("방향/세기를 읽어올 바람 구역.")]
    public DirectionalWindZone windZone;

    [Header("눈 쏠림")]
    [Tooltip("바람 속도 → 파티클 가로 속도 배율. 클수록 눈이 더 많이 휩쓸립니다.")]
    public float driftMultiplier = 1.5f;

    [Tooltip("바람이 없을 때 눈의 기본 가로 속도(0 이면 무풍일 때 완전히 수직 낙하).")]
    public float baseDriftX = 0f;

    [Tooltip("파티클 가로 속도가 목표까지 따라가는 데 걸리는 시간(초). 0 이면 즉시. " +
             "바람 자체 램프에 더해지는 추가 스무딩입니다.")]
    public float smoothTime = 0.3f;

    [Header("선택: 세기에 따라 방출량 조절")]
    public bool scaleEmission = false;
    public float calmEmissionRate = 8f;
    public float strongEmissionRate = 25f;

    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule vel;
    private ParticleSystem.EmissionModule emission;
    private float driftX;
    private float driftVel;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        vel = ps.velocityOverLifetime;
        emission = ps.emission;

        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World; // +x = 항상 월드 오른쪽
        driftX = baseDriftX;
    }

    private void Update()
    {
        float wind = windZone != null ? windZone.CurrentWindVelocity : 0f;
        float target = baseDriftX + wind * driftMultiplier;

        driftX = smoothTime > 0f
            ? Mathf.SmoothDamp(driftX, target, ref driftVel, smoothTime)
            : target;

        vel.x = driftX;

        if (scaleEmission && windZone != null)
        {
            float strength = windZone.windStrength > 0f
                ? Mathf.Clamp01(Mathf.Abs(wind) / windZone.windStrength)
                : 0f;
            emission.rateOverTime = Mathf.Lerp(calmEmissionRate, strongEmissionRate, strength);
        }
    }
}
