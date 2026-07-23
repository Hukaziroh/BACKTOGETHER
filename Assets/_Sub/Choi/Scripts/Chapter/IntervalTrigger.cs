using UnityEngine;
using TMPro;
using Mirror;

public class IntervalTrigger : NetworkBehaviour
{
    [Header("UI 연결")]
    public TMP_Text timerText;

    private ReverseZone reverseZone;
    private float toggleInterval = 5f;

    // [SyncVar]: 서버에서 관리하는 상태와 다음 전환 시각을 모든 클라이언트에 자동 동기화
    [SyncVar]
    private bool isForward = true;

    [SyncVar]
    private double nextChangeNetworkTime;

    private bool isLocalPlayerInside = false;

    private void Start()
    {
        // 동일 오브젝트에 있는 ReverseZone 컴포넌트에서 주기 가져오기
        reverseZone = GetComponent<ReverseZone>();
        if (reverseZone != null)
        {
            toggleInterval = reverseZone.toggleInterval;
        }

        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (reverseZone != null)
        {
            toggleInterval = reverseZone.toggleInterval;
        }
        // 서버 기준 첫 목표 시각 설정
        nextChangeNetworkTime = NetworkTime.time + toggleInterval;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
            // 내 로컬 플레이어일 때만 UI 켜기
            if (netIdentity != null && netIdentity.isLocalPlayer)
            {
                isLocalPlayerInside = true;
                if (timerText != null) timerText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
            // 내 로컬 플레이어일 때만 UI 끄기
            if (netIdentity != null && netIdentity.isLocalPlayer)
            {
                isLocalPlayerInside = false;
                if (timerText != null) timerText.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // 1. [서버 전용] 네트워크 시간을 기준으로 상태 반전 및 다음 목표 시각 갱신
        if (isServer)
        {
            if (reverseZone != null)
            {
                toggleInterval = reverseZone.toggleInterval;
            }

            if (NetworkTime.time >= nextChangeNetworkTime)
            {
                isForward = !isForward;
                nextChangeNetworkTime = NetworkTime.time + toggleInterval;
            }
        }

        // 2. [클라이언트/서버 공통] 내 로컬 플레이어가 트리거 안에 있을 때 실시간 카운트다운 및 색상 적용
        if (!isLocalPlayerInside || timerText == null) return;

        // 네트워크 시간을 기준으로 정확한 남은 시간 계산 (클라이언트/서버 시간 오차 방지)
        float timeLeft = (float)(nextChangeNetworkTime - NetworkTime.time);
        if (timeLeft < 0) timeLeft = 0;

        // 화살표 방향 설정
        string arrowChar = isForward ? "->" : "<-";
        timerText.text = $"{arrowChar}";

        // 요청하신 구간별 색상 적용 (5~4초: 초록 / 3~2초: 노랑 / 1초: 빨강)
        if (timeLeft > 3f)
        {
            timerText.color = Color.green; // 5 ~ 4초 구간
        }
        else if (timeLeft > 1f)
        {
            timerText.color = Color.yellow; // 3 ~ 2초 구간
        }
        else
        {
            timerText.color = Color.red; // 1초 이하 구간
        }
    }
}