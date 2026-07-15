using UnityEngine;
using Unity.Netcode;

public class EchoManager : MonoBehaviour
{
    private Transform player;
    private Vector3 waveOrigin;

    public float waveSpeed = 20f;
    public float maxRadius = 30f;
    public float waveWidth = 3f;

    // 3개의 파동 상태 관리
    private float[] radii = new float[3] { -1f, -1f, -1f };
    private float[] alphas = new float[3] { 0f, 0f, 0f };
    private bool[] active = new bool[3] { false, false, false };

    private float timer = 0f;
    private float burstTimer = 0f;
    private int burstCount = 0;
    private bool isCooldown = false; // 3초 대기 상태인지 확인

    void OnEnable()
    {
        FindHostPlayer();
        Shader.SetGlobalFloat("_OutlineEnabled", 1.0f);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_OutlineEnabled", 0.0f);
        // 끄면 모든 파동 초기화
        for (int i = 0; i < 3; i++) active[i] = false;
    }

    void FindHostPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) { FindHostPlayer(); return; }

        timer += Time.deltaTime;

        // --- 파동 발사 로직 (버스트) ---
        if (!isCooldown)
        {
            burstTimer += Time.deltaTime;
            if (burstCount < 3 && burstTimer >= 1f)
            {
                // 새 파동 생성
                radii[burstCount] = 0f;
                active[burstCount] = true;
                waveOrigin = player.position; // 쏠 때의 위치 고정

                burstCount++;
                burstTimer = 0f;
            }

            // 3번 다 쐈으면 쿨다운 진입
            if (burstCount >= 3) { isCooldown = true; timer = 0f; }
        }
        // --- 쿨다운 로직 ---
        else if (timer >= 3.0f)
        {
            // 3초 지나면 리셋
            isCooldown = false;
            burstCount = 0;
            burstTimer = 0f;
            timer = 0f;
        }

        // 파동 확장 및 알파 계산
        for (int i = 0; i < 3; i++)
        {
            if (active[i])
            {
                radii[i] += Time.deltaTime * waveSpeed;
                alphas[i] = (radii[i] > maxRadius) ? 0f : 1.0f; // 간단하게 거리 기반

                if (radii[i] > maxRadius) active[i] = false;
            }
            else { radii[i] = -1f; alphas[i] = 0f; }
        }

        // 셰이더로 데이터 전달 (1, 2, 3번 파동 각각 전달)
        Shader.SetGlobalVector("_WavePos", waveOrigin);
        Shader.SetGlobalFloat("_WaveRadius1", radii[0]); Shader.SetGlobalFloat("_WaveAlpha1", alphas[0]);
        Shader.SetGlobalFloat("_WaveRadius2", radii[1]); Shader.SetGlobalFloat("_WaveAlpha2", alphas[1]);
        Shader.SetGlobalFloat("_WaveRadius3", radii[2]); Shader.SetGlobalFloat("_WaveAlpha3", alphas[2]);
        Shader.SetGlobalFloat("_WaveWidth", waveWidth);
    }
}