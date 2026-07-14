using UnityEngine;
using Unity.Netcode;

public class EchoManager : MonoBehaviour
{
    // 자동으로 할당할 것이므로 private으로 변경
    private Transform player;

    public float waveSpeed = 10f;
    public float maxRadius = 20f;
    public float waveWidth = 3f;

    private float currentRadius = 0f;
    private float timer = 0f;

    // 오브젝트가 켜질 때마다 호스트를 찾음
    void OnEnable()
    {
        FindHostPlayer();
    }

    void FindHostPlayer()
    {
        // 1. 네트워크 환경 체크
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // 호스트(ClientId 0) 찾기
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(0, out var hostClient))
            {
                if (hostClient.PlayerObject != null)
                {
                    player = hostClient.PlayerObject.transform;
                    return;
                }
            }
        }

        // 2. 네트워크 연결 전이거나 호스트를 못 찾은 경우 태그로 찾기 (안전장치)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // 플레이어를 아직 못 찾았다면 매 프레임 찾기를 시도함
        if (player == null)
        {
            FindHostPlayer();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= 3.0f) // 3초 주기
        {
            currentRadius = 0f;
            timer = 0f;
        }

        if (currentRadius < maxRadius)
            currentRadius += Time.deltaTime * waveSpeed;

        // 셰이더 전역 변수 업데이트
        Shader.SetGlobalVector("_WavePos", player.position);
        Shader.SetGlobalFloat("_WaveRadius", currentRadius);
        Shader.SetGlobalFloat("_WaveWidth", waveWidth);
    }
}