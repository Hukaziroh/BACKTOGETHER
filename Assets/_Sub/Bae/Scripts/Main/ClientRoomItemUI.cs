using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Epic.OnlineServices.Lobby;

public class ClientRoomItemUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI roomNameText;   // 방 제목
    [SerializeField] private TextMeshProUGUI chapterText;    // 챕터 (예: "Ch.3")
    [SerializeField] private TextMeshProUGUI playerCountText; // 인원수 (예: "2/4")
    [SerializeField] private Button joinButton;             // 입장 버튼

    private LobbyDetails targetLobby;
    private System.Action<LobbyDetails> onJoinCallback;

    private void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    public void Setup(LobbyDetails lobby, System.Action<LobbyDetails> onJoinClicked)
    {
        targetLobby = lobby;
        onJoinCallback = onJoinClicked;

        // 1. 방 이름 가져오기
        string roomName = "알 수 없는 방";
        Attribute attr;
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "ROOM_NAME" }, out attr) == Epic.OnlineServices.Result.Success)
        {
            if (attr.Data != null)
            {
                roomName = attr.Data.Value.AsUtf8;
            }
        }

        // 2. 챕터 가져오기
        string chapterStr = "1";
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER" }, out attr) == Epic.OnlineServices.Result.Success)
        {
            if (attr.Data != null)
            {
                chapterStr = attr.Data.Value.AsUtf8;
            }
        }

        // 3. 인원수 가져오기 (LobbyDetailsInfo 수정)
        uint currentMembers = lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
        uint maxMembers = 4; // 기본값

        LobbyDetailsInfo lobbyInfo;
        if (lobby.CopyInfo(new LobbyDetailsCopyInfoOptions(), out lobbyInfo) == Epic.OnlineServices.Result.Success)
        {
            // .HasValue, .Value 없이 직접 접근합니다.
            maxMembers = lobbyInfo.MaxMembers;
        }

        // UI 세팅
        if (roomNameText != null) roomNameText.text = roomName;
        if (chapterText != null) chapterText.text = $"Ch.{chapterStr}";
        if (playerCountText != null) playerCountText.text = $"{currentMembers} / {maxMembers}";

        // 꽉 찬 방은 입장 불가
        if (joinButton != null)
        {
            joinButton.interactable = (currentMembers < maxMembers);
        }
    }

    private void OnJoinButtonClicked()
    {
        if (targetLobby != null)
        {
            onJoinCallback?.Invoke(targetLobby);
        }
    }
}