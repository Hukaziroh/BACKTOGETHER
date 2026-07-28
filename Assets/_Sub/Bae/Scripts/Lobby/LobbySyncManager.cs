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

    [SyncVar(hook = nameof(OnRoomCodeUpdated))]
    public string roomCode = "";

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (isServer)
        {
            int currentCount = NetworkServer.connections.Count;
            if (playerCount != currentCount)
            {
                playerCount = currentCount;
            }

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
        UpdateRoomCodeUI(roomCode);
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

    // ★ PauseManager에서 호출하여 상태를 동기화할 수 있는 함수
    public void RefreshRoomCodeUIState()
    {
        UpdateRoomCodeUI(roomCode);
    }

    private void UpdateRoomCodeUI(string code)
    {
        if (roomCodeText != null)
        {
            // PauseManager가 존재하고, 코드가 가려져 있는 상태(isCodeVisible == false)인지 확인
            bool isVisible = PauseManager.instance == null || PauseManager.instance.IsCodeVisible;

            string formattedCode = string.IsNullOrEmpty(code) ? "-" : code;

            // 현재 TMP에 적힌 텍스트에서 콜론(:)을 기준으로 앞쪽 라벨(예: "Room Code: ")을 추출
            string prefix = "Room Code: ";
            if (!string.IsNullOrEmpty(roomCodeText.text))
            {
                int colonIndex = roomCodeText.text.IndexOf(':');
                if (colonIndex != -1)
                {
                    prefix = roomCodeText.text.Substring(0, colonIndex + 2); // 콜론과 공백 포함해서 추출
                }
            }

            if (isVisible)
            {
                roomCodeText.text = prefix + formattedCode;
            }
            else
            {
                roomCodeText.text = prefix + "******";
            }
        }
    }
}