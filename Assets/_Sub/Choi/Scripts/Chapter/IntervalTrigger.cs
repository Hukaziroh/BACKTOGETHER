using UnityEngine;
using TMPro;
using Mirror;

public class IntervalTrigger : NetworkBehaviour
{
    public TMP_Text timerText;
    private const float MAX_TIME = 5.0f; // 5초

    // [SyncVar]: 서버에서 관리하는 상태와 다음 전환 목표 시각을 모든 클라이언트에 자동 동기화
    [SyncVar]
    private bool isForward = true;

    [SyncVar]
    private float nextChangeTime;

    // 클라이언트 측에서 내 로컬 플레이어가 트리거 안에 있는지 여부
    private bool isLocalPlayerInside = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 서버 시작 시 첫 번째 전환 시각 설정
        nextChangeTime = Time.time + MAX_TIME;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isLocalPlayer)
        {
            isLocalPlayerInside = true;
            if (timerText != null) timerText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isLocalPlayer)
        {
            isLocalPlayerInside = false;
            if (timerText != null) timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. [서버 전용] 시간이 다 되면 상태를 반전시키고 다음 목표 시각을 갱신합니다.
        if (isServer)
        {
            if (Time.time >= nextChangeTime)
            {
                isForward = !isForward;
                nextChangeTime = Time.time + MAX_TIME;
            }
        }

        // 2. [클라이언트/서버 공통] 로컬 플레이어가 트리거 안에 있을 때 실시간 카운트다운 및 색상 적용
        if (isLocalPlayerInside && timerText != null)
        {
            // 남은 시간 계산 (목표 시각 - 현재 시각)
            float timeLeft = nextChangeTime - Time.time;
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
}