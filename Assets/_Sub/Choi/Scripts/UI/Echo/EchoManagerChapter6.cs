using UnityEngine;

public class EchoManagerChapter6 : MonoBehaviour
{
    [Header("Chapter 6 Target Settings")]
    [Tooltip("파동이 시작될 중심 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트 위치 사용)")]
    public Transform fixedTarget;

    [Header("Wave Speed & Distance")]
    [Tooltip("파동 퍼지는 속도")]
    public float waveSpeed = 15f;

    [Tooltip("최대 반지름 (파동이 도달하는 최종 거리)")]
    public float maxRadius = 800f;

    [Tooltip("파동이 투명해지기 시작하는 거리")]
    public float fadeStartRadius = 720f;

    [Tooltip("파동 두께")]
    public float waveWidth = 5f;

    [Header("Burst & Cycle Settings")]
    [Tooltip("3연사 중 각 파동 간격(초)")]
    public float waveInterval = 0.5f;

    [Tooltip("다음 세트 파동이 발사되기 전 대기시간(초)")]
    public float waveCooldown = 2f;

    [Header("Loop Option")]
    [Tooltip("체크 ON : 파동이 maxRadius까지 다 퍼져 완전히 사라진 뒤에만 쿨다운 시작\n체크 OFF : 3발을 쏘자마자 퍼지는 중이어도 waveCooldown 후 바로 재발사")]
    public bool waitUntilWavesFinish = false;

    [Header("Scene Gizmo Settings")]
    [Tooltip("체크 시 씬 뷰에서 파동 범위를 원으로 시각화합니다.")]
    public bool showGizmos = true;
    [Tooltip("최대 범위(Max Radius) 원 색상")]
    public Color maxRadiusColor = Color.red;
    [Tooltip("페이드 시작 범위(Fade Start Radius) 원 색상")]
    public Color fadeStartColor = Color.cyan;

    private Vector3 waveOrigin;

    // 3개의 파동 상태 관리
    private float[] radii = new float[3] { -1f, -1f, -1f };
    private float[] alphas = new float[3] { 0f, 0f, 0f };
    private bool[] active = new bool[3] { false, false, false };

    private float timer = 0f;
    private float burstTimer = 0f;
    private int burstCount = 0;
    private bool isCooldown = false;

    void OnEnable()
    {
        Shader.SetGlobalFloat("_OutlineEnabled", 1.0f);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_OutlineEnabled", 0.0f);
        for (int i = 0; i < 3; i++) active[i] = false;
    }

    public void SetWaveOrigin(Transform target)
    {
        if (target != null) fixedTarget = target;
    }

    void Update()
    {
        Transform currentTarget = (fixedTarget != null) ? fixedTarget : transform;

        // --- 1. 파동 확장 및 알파 계산 ---
        bool anyWaveActive = false;
        for (int i = 0; i < 3; i++)
        {
            if (active[i])
            {
                radii[i] += Time.deltaTime * waveSpeed;

                if (radii[i] < fadeStartRadius)
                {
                    alphas[i] = 1.0f;
                }
                else
                {
                    float fadeProgress = (radii[i] - fadeStartRadius) / Mathf.Max(0.001f, (maxRadius - fadeStartRadius));
                    alphas[i] = Mathf.Clamp01(1.0f - fadeProgress);
                }

                if (radii[i] > maxRadius)
                {
                    active[i] = false;
                }
                else
                {
                    anyWaveActive = true;
                }
            }
            else
            {
                radii[i] = -1f;
                alphas[i] = 0f;
            }
        }

        // --- 2. 파동 발사 및 쿨다운 로직 ---
        if (!isCooldown)
        {
            burstTimer += Time.deltaTime;

            if (burstCount < 3 && (burstCount == 0 || burstTimer >= waveInterval))
            {
                radii[burstCount] = 0f;
                active[burstCount] = true;
                anyWaveActive = true;

                waveOrigin = currentTarget.position;

                burstCount++;
                burstTimer = 0f;
            }

            if (burstCount >= 3)
            {
                if (!waitUntilWavesFinish || !anyWaveActive)
                {
                    isCooldown = true;
                    timer = 0f;
                }
            }
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= waveCooldown)
            {
                isCooldown = false;
                burstCount = 0;
                burstTimer = 0f;
                timer = 0f;
            }
        }

        // --- 3. 셰이더로 데이터 전달 ---
        Shader.SetGlobalVector("_WavePos", waveOrigin);
        Shader.SetGlobalFloat("_WaveRadius1", radii[0]); Shader.SetGlobalFloat("_WaveAlpha1", alphas[0]);
        Shader.SetGlobalFloat("_WaveRadius2", radii[1]); Shader.SetGlobalFloat("_WaveAlpha2", alphas[1]);
        Shader.SetGlobalFloat("_WaveRadius3", radii[2]); Shader.SetGlobalFloat("_WaveAlpha3", alphas[2]);
        Shader.SetGlobalFloat("_WaveWidth", waveWidth);
    }

    // --- 💡 씬 뷰 범위 시각화 (기즈모) ---
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 center = (fixedTarget != null) ? fixedTarget.position : transform.position;

        // 1. 투명해지기 시작하는 범위 (기본값: 청록색)
        Gizmos.color = fadeStartColor;
        Gizmos.DrawWireSphere(center, fadeStartRadius);

        // 2. 파동 최대 도달 범위 (기본값: 빨간색)
        Gizmos.color = maxRadiusColor;
        Gizmos.DrawWireSphere(center, maxRadius);
    }
}