using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Linq;
using Epic.OnlineServices;

public class ClientLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;             // 메인 화면
    [SerializeField] private GameObject clientSelectionPanel;  // [Public / Private] 선택 팝업
    [SerializeField] private GameObject clientPublicPanel;     // 퍼블릭 방 리스트 화면
    [SerializeField] private GameObject clientPrivatePanel;    // 프라이빗 코드 입력 화면 (기존 패널)

    [Header("메인 & 패널 버튼 연결")]
    [SerializeField] private Button mainClientButton;          // 메인 화면의 [CLIENT] 버튼
    [SerializeField] private Button selectPublicModeButton;    // 팝업 내 [Public] 버튼
    [SerializeField] private Button selectPrivateModeButton;   // 팝업 내 [Private] 버튼

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;  // 방 이름 검색창
    [SerializeField] private TMP_Dropdown chapterFilterDropdown; // 챕터 정렬/필터 (All, 1, 2, 3, 4, 5, 6)
    [SerializeField] private Button researchButton;            // 리서치(새로고침) 버튼
    [SerializeField] private Button quickJoinButton;           // 빠른 입장 버튼

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

    private void Start()
    {
        // 이벤트 연결
        if (mainClientButton != null) mainClientButton.onClick.AddListener(OnClick_OpenClientSelectionPanel);
        if (selectPublicModeButton != null) selectPublicModeButton.onClick.AddListener(OnClick_OpenPublicPanel);
        if (selectPrivateModeButton != null) selectPrivateModeButton.onClick.AddListener(OnClick_OpenPrivatePanel);

        if (researchButton != null) researchButton.onClick.AddListener(RefreshLobbyList);
        if (quickJoinButton != null) quickJoinButton.onClick.AddListener(OnClick_QuickJoin);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(OnClick_PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(OnClick_NextPage);
        if (joinPrivateButton != null) joinPrivateButton.onClick.AddListener(OnClick_JoinPrivateRoom);

        // 검색/필터 값이 바뀔 때마다 즉시 리스트 필터링
        if (searchInputField != null) searchInputField.onValueChanged.AddListener(delegate { ApplyFilters(); });
        if (chapterFilterDropdown != null) chapterFilterDropdown.onValueChanged.AddListener(delegate { ApplyFilters(); });

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
        RefreshLobbyList(); // 퍼블릭 패널을 열 때 자동으로 방 검색
    }

    public void OnClick_OpenPrivatePanel()
    {
        CloseAllPanels();
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(true);
    }
    #endregion

    #region --- 퍼블릭 방 리스트 및 검색/필터 로직 ---
    public void RefreshLobbyList()
    {
        if (eosLobby == null) return;

        if (loadingText != null) loadingText.SetActive(true);
        ClearRoomListUI();

        // 오직 "PUBLIC" 방만 가져옵니다
        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "ROOM_TYPE", Value = "PUBLIC" } }
        };

        eosLobby.FindLobbies(100, searchOptions);
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        if (loadingText != null) loadingText.SetActive(false);
        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();
        ApplyFilters(); // 데이터가 들어오면 필터 적용 후 화면 업데이트
    }

    private void OnFindLobbiesFailed(string error)
    {
        if (loadingText != null) loadingText.SetActive(false);
        allFetchedLobbies.Clear();
        ApplyFilters();
        ShowError("방 목록을 불러오지 못했습니다.");
    }

    private void ApplyFilters()
    {
        string searchKeyword = searchInputField != null ? searchInputField.text.Trim().ToLower() : "";

        // Dropdown 0번이 "All" 이고, 1번이 "Ch.1" 이라고 가정
        int targetChapter = chapterFilterDropdown != null ? chapterFilterDropdown.value : 0;

        filteredLobbies = allFetchedLobbies.Where(lobby =>
        {
            string roomName = "";
            string chapterStr = "";

            Epic.OnlineServices.Lobby.Attribute attr;
            if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "ROOM_NAME" }, out attr) == Epic.OnlineServices.Result.Success)
                roomName = attr.Data.Value.Value.AsUtf8.ToLower();

            if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER" }, out attr) == Epic.OnlineServices.Result.Success)
                chapterStr = attr.Data.Value.Value.AsUtf8;

            // 1. 방 이름 검색 (포함되어 있는지)
            if (!string.IsNullOrEmpty(searchKeyword) && !roomName.Contains(searchKeyword))
                return false;

            // 2. 챕터 필터 (0이면 전체보기, 아니면 해당 챕터만)
            if (targetChapter != 0 && chapterStr != targetChapter.ToString())
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

    #region --- 입장 기능 (Quick Join / 선택 입장 / 코드 입장) ---

    // 선택된 퍼블릭 방 입장
    private void JoinRoom(LobbyDetails lobby)
    {
        if (eosLobby != null)
        {
            Debug.Log("[Client] 퍼블릭 방 입장을 시도합니다.");
            SetInteractableAll(false);
            eosLobby.JoinLobby(lobby);
        }
    }

    // 빠른 입장 (Quick Join)
    public void OnClick_QuickJoin()
    {
        // 필터링된 방 목록 중에서, 아직 자리가 남은(4명 미만) 첫 번째 방을 찾아 들어감
        var availableRoom = filteredLobbies.FirstOrDefault(lobby =>
        {
            uint currentMembers = lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
            uint maxMembers = 4;
            var infoResult = lobby.GetLobbyDetailsInfo(new LobbyDetailsGetLobbyDetailsInfoOptions(), out var lobbyInfo);
            if (infoResult == Epic.OnlineServices.Result.Success && lobbyInfo.HasValue) maxMembers = lobbyInfo.Value.MaxMembers;
            return currentMembers < maxMembers;
        });

        if (availableRoom != null)
        {
            JoinRoom(availableRoom);
        }
        else
        {
            ShowError("현재 입장 가능한 방이 없습니다.");
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

        // 프라이빗 방이면서, 해당 숏코드와 일치하는 방만 검색
        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "ROOM_TYPE", Value = "PRIVATE" } },
            new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "SHORTCODE", Value = inputCode } }
        };

        eosLobby.FindLobbies(1, searchOptions);
        StartCoroutine(WaitForPrivateJoinRoutine(inputCode));
    }

    private IEnumerator WaitForPrivateJoinRoutine(string inputCode)
    {
        yield return new WaitForSeconds(3f); // 검색 대기

        if (allFetchedLobbies.Count > 0)
        {
            // 찾았으면 첫번째 로비로 접속
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

    private void SetInteractableAll(bool interactable)
    {
        if (quickJoinButton != null) quickJoinButton.interactable = interactable;
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