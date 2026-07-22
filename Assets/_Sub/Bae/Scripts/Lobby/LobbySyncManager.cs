using UnityEngine;
using TMPro;
using Mirror;

public class LobbySyncManager : NetworkBehaviour
{
    public static LobbySyncManager instance;

    [Header("왼쪽 UI: 플레이어 인원수")]
    public TextMeshProUGUI playerCountText;

    [SyncVar(hook = nameof(OnPlayerCountUpdated))]
    public int playerCount = 0;

    [Header("오른쪽 UI: 룸코드")]
    public TextMeshProUGUI roomCodeText;

    // ★ Mirror SyncVar: 호스트(서버)가 이 값을 바꾸면 클라이언트들에게 자동으로 동기화됨!
    [SyncVar(hook = nameof(OnRoomCodeUpdated))]
    public string roomCode = "";

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // 서버(호스트) 전용 로직
        if (isServer)
        {
            // 1. 실시간 인원수 체크
            int currentCount = NetworkServer.connections.Count;
            if (playerCount != currentCount)
            {
                playerCount = currentCount;
            }

            // 2. 호스트가 발급받은 숏코드가 생성되었다면 SyncVar에 등록하여 클라이언트들에게 전송
            if (string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
            {
                roomCode = PrivateLobbyManager.currentShortCode;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdatePlayerCountUI(playerCount);
        UpdateRoomCodeUI(roomCode); // 클라이언트 접속 시 초기화
    }

    [Server]
    public void RefreshPlayerCount()
    {
        playerCount = NetworkServer.connections.Count;
    }

    void OnPlayerCountUpdated(int oldValue, int newValue)
    {
        UpdatePlayerCountUI(newValue);
    }

    void OnRoomCodeUpdated(string oldValue, string newValue)
    {
        UpdateRoomCodeUI(newValue);
    }

    private void UpdatePlayerCountUI(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Player: {count} / 4";
        }
    }

    private void UpdateRoomCodeUI(string code)
    {
        if (roomCodeText != null)
        {
            roomCodeText.text = $"Room Code: {(string.IsNullOrEmpty(code) ? "-" : code)}";
        }
    }
}