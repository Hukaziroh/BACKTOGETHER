using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EpicTransport;
using System.Collections;

public class ClientLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject clientSelectionPanel;
    [SerializeField] private GameObject clientPublicPanel;
    [SerializeField] private GameObject clientPrivatePanel;

    [Header("선택 패널 버튼 연결 (Client Selection)")]
    [SerializeField] private Button mainClientButton;
    [SerializeField] private Button selectPublicModeButton;
    [SerializeField] private Button selectPrivateModeButton;
    [SerializeField] private Button quickJoinSelectionButton;

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button researchButton;
    [SerializeField] private TextMeshProUGUI filterChapterText;
    [SerializeField] private Button prevFilterChapterButton;
    [SerializeField] private Button nextFilterChapterButton;

    [Header("퍼블릭 방 리스트 - 스크롤 및 로비 아이템")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private GameObject noRoomFoundText;

    [Header("퍼블릭 방 리스트 - 하단 페이지네이션")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("공통 로딩 & 에러 UI")]
    [SerializeField] private GameObject loadingText;
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    private List<LobbyDetails> allFetchedLobbies = new List<LobbyDetails>();
    private List<LobbyDetails> filteredLobbies = new List<LobbyDetails>();

    private int currentPage = 0;
    private const int itemsPerPage = 8;
    private int selectedFilterChapter = 0;
    private bool isQuickJoining = false;
    private EOSLobby GetEOSLobby()
    {
        if (eosLobby == null && NetworkManager.singleton != null)
        {
            eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        }
        return eosLobby;
    }

    private void SubscribeEvents()
    {
        if (isSubscribed) return;

        var lobby = GetEOSLobby();
        if (lobby != null)
        {
            lobby.FindLobbiesSucceeded += OnFindLobbiesSucceeded;
            lobby.FindLobbiesFailed += OnFindLobbiesFailed;
            lobby.JoinLobbySucceeded += OnJoinLobbySucceeded;
            lobby.JoinLobbyFailed += OnJoinLobbyFailed;
            isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (eosLobby != null)
        {
            eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSucceeded;
            eosLobby.FindLobbiesFailed -= OnFindLobbiesFailed;
            eosLobby.JoinLobbySucceeded -= OnJoinLobbySucceeded;
            eosLobby.JoinLobbyFailed -= OnJoinLobbyFailed;
        }
        isSubscribed = false;
    }

    private void OnEnable() { SubscribeEvents(); }
    private void Start()
    {
        SubscribeEvents();
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (loadingText != null) loadingText.SetActive(false);

        // 메인 패널은 켜둠
        if (mainPanel != null) mainPanel.SetActive(true);

        UpdateFilterChapterUI();
    }
    private void OnDisable() { UnsubscribeEvents(); }

    #region Panel Navigation

    public void OnClick_MainClient()
    {
        SubscribeEvents();

        // ★ 메인화면을 끄고 Client 모드 선택창(Public/Private/QuickJoin)을 켭니다.
        if (mainPanel != null) mainPanel.SetActive(false);
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(true);
    }

    public void OnClick_SelectPublicMode()
    {
        SubscribeEvents();
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(true);

        OnClick_Research();
    }

    public void OnClick_SelectPrivateMode()
    {
        SubscribeEvents();
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(true);
    }

    // ★ 닫기 버튼들 처리: 닫을 때 이전 단계 패널을 다시 켜줍니다.
    public void OnClick_CloseSelectionPanel()
    {
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true); // 메인화면 복구
    }

    public void OnClick_ClosePublicPanel()
    {
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(true); // 선택창 복구
    }

    public void OnClick_ClosePrivatePanel()
    {
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(true); // 선택창 복구
    }

    #endregion

    #region Public Room List Logic

    public void OnClick_Research()
    {
        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        SetInteractableAll(false);
        if (loadingText != null) loadingText.SetActive(true);

        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            }
        };

        lobby.FindLobbies(50, searchOptions);
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        SetInteractableAll(true);
        if (loadingText != null) loadingText.SetActive(false);

        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();

        // ★ 빠른 참가 버튼을 눌렀을 때의 동작
        if (isQuickJoining)
        {
            isQuickJoining = false;

            // 검색된 방 중 인원이 꽉 차지 않은 첫 번째 방 찾기
            foreach (var lobby in allFetchedLobbies)
            {
                if (EOSLobby.IsLobbyJoinable(lobby, out uint currentMembers, out uint maxMembers))
                {
                    Debug.Log($"[QuickJoin] 빈 방 발견! ({currentMembers}/{maxMembers}) 즉시 입장합니다.");
                    JoinRoom(lobby);
                    return;
                }
            }

            // 빈 방이 없다면
            ShowError("현재 입장 가능한 퍼블릭 방이 없습니다.");
            return;
        }

        // 일반 검색일 경우 리스트 새로고침
        ApplyFiltersAndRefresh();
    }

    private void OnFindLobbiesFailed(string error)
    {
        isQuickJoining = false; // ★ 플래그 초기화

        SetInteractableAll(true);
        if (loadingText != null) loadingText.SetActive(false);
        ShowError("방 목록을 불러오지 못했습니다: " + error);
    }

    public void OnSearchInputChanged(string input) { ApplyFiltersAndRefresh(); }

    public void OnClick_PrevFilterChapter()
    {
        selectedFilterChapter--;
        if (selectedFilterChapter < 0) selectedFilterChapter = 6;
        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    public void OnClick_NextFilterChapter()
    {
        selectedFilterChapter++;
        if (selectedFilterChapter > 6) selectedFilterChapter = 0;
        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    private void UpdateFilterChapterUI()
    {
        if (filterChapterText != null)
            filterChapterText.text = (selectedFilterChapter == 0) ? "Chapter All" : $"Chapter {selectedFilterChapter}";
    }

    private void ApplyFiltersAndRefresh()
    {
        filteredLobbies.Clear();
        string searchKey = (searchInputField != null) ? searchInputField.text.Trim().ToLower() : "";

        foreach (var lobby in allFetchedLobbies)
        {
            if (lobby == null) continue;

            string roomName = GetLobbyAttribute(lobby, "ROOM_NAME", "");
            if (!string.IsNullOrEmpty(searchKey) && !roomName.ToLower().Contains(searchKey))
                continue;

            if (selectedFilterChapter > 0)
            {
                string chapterStr = GetLobbyAttribute(lobby, "CHAPTER", "1");
                if (int.TryParse(chapterStr, out int ch) && ch != selectedFilterChapter)
                    continue;
            }

            filteredLobbies.Add(lobby);
        }

        currentPage = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (roomListContainer != null)
        {
            foreach (Transform child in roomListContainer) Destroy(child.gameObject);
        }

        if (filteredLobbies.Count == 0)
        {
            if (noRoomFoundText != null) noRoomFoundText.SetActive(true);
            if (pageText != null) pageText.text = "0 / 0";
            if (prevPageButton != null) prevPageButton.interactable = false;
            if (nextPageButton != null) nextPageButton.interactable = false;
            return;
        }

        if (noRoomFoundText != null) noRoomFoundText.SetActive(false);

        int maxPage = Mathf.CeilToInt((float)filteredLobbies.Count / itemsPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, maxPage - 1);

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredLobbies.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var lobby = filteredLobbies[i];
            if (roomListContainer != null && roomItemPrefab != null)
            {
                GameObject itemObj = Instantiate(roomItemPrefab, roomListContainer);
                ClientRoomItemUI itemUI = itemObj.GetComponent<ClientRoomItemUI>();
                if (itemUI != null) itemUI.Setup(lobby, JoinRoom);
            }
        }

        if (pageText != null) pageText.text = $"{currentPage + 1} / {maxPage}";
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 0);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < maxPage - 1);
    }

    public void OnClick_PrevPage()
    {
        if (currentPage > 0) { currentPage--; RefreshUI(); }
    }

    public void OnClick_NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)filteredLobbies.Count / itemsPerPage);
        if (currentPage < maxPage - 1) { currentPage++; RefreshUI(); }
    }

    #endregion

    #region Join Room Logic

    public void JoinRoom(LobbyDetails lobby)
    {
        SubscribeEvents();
        if (lobby == null) return;

        var eos = GetEOSLobby();
        if (eos == null)
        {
            ShowError("네트워크 시스템을 찾을 수 없습니다.");
            return;
        }

        SetInteractableAll(false);
        if (loadingText != null) loadingText.SetActive(true);

        eos.JoinLobby(lobby);
    }

    private void OnJoinLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        if (loadingText != null) loadingText.SetActive(false);

        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedLobbyDetails != null)
        {
            Epic.OnlineServices.Lobby.Attribute attr;
            if (eos.ConnectedLobbyDetails.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = EOSLobby.hostAddressKey }, out attr) == Result.Success)
            {
                string hostAddress = attr.Data?.Value.AsUtf8 ?? "";
                if (!string.IsNullOrEmpty(hostAddress))
                {
                    EosTransport transport = NetworkManager.singleton.transport as EosTransport;
                    if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

                    NetworkManager.singleton.networkAddress = hostAddress;
                    NetworkManager.singleton.StartClient();
                    return;
                }
            }
        }

        SetInteractableAll(true);
        ShowError("방장의 주소 정보를 가져오지 못했습니다.");
    }

    private void OnJoinLobbyFailed(string error)
    {
        SetInteractableAll(true);
        if (loadingText != null) loadingText.SetActive(false);
        ShowError("방 입장에 실패했습니다: " + error);
    }

    #endregion

    private string GetLobbyAttribute(LobbyDetails lobby, string key, string defaultValue)
    {
        Epic.OnlineServices.Lobby.Attribute attr;
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attr) == Result.Success)
        {
            return attr.Data?.Value.AsUtf8 ?? defaultValue;
        }
        return defaultValue;
    }

    private void SetInteractableAll(bool interactable)
    {
        if (selectPublicModeButton != null) selectPublicModeButton.interactable = interactable;
        if (selectPrivateModeButton != null) selectPrivateModeButton.interactable = interactable;
        if (quickJoinSelectionButton != null) quickJoinSelectionButton.interactable = interactable;
        if (researchButton != null) researchButton.interactable = interactable;
        if (prevPageButton != null) prevPageButton.interactable = interactable;
        if (nextPageButton != null) nextPageButton.interactable = interactable;
    }

    private void ShowError(string msg)
    {
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = msg;
            errorPopupPanel.SetActive(true);
        }
    }

    public void OnClick_CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }

    public void OnClick_QuickJoin()
    {
        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        SetInteractableAll(false);
        if (loadingText != null) loadingText.SetActive(true);

        // 1. 퍼블릭 방이면서
        // 2. 남은 자리가 있는 방만 검색하도록 필터를 걸어 에픽 서버에 요청
        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            }
        };

        // 빠른 참가를 위해 내부적으로 검색 중임을 표시하는 플래그
        isQuickJoining = true;
        lobby.FindLobbies(50, searchOptions);
    }
}