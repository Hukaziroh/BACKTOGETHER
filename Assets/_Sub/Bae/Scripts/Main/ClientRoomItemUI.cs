using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Epic.OnlineServices.Lobby;

public class ClientRoomItemUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI roomNameText;    // 방 제목
    [SerializeField] private TextMeshProUGUI chapterText;     // 챕터 (예: "Ch.3")
    [SerializeField] private TextMeshProUGUI playerCountText; // 인원수 (예: "2/4")
    [SerializeField] private Button joinButton;              // 입장 버튼

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

        // 3. 인원수 가져오기
        uint currentMembers, maxMembers;
        bool joinable = EOSLobby.IsLobbyJoinable(lobby, out currentMembers, out maxMembers);

        // ⭐ [추가 1] 인원이 0명이면 에픽 백엔드 삭제 지연 중인 유령방이므로 입장 불가 처리!
        if (currentMembers == 0)
        {
            joinable = false;
        }

        // UI 세팅
        if (roomNameText != null) roomNameText.text = roomName;
        if (chapterText != null) chapterText.text = $"Ch.{chapterStr}";
        if (playerCountText != null) playerCountText.text = $"{currentMembers} / {maxMembers}";

        // 꽉 찼거나 0명인 유령방은 버튼 비활성화
        if (joinButton != null)
        {
            joinButton.interactable = joinable;
        }
    }

    private void OnJoinButtonClicked()
    {
        Debug.Log("[UI] 방 입장 버튼 클릭됨!");
        if (targetLobby != null)
        {
            // ⭐ [추가 2] 클릭하는 찰나에 0명이 되었는지 2중 방어 검사
            uint currentMembers = targetLobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
            if (currentMembers == 0)
            {
                Debug.LogWarning("[UI] 이미 파괴 진행 중인 유령방입니다. 입장을 차단합니다.");
                if (joinButton != null) joinButton.interactable = false;
                return;
            }

            onJoinCallback?.Invoke(targetLobby);
        }
        else
        {
            Debug.LogError("[UI] targetLobby가 null입니다!");
        }
    }
}