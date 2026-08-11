using UnityEngine;
using UnityEngine.Rendering.Universal;
using Mirror;

public class EchoZoneController : NetworkBehaviour
{
    [Header("존에 들어가면 켤 라이트 (씬의 Global Light 2D)")]
    public Light2D globalLight;

    [Header("존에 들어가면 켤 에코 매니저")]
    public GameObject echoManager;

    private bool isSolved = false;

    // [개선 1] 인원수를 서버에서 SyncVar로 관리하여 모든 클라이언트에 동일하게 전파
    [SyncVar(hook = nameof(OnPlayersInsideCountChanged))]
    private int playersInsideCount = 0;

    // [개선 2] 첫 진입자 NetId를 서버에서 관리
    [SyncVar(hook = nameof(OnFirstEntrantChanged))]
    private uint firstEntrantNetId = 0;

    // 내(로컬 플레이어) 캐릭터가 현재 존 안에 있는지 (개인 연출용)
    private bool isLocalPlayerInside = false;

    private Collider2D zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (echoManager != null) echoManager.SetActive(false);
    }

    void Update()
    {
        if (isSolved || zoneCollider == null) return;

        // 1. [서버] 전체 플레이어 감지 및 SyncVar 갱신
        if (isServer)
        {
            ServerCheckPlayersInZone();
        }

        // 2. [클라이언트 & 호스트] 내 로컬 플레이어의 진입 상태만 직접 확인 (라이트 조절용)
        CheckLocalPlayerInside();

        // 3. 에코 매니저가 활성화되어 있다면 위치 주기적 갱신
        if (echoManager != null && echoManager.activeSelf)
        {
            UpdateEchoWaveOrigin();
        }
    }

    [Server]
    private void ServerCheckPlayersInZone()
    {
        int count = 0;

        foreach (var kvp in CoopPlayerIdentity.players)
        {
            CoopPlayerIdentity p = kvp.Value;
            if (p == null) continue;

            // [개선 3] Point 하나만 검사하는 OverlapPoint 대신 Bounds 기준 검사로 판정 완화
            if (zoneCollider.bounds.Contains(p.transform.position))
            {
                count++;

                if (firstEntrantNetId == 0)
                {
                    NetworkIdentity identity = p.GetComponent<NetworkIdentity>();
                    if (identity != null) firstEntrantNetId = identity.netId;
                }
            }
        }

        // 서버의 Count가 변경되면 SyncVar를 통해 모든 클라이언트로 전파됨
        playersInsideCount = count;
    }

    private void CheckLocalPlayerInside()
    {
        if (NetworkClient.localPlayer == null) return;

        // 내 로컬 플레이어 캐릭터의 위치 감지
        bool localInside = zoneCollider.bounds.Contains(NetworkClient.localPlayer.transform.position);

        if (isLocalPlayerInside != localInside)
        {
            isLocalPlayerInside = localInside;
            UpdateLocalLight();
        }
    }

    // SyncVar Hook: 서버에서 인원수가 바뀌었을 때 모든 클라이언트에서 자동 실행
    private void OnPlayersInsideCountChanged(int oldCount, int newCount)
    {
        UpdateEchoVisibility();
    }

    // SyncVar Hook: 선두 플레이어가 변경되었을 때
    private void OnFirstEntrantChanged(uint oldId, uint newId)
    {
        UpdateEchoWaveOrigin();
    }

    private void UpdateLocalLight()
    {
        if (isSolved) return;
        if (globalLight != null) globalLight.gameObject.SetActive(isLocalPlayerInside);
    }

    private void UpdateEchoVisibility()
    {
        if (echoManager == null || isSolved) return;

        bool shouldShow = playersInsideCount > 0;
        echoManager.SetActive(shouldShow);

        if (shouldShow) UpdateEchoWaveOrigin();
    }

    private void UpdateEchoWaveOrigin()
    {
        if (echoManager == null || firstEntrantNetId == 0) return;

        if (NetworkClient.spawned.TryGetValue(firstEntrantNetId, out NetworkIdentity chosenIdentity))
        {
            EchoManager manager = echoManager.GetComponent<EchoManager>();
            if (manager != null) manager.SetWaveOrigin(chosenIdentity.transform);
        }
    }

    public void ForceLightsOn()
    {
        isSolved = true;
        if (globalLight != null) globalLight.gameObject.SetActive(false);
        if (echoManager != null) echoManager.SetActive(false);
    }
}