using UnityEngine;
using TMPro;
using Mirror;

public class IntervalTrigger : NetworkBehaviour
{
    [Header("설정")]
    public float toggleInterval = 5f;

    [Header("UI 연결")]
    public TMP_Text timerText;

    // 서버가 결정하는 현재 방향 상태 (true: 정방향, false: 역방향)
    [SyncVar]
    public bool isForward = true;

    [SyncVar]
    private double nextChangeNetworkTime;

    private bool isLocalPlayerInside = false;

    private void Start()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        nextChangeNetworkTime = NetworkTime.time + toggleInterval;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // 플레이어에게 존 참조 전달
                player.SetCurrentReverseZone(this);
            }

            NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
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
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // 플레이어 존 참조 해제
                player.ClearCurrentReverseZone();
            }

            NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
            if (netIdentity != null && netIdentity.isLocalPlayer)
            {
                isLocalPlayerInside = false;
                if (timerText != null) timerText.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (isServer)
        {
            if (NetworkTime.time >= nextChangeNetworkTime)
            {
                isForward = !isForward;
                nextChangeNetworkTime = NetworkTime.time + toggleInterval;
            }
        }

        if (!isLocalPlayerInside || timerText == null) return;

        float timeLeft = (float)(nextChangeNetworkTime - NetworkTime.time);
        if (timeLeft < 0) timeLeft = 0;

        string arrowChar = isForward ? "->" : "<-";
        timerText.text = $"{arrowChar}";

        if (timeLeft > 3f) timerText.color = Color.green;
        else if (timeLeft > 1f) timerText.color = Color.yellow;
        else timerText.color = Color.red;
    }
}