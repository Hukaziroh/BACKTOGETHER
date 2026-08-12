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
    private Collider2D zoneCollider;
    private bool isLocalPlayerInside = false;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (echoManager != null) echoManager.SetActive(false);
    }

    // 챕터5의 ZoneTrigger와 동일하게 트리거 이벤트를 기본으로 쓰되,
    // 챕터6 보스맵처럼 씬 시작부터 존 안에 스폰돼서 OnTriggerEnter2D 자체가 안 뜨는 경우나
    // (플레이어가 이미 겹친 채로 스폰되면 유니티 트리거가 발동 안 함),
    // 네트워크 초기 동기화 타이밍 때문에 상태가 어긋난 경우를 대비해
    // 꺼져 있는 동안만 매 프레임 직접 겹침을 재확인해서 자기 복구한다.
    void Update()
    {
        if (isSolved || zoneCollider == null) return;

        CheckLocalLightSelfHeal();

        if (echoManager != null && !echoManager.activeSelf)
        {
            CheckEchoSelfHeal();
        }
    }

    private void CheckLocalLightSelfHeal()
    {
        if (NetworkClient.localPlayer == null) return;

        bool localInside = zoneCollider.OverlapPoint(NetworkClient.localPlayer.transform.position);
        if (localInside != isLocalPlayerInside)
        {
            isLocalPlayerInside = localInside;
            if (globalLight != null) globalLight.gameObject.SetActive(localInside);
        }
    }

    private void CheckEchoSelfHeal()
    {
        foreach (var kvp in CoopPlayerIdentity.players)
        {
            CoopPlayerIdentity p = kvp.Value;
            if (p == null) continue;

            if (zoneCollider.OverlapPoint(p.transform.position))
            {
                echoManager.SetActive(true);
                UpdateEchoWaveOrigin();
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isSolved || !other.CompareTag("Player")) return;

        if (echoManager != null)
        {
            echoManager.SetActive(true);
            UpdateEchoWaveOrigin();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isSolved || !other.CompareTag("Player")) return;

        if (echoManager != null) echoManager.SetActive(false);
    }

    // 파동은 "내 번호 + 1번" 플레이어 위치를 중심으로 나간다 (챕터5 ZoneTrigger와 동일한 방식).
    // 서버 동기화가 필요 없다 - 각자 자기 화면 기준으로 파트너를 계산해서 자기 파동만 갱신하면 된다.
    private void UpdateEchoWaveOrigin()
    {
        if (echoManager == null) return;
        if (NetworkClient.localPlayer == null) return;

        CoopPlayerIdentity myIdentity = NetworkClient.localPlayer.GetComponent<CoopPlayerIdentity>();
        if (myIdentity == null) return;

        int partnerIndex = (myIdentity.playerIndex + 1) % 4;
        if (!CoopPlayerIdentity.players.TryGetValue(partnerIndex, out CoopPlayerIdentity partner)) return;
        if (partner == null) return;

        EchoManager manager = echoManager.GetComponent<EchoManager>();
        if (manager != null) manager.SetWaveOrigin(partner.transform);
    }

    public void ForceLightsOn()
    {
        isSolved = true;
        if (globalLight != null) globalLight.gameObject.SetActive(false);
        if (echoManager != null) echoManager.SetActive(false);
    }
}
