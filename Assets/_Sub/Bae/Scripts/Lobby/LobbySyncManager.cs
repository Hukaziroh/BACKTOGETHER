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

    [SyncVar(hook = nameof(OnRoomNameUpdated))]
    public string roomName = ""; // <--- 추가: 방 이름 SyncVar

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 방 생성시 저장된 이름을 가져와 동기화
        roomName = PrivateLobbyManager.lastCreatedRoomName;
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

    void OnRoomNameUpdated(string oldValue, string newValue) // <--- 추가: 방 이름 업데이트 감지
    {
        UpdateRoomCodeUI(roomCode);
    }

    private void UpdatePlayerCountUI(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Player: {count} / 4";
        }
    }

    public void RefreshRoomCodeUIState()
    {
        UpdateRoomCodeUI(roomCode);
    }

    private void UpdateRoomCodeUI(string code)
    {
        if (roomCodeText == null) return;

        bool isMainScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main";

        // --- 수정된 부분: 공개방(code가 비어있음)일 때 방 이름 사용 ---
        string formattedCode = (isMainScene) ? "Empty" : (string.IsNullOrEmpty(code) ? roomName : code);

        // 스트리머 모드 적용 여부 확인
        bool isCodeVisible = true;
        if (OptionsManager.instance != null)
        {
            isCodeVisible = OptionsManager.instance.IsCodeVisible;
        }

        // 비공개방(code가 있고)이고 마스킹 옵션이 켜져있을 때만 마스킹
        if (!isMainScene && !string.IsNullOrEmpty(code) && !isCodeVisible)
        {
            formattedCode = "******";
        }

        // 줄바꿈 문자(\n, \r)를 유지하거나 기존 텍스트 형태에 맞춰 prefix 추출
        string prefix = "Code:\n";
        if (!string.IsNullOrEmpty(roomCodeText.text))
        {
            int splitIndex = roomCodeText.text.IndexOf('\n');
            if (splitIndex != -1 && roomCodeText.text.Length >= splitIndex + 1)
            {
                prefix = roomCodeText.text.Substring(0, splitIndex + 1);
            }
            else
            {
                int colonIndex = roomCodeText.text.IndexOf(':');
                if (colonIndex != -1)
                {
                    prefix = roomCodeText.text.Substring(0, colonIndex + 1);
                }
            }
        }

        // 최종 텍스트 적용 (줄바꿈 포함)
        roomCodeText.text = prefix + formattedCode;
    }
}