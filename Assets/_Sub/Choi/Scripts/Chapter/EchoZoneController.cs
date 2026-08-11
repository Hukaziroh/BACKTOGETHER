using UnityEngine;
using UnityEngine.Rendering.Universal;
using Mirror;

public class EchoZoneController : NetworkBehaviour
{
    // 이 프로젝트 라이팅 셋업 특성: Global Light 2D는 꺼져있으면 전체가 기본으로 밝고,
    // 켜지면 그 라이트가 비추는 범위만 밝아지고 나머지는 어두워진다.
    // 그래서 "존에 들어가서 어두워져야 할 때"는 라이트를 꺼야 하는게 아니라 오히려 켜야 한다.
    [Header("존에 들어가면 켤 라이트 (씬의 Global Light 2D)")]
    public Light2D globalLight;

    [Header("존에 들어가면 켤 에코 매니저")]
    public GameObject echoManager;

    // 버튼 4개를 다 눌러서 해결되면 true로 고정 -> 존을 다시 드나들어도 반응 안 함
    private bool isSolved = false;

    // 0 = 아직 아무도 배정 안 됨. 서버가 가장 먼저 들어온 사람의 netId로 딱 한 번만 채운다.
    // 파동이 "누구" 위치에서 나갈지 정하는 용도.
    [SyncVar(hook = nameof(OnFirstEntrantChanged))]
    private uint firstEntrantNetId = 0;

    // 내(로컬 플레이어) 캐릭터가 지금 이 존 콜라이더 안에 있는지 -> 라이트는 개인별로 이걸로만 판단
    private bool isLocalPlayerInside = false;

    // 지금 이 존 안에 (누구든) 몇 명이나 있는지 -> 파동은 이게 0보다 크면 전원에게 보임
    private int playersInsideCount = 0;

    void Start()
    {
        if (echoManager != null) echoManager.SetActive(false);
    }

    void Update()
    {
        // 존 시작부터 바로 켜지는 맵(챕터6 보스맵 등)은 씬 시작 타이밍에 플레이어 스폰/
        // firstEntrantNetId 전파가 아직 안 끝났을 수 있다. 한 번 실패해도 다음 프레임에
        // 자동으로 재시도해서 자기복구하도록 함. playersInsideCount가 아니라
        // echoManager의 실제 활성 상태를 기준으로 삼아서, 트리거를 거치지 않고
        // 강제로 켜진 경우(처음부터 시작 등)에도 재시도가 걸리게 한다.
        if (echoManager != null && echoManager.activeSelf) UpdateEchoWaveOrigin();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        NetworkIdentity identity = GetPlayerIdentity(other);
        if (identity == null) return;
        if (isSolved) return;

        if (isServer && firstEntrantNetId == 0)
        {
            firstEntrantNetId = identity.netId;
        }

        // 파동은 원본 ZoneTrigger.cs처럼 "누구든" 들어가면 켜짐.
        // 4명의 콜라이더가 전부 각 클라이언트에 동일하게 시뮬레이션되므로,
        // 이 이벤트는 전원의 클라이언트에서 동시에 일어나 자동으로 전원에게 보인다.
        playersInsideCount++;
        UpdateEchoVisibility();

        // 라이트(어둠)는 본인이 들어갔을 때만 개인별로.
        if (identity.isLocalPlayer)
        {
            isLocalPlayerInside = true;
            UpdateLocalLight();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        NetworkIdentity identity = GetPlayerIdentity(other);
        if (identity == null) return;
        if (isSolved) return;

        playersInsideCount = Mathf.Max(0, playersInsideCount - 1);
        UpdateEchoVisibility();

        if (identity.isLocalPlayer)
        {
            isLocalPlayerInside = false;
            UpdateLocalLight();
        }
    }

    private NetworkIdentity GetPlayerIdentity(Collider2D other)
    {
        if (!other.CompareTag("Player")) return null;
        return other.GetComponentInParent<NetworkIdentity>();
    }

    // firstEntrantNetId가 서버에서 정해져서 뒤늦게 전파되는 경우를 대비.
    // (파동이 이미 켜진 상태에서 원점 정보만 나중에 도착하는 경우 여기서 갱신)
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
        if (echoManager == null) return;

        bool shouldShow = playersInsideCount > 0;
        echoManager.SetActive(shouldShow);

        if (shouldShow) UpdateEchoWaveOrigin();
    }

    // 파동은 항상 "선두로 배정된 1명"의 위치를 중심으로 나가야 한다.
    private void UpdateEchoWaveOrigin()
    {
        if (echoManager == null) return;
        if (firstEntrantNetId == 0) return;
        if (!NetworkClient.spawned.TryGetValue(firstEntrantNetId, out NetworkIdentity chosenIdentity)) return;

        EchoManager manager = echoManager.GetComponent<EchoManager>();
        if (manager != null) manager.SetWaveOrigin(chosenIdentity.transform);
    }

    // EchoLightButtonManager가 전원 버튼을 다 눌렀을 때 모든 클라이언트에서 호출
    public void ForceLightsOn()
    {
        isSolved = true;
        if (globalLight != null) globalLight.gameObject.SetActive(false);
        if (echoManager != null) echoManager.SetActive(false);
    }
}
