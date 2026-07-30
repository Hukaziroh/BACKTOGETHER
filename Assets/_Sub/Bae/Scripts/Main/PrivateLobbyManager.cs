using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject hostPanel;              // [HOST] 클릭 시 뜨는 설정 패널 (사진 속 패널)

    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;           // 메인 화면의 [HOST] 버튼
    [SerializeField] private Button makeRoomButton;            // 패널 안의 [MAKE ROOM] 버튼

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField; // 방 이름 입력창 (ROOM TITLE)

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private TextMeshProUGUI chapterDisplayText; // 선택된 챕터 글자
    [SerializeField] private Button prevChapterButton;          // 챕터 왼쪽 화살표 <
    [SerializeField] private Button nextChapterButton;          // 챕터 오른쪽 화살표 >

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText; // 선택된 타입 글자 ("Public" 또는 "Private")
    [SerializeField] private Button prevRoomTypeButton;         // 타입 왼쪽 화살표 <
    [SerializeField] private Button nextRoomTypeButton;         // 타입 오른쪽 화살표 >

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorConfirmButton;

    [Header("씬 이동 설정")]
    [SerializeField] private string mainSceneName = "MainScene";

    // 전역 상태 변수
    public static string currentShortCode = "";
    public static string currentRoomName = "";
    public static int currentChapter = 1;
    public static bool isCurrentLobbyPublic = true;

    // 내부 제어 변수
    private int selectedChapterIndex = 1;
    private const int MAX_CHAPTER = 6;
    private bool isPublicSelected = true; // true = Public, false = Private

    // EOSLobby 이벤트 구독용 (방 생성 실패 / 속성 일괄 업데이트 결과를 폴링 대신 이벤트로 받기 위함)
    private EOSLobby eosLobby;
    private bool lobbyCreationFailed = false;
    private string lobbyCreationErrorMessage = "";
    private bool attributeUpdateDone = false;
    private bool attributeUpdateFailed = false;

    private void OnEnable()
    {
        if (eosLobby == null && NetworkManager.singleton != null)
            eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby != null)
        {
            eosLobby.CreateLobbyFailed += OnCreateLobbyFailed;
            eosLobby.LobbyAttributesUpdateSucceeded += OnLobbyAttributesUpdateSucceeded;
            eosLobby.LobbyAttributesUpdateFailed += OnLobbyAttributesUpdateFailed;
        }
    }

    private void OnDisable()
    {
        if (eosLobby != null)
        {
            eosLobby.CreateLobbyFailed -= OnCreateLobbyFailed;
            eosLobby.LobbyAttributesUpdateSucceeded -= OnLobbyAttributesUpdateSucceeded;
            eosLobby.LobbyAttributesUpdateFailed -= OnLobbyAttributesUpdateFailed;
        }
    }

    private void OnCreateLobbyFailed(string errorMessage)
    {
        // 폴링 타임아웃(5초)을 다 기다리지 않고 실패를 즉시 감지하기 위한 콜백
        lobbyCreationFailed = true;
        lobbyCreationErrorMessage = errorMessage;
    }

    private void OnLobbyAttributesUpdateSucceeded()
    {
        attributeUpdateDone = true;
        attributeUpdateFailed = false;
    }

    private void OnLobbyAttributesUpdateFailed(string errorMessage)
    {
        attributeUpdateDone = true;
        attributeUpdateFailed = true;
        Debug.LogError("[PrivateLobbyManager] 로비 속성 업데이트 실패: " + errorMessage);
    }

    private void Start()
    {
        SetAllButtonsInteractable(false);

        // 시작 시 설정 패널 비활성화
        if (hostPanel != null) hostPanel.SetActive(false);

        // 로딩 패널 자동 확보 및 비활성화
        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        // 에러 팝업 초기화
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (errorConfirmButton != null)
        {
            errorConfirmButton.onClick.AddListener(OnClick_ReturnToMain);
        }

        // 버튼 리스너 자동 연결
        if (mainHostButton != null) mainHostButton.onClick.AddListener(OnClick_OpenHostPanel);
        if (makeRoomButton != null) makeRoomButton.onClick.AddListener(OnClick_MakeRoom);

        // 챕터 화살표 버튼 리스너 연결
        if (prevChapterButton != null) prevChapterButton.onClick.AddListener(OnClick_PrevChapter);
        if (nextChapterButton != null) nextChapterButton.onClick.AddListener(OnClick_NextChapter);

        // 방 타입 화살표 버튼 리스너 연결
        if (prevRoomTypeButton != null) prevRoomTypeButton.onClick.AddListener(ToggleRoomType);
        if (nextRoomTypeButton != null) nextRoomTypeButton.onClick.AddListener(ToggleRoomType);

        // UI 텍스트 초기화
        UpdateChapterUI();
        UpdateRoomTypeUI();

        StartCoroutine(WaitForEpicLoginRoutine());
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1. 에러 팝업 엔터키 처리
        if (errorPopupPanel != null && errorPopupPanel.activeSelf)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                OnClick_ReturnToMain();
            }
            return;
        }

        // 2. 키보드 방향키/AD 조작 (방 이름 타이핑 중이 아니고, 설정 패널이 켜져 있을 때)
        bool isTyping = roomNameInputField != null && roomNameInputField.isFocused;
        bool isHostPanelActive = hostPanel != null && hostPanel.activeSelf;

        if (!isTyping && isHostPanelActive)
        {
            // A/D 또는 좌우 화살표로 챕터 변경
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                OnClick_PrevChapter();
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                OnClick_NextChapter();
            }

            // W/S 또는 위아래 화살표로 Public <-> Private 전환
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                ToggleRoomType();
            }
        }
    }

    #region --- UI 패널 및 토글 제어 ---

    /// <summary>
    /// [HOST] 버튼 클릭 시 설정 패널 열기
    /// </summary>
    public void OnClick_OpenHostPanel()
    {
        if (hostPanel != null) hostPanel.SetActive(true);
    }

    /// <summary>
    /// 패널 닫기 (뒤로가기 버튼 등에 연결)
    /// </summary>
    public void OnClick_CloseHostPanel()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
    }

    /// <summary>
    /// Room Type 토글 (Public <-> Private)
    /// </summary>
    public void ToggleRoomType()
    {
        isPublicSelected = !isPublicSelected;
        UpdateRoomTypeUI();
    }

    private void UpdateRoomTypeUI()
    {
        if (roomTypeDisplayText != null)
        {
            roomTypeDisplayText.text = isPublicSelected ? "Public" : "Private";
        }
    }

    #endregion

    #region --- 챕터 선택 (좌우 넘기기) ---

    public void OnClick_PrevChapter()
    {
        if (selectedChapterIndex > 1)
        {
            selectedChapterIndex--;
            UpdateChapterUI();
        }
    }

    public void OnClick_NextChapter()
    {
        if (selectedChapterIndex < MAX_CHAPTER)
        {
            selectedChapterIndex++;
            UpdateChapterUI();
        }
    }

    private void UpdateChapterUI()
    {
        if (chapterDisplayText != null)
        {
            chapterDisplayText.text = $"Chapter {selectedChapterIndex}";
        }

        if (prevChapterButton != null) prevChapterButton.interactable = (selectedChapterIndex > 1);
        if (nextChapterButton != null) nextChapterButton.interactable = (selectedChapterIndex < MAX_CHAPTER);
    }

    #endregion

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (mainHostButton != null) mainHostButton.interactable = interactable;
        if (makeRoomButton != null) makeRoomButton.interactable = interactable;
    }

    private GameObject GetLoadingPanel()
    {
        if (loadingPanel == null)
        {
            WalkingLoadingPanel[] allPanels = Resources.FindObjectsOfTypeAll<WalkingLoadingPanel>();

            foreach (var panel in allPanels)
            {
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    loadingPanel = panel.gameObject;
                    break;
                }
            }
        }
        return loadingPanel;
    }

    private IEnumerator WaitForEpicLoginRoutine()
    {
        float timeout = 5f;
        bool isLoggedIn = false;

        while (!isLoggedIn && timeout > 0f)
        {
            try
            {
                if (!string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
                {
                    isLoggedIn = true;
                }
            }
            catch { }

            timeout -= Time.deltaTime;
            yield return null;
        }

        SetAllButtonsInteractable(true);
    }

    #region --- 방 생성 (MAKE ROOM) 실행 ---

    /// <summary>
    /// [MAKE ROOM] 버튼 클릭 시 실행
    /// </summary>
    public void OnClick_MakeRoom()
    {
        string roomTitle = "즐거운 게임 방";
        if (roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text))
        {
            roomTitle = roomNameInputField.text.Trim();
        }

        SetAllButtonsInteractable(false);
        StartCoroutine(CleanAndCreateLobbyRoutine(isPublic: isPublicSelected, roomName: roomTitle, chapter: selectedChapterIndex));
    }

    #endregion

    private IEnumerator CleanAndCreateLobbyRoutine(bool isPublic, string roomName, int chapter)
    {
        isCurrentLobbyPublic = isPublic;

        GameObject currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.05f);
        }

        if (NetworkServer.active) NetworkManager.singleton.StopHost();
        else if (NetworkClient.active) NetworkManager.singleton.StopClient();

        if (eosLobby == null)
        {
            eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null)
            {
                // OnEnable 시점보다 늦게 참조를 얻었을 수 있으므로 여기서도 구독을 보장합니다.
                eosLobby.CreateLobbyFailed -= OnCreateLobbyFailed;
                eosLobby.CreateLobbyFailed += OnCreateLobbyFailed;
                eosLobby.LobbyAttributesUpdateSucceeded -= OnLobbyAttributesUpdateSucceeded;
                eosLobby.LobbyAttributesUpdateSucceeded += OnLobbyAttributesUpdateSucceeded;
                eosLobby.LobbyAttributesUpdateFailed -= OnLobbyAttributesUpdateFailed;
                eosLobby.LobbyAttributesUpdateFailed += OnLobbyAttributesUpdateFailed;
            }
        }

        if (eosLobby == null)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("네트워크 시스템 오류가 발생했습니다.");
            yield break;
        }

        if (eosLobby.ConnectedToLobby)
        {
            eosLobby.LeaveLobby();
            float leaveTimeout = 5f;
            while (eosLobby.ConnectedToLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        lobbyCreationFailed = false;
        lobbyCreationErrorMessage = "";

        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;
        eosLobby.CreateLobby(maxPlayers, permissionLevel, true);

        float timeout = 5f;
        while (string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()) && timeout > 0f && !lobbyCreationFailed)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (lobbyCreationFailed)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup(string.IsNullOrEmpty(lobbyCreationErrorMessage) ? "로비 생성에 실패했습니다." : lobbyCreationErrorMessage);
            yield break;
        }

        if (timeout <= 0f)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("로비 생성 시간이 초과되었습니다.");
            yield break;
        }

        // EOS Attribute 등록
        // 속성마다 UpdateLobbyAttribute를 따로 호출하면 각각 별개의 비동기 요청이 되어
        // 순서를 보장할 수 없고 일부만 반영될 수 있으므로, 한 번의 UpdateLobbyAttributes 호출로 묶어서 보냅니다.
        List<AttributeData> attributesToSet = new List<AttributeData>();

        if (isPublic)
        {
            currentRoomName = roomName;
            currentChapter = chapter;
            currentShortCode = "";

            attributesToSet.Add(new AttributeData { Key = "ROOM_TYPE", Value = "PUBLIC" });
            attributesToSet.Add(new AttributeData { Key = "ROOM_NAME", Value = currentRoomName });
            attributesToSet.Add(new AttributeData { Key = "CHAPTER", Value = currentChapter.ToString() });
        }
        else
        {
            currentShortCode = GenerateShortCode();
            currentRoomName = "";
            currentChapter = 1;

            attributesToSet.Add(new AttributeData { Key = "ROOM_TYPE", Value = "PRIVATE" });
            attributesToSet.Add(new AttributeData { Key = "SHORTCODE", Value = currentShortCode });
        }

        attributeUpdateDone = false;
        attributeUpdateFailed = false;
        eosLobby.UpdateLobbyAttributes(attributesToSet.ToArray());

        float attributeTimeout = 5f;
        while (!attributeUpdateDone && attributeTimeout > 0f)
        {
            attributeTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (attributeUpdateFailed || attributeTimeout <= 0f)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("방 정보를 설정하지 못했습니다. 다시 시도해주세요.");
            yield break;
        }

        EosTransport transport = NetworkManager.singleton.transport as EosTransport;
        if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

        NetworkManager.singleton.StartHost();

        yield return new WaitForSecondsRealtime(0.2f);

        SetAllButtonsInteractable(true);
        if (currentPanel != null) currentPanel.SetActive(false);
    }

    private string GenerateShortCode()
    {
        string allowedChars = "0123456789";
        string code = "";
        for (int i = 0; i < 6; i++) code += allowedChars[Random.Range(0, allowedChars.Length)];
        return code;
    }

    private void ShowErrorPopup(string message)
    {
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = message;
            errorPopupPanel.SetActive(true);
        }
    }

    public void OnClick_ReturnToMain()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (!string.IsNullOrEmpty(mainSceneName)) SceneManager.LoadScene(mainSceneName);
    }
}