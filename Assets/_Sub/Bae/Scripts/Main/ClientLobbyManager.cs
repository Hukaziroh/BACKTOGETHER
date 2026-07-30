using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EpicTransport;

public class ClientLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;             // 메인 화면
    [SerializeField] private GameObject clientSelectionPanel;  // [Public / Private / Quick Join] 선택 팝업
    [SerializeField] private GameObject clientPublicPanel;     // 퍼블릭 방 리스트 화면
    [SerializeField] private GameObject clientPrivatePanel;    // 프라이빗 코드 입력 화면

    [Header("선택 패널 버튼 연결 (Client Selection)")]
    [SerializeField] private Button mainClientButton;          // 메인 화면의 [CLIENT] 버튼
    [SerializeField] private Button selectPublicModeButton;    // 팝업 내 [Public (방 리스트)] 버튼
    [SerializeField] private Button selectPrivateModeButton;   // 팝업 내 [Private (코드 입력)] 버튼
    [SerializeField] private Button quickJoinSelectionButton;  // 팝업 내 [Quick Join (빠른 입장)] 버튼

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;  // 방 이름 검색창
    [SerializeField] private Button researchButton;            // 리서치(새로고침) 버튼
    [SerializeField] private TextMeshProUGUI filterChapterText;// 챕터 정렬 텍스트 (< Chapter All >)
    [SerializeField] private Button prevFilterChapterButton;   // 챕터 정렬 <
    [SerializeField] private Button nextFilterChapterButton;   // 챕터 정렬 >

    [Header("퍼블릭 방 리스트 - 스크롤 및 로비 아이템")]
    [SerializeField] private Transform roomListContainer;      // 방 목록 아이템들이 배치될 컨테이너
    [SerializeField] private GameObject roomItemPrefab;        // 방 목록 아이템 프리팹 (ClientRoomItemUI)
    [SerializeField] private GameObject noRoomFoundText;       // "방이 없습니다" 안내 텍스트

    [Header("퍼블릭 방 리스트 - 하단 페이지네이션")]
    [SerializeField] private Button prevPageButton;            // 이전 페이지 <
    [SerializeField] private Button nextPageButton;            // 다음 페이지 >
    [SerializeField] private TextMeshProUGUI pageText;         // 페이지 표시 (예: "1 / 3")

    [Header("프라이빗 코드 입력 UI")]
    [SerializeField] private SixDigitCodeInputUI codeInputUI;  // 6자리 코드 입력 스크립트
    [SerializeField] private Button joinPrivateButton;         // [JOIN] 버튼

    [Header("공통 로딩 & 에러 UI")]
    [SerializeField] private GameObject loadingText;           // 로딩 중 UI
    [SerializeField] private GameObject errorPopupPanel;       // 에러 팝업
    [SerializeField] private TextMeshProUGUI errorMessageText;  // 에러 메시지 텍스트

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    private List<LobbyDetails> allFetchedLobbies = new List<LobbyDetails>();
    private List<LobbyDetails> filteredLobbies = new List<LobbyDetails>();

    private int currentPage = 0;
    private const int itemsPerPage = 8;
    private int selectedFilterChapter = 0; // 0 = All

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

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        SubscribeEvents();

        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (loadingText != null) loadingText.SetActive(false);

        UpdateFilterChapterUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    #region Panel Navigation

    public void OnClick_MainClient()
    {
        SubscribeEvents();
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

    public void OnClick_CloseSelectionPanel()
    {
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
    }

    public void OnClick_ClosePublicPanel()
    {
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
    }

    public void OnClick_ClosePrivatePanel()
    {
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
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

        lobby.FindLobbies(50, null);
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        SetInteractableAll(true);
        if (loadingText != null) loadingText.SetActive(false);

        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();
        ApplyFiltersAndRefresh();
    }

    private void OnFindLobbiesFailed(string error)
    {
        SetInteractableAll(true);
        if (loadingText != null) loadingText.SetActive(false);

        ShowError("방 목록을 불러오지 못했습니다: " + error);
    }

    public void OnSearchInputChanged(string input)
    {
        ApplyFiltersAndRefresh();
    }

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
        {
            filterChapterText.text = (selectedFilterChapter == 0) ? "Chapter All" : $"Chapter {selectedFilterChapter}";
        }
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
            {
                continue;
            }

            if (selectedFilterChapter > 0)
            {
                string chapterStr = GetLobbyAttribute(lobby, "CHAPTER", "1");
                if (int.TryParse(chapterStr, out int ch) && ch != selectedFilterChapter)
                {
                    continue;
                }
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
            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }
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
                if (itemUI != null)
                {
                    itemUI.Setup(lobby, JoinRoom);
                }
            }
        }

        if (pageText != null) pageText.text = $"{currentPage + 1} / {maxPage}";
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 0);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < maxPage - 1);
    }

    public void OnClick_PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshUI();
        }
    }

    public void OnClick_NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)filteredLobbies.Count / itemsPerPage);
        if (currentPage < maxPage - 1)
        {
            currentPage++;
            RefreshUI();
        }
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

    private void OnJoinLobbySucceeded(List<Attribute> attributes) 
    {
        if (loadingText != null) loadingText.SetActive(false);

        var eos = GetEOSLobby();
        // ★ 에픽 서버에서 연결된 로비의 상세 정보를 안전하게 가져옴
        if (eos != null && eos.ConnectedLobbyDetails != null)
        {
            Attribute attr;
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

    #region Private Room Logic

    public void OnClick_JoinPrivateRoom()
    {
        SubscribeEvents();

        if (codeInputUI == null) return;

        string inputCode = codeInputUI.GetCode();
        if (string.IsNullOrEmpty(inputCode) || inputCode.Length < 6)
        {
            ShowError("6자리 코드를 정확히 입력해주세요.");
            return;
        }

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
                Parameter = new AttributeData { Key = "SHORTCODE", Value = inputCode }
            }
        };

        lobby.FindLobbies(1, searchOptions);
        StartCoroutine(WaitForPrivateJoinRoutine(inputCode));
    }

    private IEnumerator WaitForPrivateJoinRoutine(string inputCode)
    {
        yield return new WaitForSeconds(3f);

        if (allFetchedLobbies != null && allFetchedLobbies.Count > 0)
        {
            JoinRoom(allFetchedLobbies[0]);
        }
        else
        {
            SetInteractableAll(true);
            if (loadingText != null) loadingText.SetActive(false);
            ShowError("해당 코드를 가진 방을 찾을 수 없습니다.");
        }
    }

    #endregion

    #region Helpers

    private string GetLobbyAttribute(LobbyDetails lobby, string key, string defaultValue)
    {
        Attribute attr;
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
        if (joinPrivateButton != null) joinPrivateButton.interactable = interactable;
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

    #endregion
}