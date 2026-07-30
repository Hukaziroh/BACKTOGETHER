using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;             // 메인 화면
    [SerializeField] private GameObject clientSelectionPanel;  // [Public / Private / Quick Join] 선택 팝업
    [SerializeField] private GameObject clientPublicPanel;     // 퍼블릭 방 리스트 화면
    [SerializeField] private GameObject clientPrivatePanel;    // 프라이빗 코드 입력 화면 (기존 패널)

    [Header("선택 패널 버튼 연결 (Client Selection)")]
    [SerializeField] private Button mainClientButton;          // 메인 화면의 [CLIENT] 버튼
    [SerializeField] private Button selectPublicModeButton;    // 팝업 내 [Public (방 리스트)] 버튼
    [SerializeField] private Button selectPrivateModeButton;   // 팝업 내 [Private (코드 입력)] 버튼
    [SerializeField] private Button quickJoinSelectionButton;  // 팝업 내 [Quick Join (빠른 입장)] 버튼

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;  // 방 이름 검색창
    [SerializeField] private Button researchButton;            // 리서치(새로고침) 버튼

    [Header("퍼블릭 방 리스트 - 챕터 필터 (좌우 조작)")]
    [SerializeField] private TextMeshProUGUI chapterFilterText;  // 필터 텍스트 (ALL, Chapter 1...)
    [SerializeField] private Button prevChapterFilterButton;     // 왼쪽 화살표 <
    [SerializeField] private Button nextChapterFilterButton;     // 오른쪽 화살표 >

    [Header("퍼블릭 방 리스트 - 화면 및 페이징")]
    [SerializeField] private Transform roomListParent;         // 8개 방 프리팹이 생성될 부모 Grid
    [SerializeField] private GameObject clientRoomItemPrefab;  // 방 아이템 프리팹 (ClientRoomItemUI)
    [SerializeField] private Button prevPageButton;            // < 이전 페이지
    [SerializeField] private Button nextPageButton;            // > 다음 페이지
    [SerializeField] private TextMeshProUGUI pageText;         // 페이지 텍스트 (예: 1 / 3)
    [SerializeField] private GameObject loadingText;           // "검색 중..." 텍스트/이미지

    [Header("프라이빗 패널 설정")]
    [SerializeField] private TMP_InputField shortCodeInputField; // 프라이빗 룸코드 입력창
    [SerializeField] private Button joinPrivateButton;           // 코드 입력 후 입장 버튼

    [Header("에러 팝업")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    private EOSLobby eosLobby;
    private List<LobbyDetails> allFetchedLobbies = new List<LobbyDetails>(); // 서버에서 가져온 전체 방
    private List<LobbyDetails> filteredLobbies = new List<LobbyDetails>();   // 조건에 맞게 걸러진 방

    private const int ROOMS_PER_PAGE = 8;
    private int currentPage = 0;

    // 내부 제어 변수
    private int currentChapterFilter = 0; // 0 = ALL, 1~6 = Chapter 1~6
    private bool isQuickJoining = false;   // Quick Join을 눌러서 검색 중인지 여부
    private bool isPrivateJoining = false; // 프라이빗 코드로 검색 중인지 여부 (고정 대기 대신 이벤트로 결과를 받기 위한 플래그)

    private void Start()
    {
        // 이벤트 연결
        if (mainClientButton != null) mainClientButton.onClick.AddListener(OnClick_OpenClientSelectionPanel);

        // 선택 패널 3가지 버튼 연결
        if (selectPublicModeButton != null) selectPublicModeButton.onClick.AddListener(OnClick_OpenPublicPanel);
        if (selectPrivateModeButton != null) selectPrivateModeButton.onClick.AddListener(OnClick_OpenPrivatePanel);
        if (quickJoinSelectionButton != null) quickJoinSelectionButton.onClick.AddListener(OnClick_QuickJoinSelection);

        // 퍼블릭 컨트롤 버튼 연결
        if (researchButton != null) researchButton.onClick.AddListener(OnClick_Research);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(OnClick_PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(OnClick_NextPage);

        // 챕터 필터 좌우 버튼 연결
        if (prevChapterFilterButton != null) prevChapterFilterButton.onClick.AddListener(OnClick_PrevChapterFilter);
        if (nextChapterFilterButton != null) nextChapterFilterButton.onClick.AddListener(OnClick_NextChapterFilter);

        // 프라이빗 입장 버튼
        if (joinPrivateButton != null) joinPrivateButton.onClick.AddListener(OnClick_JoinPrivateRoom);

        // 검색어 입력 시 즉시 필터링
        if (searchInputField != null) searchInputField.onValueChanged.AddListener(delegate { ApplyFilters(); });

        UpdateChapterFilterUI();
        CloseAllPanels();
    }

    private void OnEnable()
    {
        if (NetworkManager.singleton != null)
            eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby != null)
        {
            eosLobby.FindLobbiesSucceeded += OnFindLobbiesSucceeded;
            eosLobby.FindLobbiesFailed += OnFindLobbiesFailed;
        }
    }

    private void OnDisable()
    {
        if (eosLobby != null)
        {
            eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSucceeded;
            eosLobby.FindLobbiesFailed -= OnFindLobbiesFailed;
        }
    }

    #region --- 패널 전환 제어 ---
    public void CloseAllPanels()
    {
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
    }

    public void OnClick_OpenClientSelectionPanel()
    {
        CloseAllPanels();
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(true);
    }

    public void OnClick_OpenPublicPanel()
    {
        CloseAllPanels();
        if (clientPublicPanel != null) clientPublicPanel.SetActive(true);
        isQuickJoining = false;
        RefreshLobbyList(); // 퍼블릭 패널을 열 때 자동으로 방 검색
    }

    public void OnClick_OpenPrivatePanel()
    {
        CloseAllPanels();
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(true);
    }
    #endregion

    #region --- 로비 검색 및 퀵 조인(Quick Join) 로직 ---

    public void OnClick_Research()
    {
        isQuickJoining = false;
        RefreshLobbyList();
    }

    public void OnClick_QuickJoinSelection()
    {
        isQuickJoining = true;
        SetInteractableAll(false);
        RefreshLobbyList(); // 백그라운드에서 방 리스트 검색 시작
    }

    public void RefreshLobbyList()
    {
        if (eosLobby == null) return;

        if (loadingText != null) loadingText.SetActive(true);
        if (!isQuickJoining) ClearRoomListUI();

        // "PUBLIC" 속성인 방만 서버에 검색 요청
        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "ROOM_TYPE", Value = "PUBLIC" } }
        };

        eosLobby.FindLobbies(100, searchOptions);
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        if (loadingText != null) loadingText.SetActive(false);

        // EOSLobby가 내부적으로 들고 있는 리스트를 그대로 참조하지 않도록 방어적으로 복사합니다.
        // (다음 검색 때 EOSLobby 쪽에서 이 핸들들을 Release/Clear 하기 때문에, 같은 리스트를 공유하면
        //  타이밍에 따라 예기치 않게 비워질 수 있습니다.)
        allFetchedLobbies = lobbies != null ? new List<LobbyDetails>(lobbies) : new List<LobbyDetails>();

        if (isPrivateJoining)
        {
            isPrivateJoining = false;

            if (allFetchedLobbies.Count > 0)
            {
                JoinRoom(allFetchedLobbies[0]);
            }
            else
            {
                SetInteractableAll(true);
                ShowError("해당 코드를 가진 방을 찾을 수 없습니다.");
            }
            return;
        }

        if (isQuickJoining)
        {
            ProcessQuickJoin();
        }
        else
        {
            ApplyFilters(); // 일반 검색이면 리스트 화면 업데이트
        }
    }

    private void OnFindLobbiesFailed(string error)
    {
        if (loadingText != null) loadingText.SetActive(false);
        allFetchedLobbies.Clear();
        SetInteractableAll(true);

        if (isQuickJoining)
        {
            isQuickJoining = false;
            ShowError("빠른 입장을 위한 방을 찾을 수 없습니다.");
        }
        else if (isPrivateJoining)
        {
            isPrivateJoining = false;
            ShowError("해당 코드를 가진 방을 찾을 수 없습니다.");
        }
        else
        {
            ApplyFilters();
            ShowError("방 목록을 불러오지 못했습니다.");
        }
    }

    private void ProcessQuickJoin()
    {
        isQuickJoining = false; // 플래그 리셋

        // 인원이 꽉 차지 않은 방만 걸러냄
        var availableRooms = allFetchedLobbies.Where(lobby =>
        {
            uint currentMembers, maxMembers;
            return EOSLobby.IsLobbyJoinable(lobby, out currentMembers, out maxMembers);
        }).ToList();

        if (availableRooms.Count > 0)
        {
            // 빈 방들 중에서 완전히 무작위(Random)로 하나 선택
            int randomIndex = Random.Range(0, availableRooms.Count);
            JoinRoom(availableRooms[randomIndex]);
        }
        else
        {
            SetInteractableAll(true);
            ShowError("현재 입장 가능한 퍼블릭 방이 없습니다.");
        }
    }

    #endregion

    #region --- 챕터 필터 (좌우 버튼 조작) ---

    public void OnClick_PrevChapterFilter()
    {
        if (currentChapterFilter > 0)
        {
            currentChapterFilter--;
            UpdateChapterFilterUI();
            ApplyFilters(); // 값 변경 시 즉시 필터 적용
        }
    }

    public void OnClick_NextChapterFilter()
    {
        if (currentChapterFilter < 6)
        {
            currentChapterFilter++;
            UpdateChapterFilterUI();
            ApplyFilters();
        }
    }

    private void UpdateChapterFilterUI()
    {
        if (chapterFilterText != null)
        {
            chapterFilterText.text = currentChapterFilter == 0 ? "ALL" : $"Chapter {currentChapterFilter}";
        }

        if (prevChapterFilterButton != null) prevChapterFilterButton.interactable = (currentChapterFilter > 0);
        if (nextChapterFilterButton != null) nextChapterFilterButton.interactable = (currentChapterFilter < 6);
    }

    #endregion

    #region --- 퍼블릭 방 리스트 출력 (필터 및 페이징) ---

    private void ApplyFilters()
    {
        string searchKeyword = searchInputField != null ? searchInputField.text.Trim().ToLower() : "";

        filteredLobbies = allFetchedLobbies.Where(lobby =>
        {
            string roomName = "";
            string chapterStr = "";

            Attribute attr;
            if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "ROOM_NAME" }, out attr) == Epic.OnlineServices.Result.Success)
            {
                if (attr.Data != null) roomName = attr.Data.Value.AsUtf8.ToLower();
            }

            if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER" }, out attr) == Epic.OnlineServices.Result.Success)
            {
                if (attr.Data != null) chapterStr = attr.Data.Value.AsUtf8;
            }

            // 1. 방 이름 검색 (포함되어 있는지)
            if (!string.IsNullOrEmpty(searchKeyword) && !roomName.Contains(searchKeyword))
                return false;

            // 2. 챕터 필터 (0이면 전체보기, 아니면 해당 챕터만)
            if (currentChapterFilter != 0 && chapterStr != currentChapterFilter.ToString())
                return false;

            return true; // 조건 통과
        }).ToList();

        currentPage = 0;
        UpdatePageUI();
    }

    private void UpdatePageUI()
    {
        ClearRoomListUI();

        int totalLobbies = filteredLobbies.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalLobbies / ROOMS_PER_PAGE));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        int startIndex = currentPage * ROOMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ROOMS_PER_PAGE, totalLobbies);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject itemObj = Instantiate(clientRoomItemPrefab, roomListParent);
            ClientRoomItemUI itemScript = itemObj.GetComponent<ClientRoomItemUI>();
            if (itemScript != null)
            {
                itemScript.Setup(filteredLobbies[i], JoinRoom);
            }
        }

        if (pageText != null) pageText.text = $"{currentPage + 1} / {totalPages}";
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 0);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < totalPages - 1);
    }

    private void ClearRoomListUI()
    {
        if (roomListParent == null) return;
        foreach (Transform child in roomListParent) Destroy(child.gameObject);
    }

    private void OnClick_PrevPage()
    {
        if (currentPage > 0) { currentPage--; UpdatePageUI(); }
    }

    private void OnClick_NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)filteredLobbies.Count / ROOMS_PER_PAGE);
        if (currentPage < totalPages - 1) { currentPage++; UpdatePageUI(); }
    }

    #endregion

    #region --- 방 입장 (선택 입장 / 프라이빗 코드 입장) ---

    // 선택된 퍼블릭 방 입장
    private void JoinRoom(LobbyDetails lobby)
    {
        if (eosLobby != null)
        {
            Debug.Log("[Client] 선택된 방에 입장을 시도합니다.");
            SetInteractableAll(false);
            eosLobby.JoinLobby(lobby);
        }
    }

    // 기존 프라이빗 방 코드 입력 입장
    public void OnClick_JoinPrivateRoom()
    {
        string inputCode = shortCodeInputField != null ? shortCodeInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(inputCode) || inputCode.Length != 6)
        {
            ShowError("6자리 코드를 정확히 입력하세요.");
            return;
        }

        if (loadingText != null) loadingText.SetActive(true);
        SetInteractableAll(false);

        isPrivateJoining = true;

        // 프라이빗 방이면서, 해당 숏코드와 일치하는 방만 검색
        // 결과는 고정 시간을 기다리는 대신 FindLobbiesSucceeded/FindLobbiesFailed 이벤트로 처리합니다
        // (OnFindLobbiesSucceeded / OnFindLobbiesFailed 참고).
        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "ROOM_TYPE", Value = "PRIVATE" } },
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "SHORTCODE", Value = inputCode } }
        };

        eosLobby.FindLobbies(1, searchOptions);
    }

    #endregion

    private void SetInteractableAll(bool interactable)
    {
        if (selectPublicModeButton != null) selectPublicModeButton.interactable = interactable;
        if (selectPrivateModeButton != null) selectPrivateModeButton.interactable = interactable;
        if (quickJoinSelectionButton != null) quickJoinSelectionButton.interactable = interactable;
        if (researchButton != null) researchButton.interactable = interactable;
        if (joinPrivateButton != null) joinPrivateButton.interactable = interactable;
    }

    private void ShowError(string msg)
    {
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = msg;
            errorPopupPanel.SetActive(true);
        }
    }
}