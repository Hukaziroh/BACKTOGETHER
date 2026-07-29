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
            // 1. 현재 PauseManager의 눈 상태 가져오기
            bool isVisible = PauseManager.instance == null || PauseManager.instance.IsCodeVisible;

            string formattedCode = string.IsNullOrEmpty(code) ? "-" : code;

            // 2. 기본 프리픽스 추출 (언어별로 바뀌는 Code: / 코드: 등의 라벨과 줄바꿈 인식)
            string prefix = "Code:";

            if (!string.IsNullOrEmpty(roomCodeText.text))
            {
                int newlineIndex = roomCodeText.text.IndexOf('\n');
                if (newlineIndex != -1 && roomCodeText.text.Length >= newlineIndex + 1)
                {
                    prefix = roomCodeText.text.Substring(0, newlineIndex + 1);
                }
                else
                {
                    int colonIndex = roomCodeText.text.IndexOf(':');
                    if (colonIndex != -1 && roomCodeText.text.Length >= colonIndex + 2)
                    {
                        prefix = roomCodeText.text.Substring(0, colonIndex + 2);
                    }
                }
            }

            // 3. 만약 언어가 바뀌어서 텍스트가 강제로 리셋되었더라도, 
            //    실제 내부 눈 상태(isVisible)가 켜져 있다면 곧바로 숫자로 복구해 줌!
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