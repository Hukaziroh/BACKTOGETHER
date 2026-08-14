using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject logo;

    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;
    [SerializeField] private Button makeRoomButton;

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField;

    [Header("기본 방 이름 목록 (미입력 시 랜덤 선택)")]
    [SerializeField]
    private string[] defaultRoomNames = new string[]
    {
        "Better Together!",
        "Don't Step on My Head",
        "Teamwork Makes the Dream Work",
        "Back Together: Assemble!",
        "Chaos Incoming...",
        "One Mind, Four Bodies"
    };

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private GameObject chapterSelectObject;
    [SerializeField] private TextMeshProUGUI chapterDisplayText;
    [SerializeField] private Button prevChapterButton;
    [SerializeField] private Button nextChapterButton;
    [SerializeField] private Image chapterPreviewImage;
    [SerializeField] private Sprite[] chapterSprites;

    [Header("챕터 잠금 UI")]
    [SerializeField] private GameObject chapterLockObject;

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private GameObject roomTypeSelectObject;
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText;
    [SerializeField] private Button prevRoomTypeButton;
    [SerializeField] private Button nextRoomTypeButton;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorCloseButton;

    [Header("씬 설정")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainSceneName = "Main";

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    public static string currentShortCode = "";
    public static string lastCreatedRoomName = "";
    public static int selectedChapter = 1;
    private int selectedChapterIndex = 1;
    private int maxChapterCount = 6;
    private bool isPublicRoom = true;

    private bool isCreatingLobby = false;
    private bool attributeUpdateDone = false;
    private bool attributeUpdateFailed = false;
    private bool createLobbySuccess = false;

    private GameObject currentPanel;

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
            lobby.CreateLobbySucceeded += OnCreateLobbySucceeded;
            lobby.CreateLobbyFailed += OnCreateLobbyFailed;
            lobby.LobbyAttributesUpdateSucceeded += OnLobbyAttributesUpdateSucceeded;
            lobby.LobbyAttributesUpdateFailed += OnLobbyAttributesUpdateFailed;
            isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (eosLobby != null)
        {
            eosLobby.CreateLobbySucceeded -= OnCreateLobbySucceeded;
            eosLobby.CreateLobbyFailed -= OnCreateLobbyFailed;
            eosLobby.LobbyAttributesUpdateSucceeded -= OnLobbyAttributesUpdateSucceeded;
            eosLobby.LobbyAttributesUpdateFailed -= OnLobbyAttributesUpdateFailed;
        }
        isSubscribed = false;
    }

    private void OnEnable() { SubscribeEvents(); }

    private void Start()
    {
        Application.runInBackground = true;

        SubscribeEvents();
        if (hostPanel != null) hostPanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }

        if (roomNameInputField != null)
        {
            roomNameInputField.onEndEdit.AddListener(OnRoomNameEndEdit);
            roomNameInputField.characterLimit = 15; // ★ 방 이름 입력 최대 글자 수 제한 (원하는 숫자로 변경 가능)[cite: 2]
        }

        UpdateChapterUI();
        UpdateRoomTypeUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        if (roomNameInputField != null)
        {
            roomNameInputField.onEndEdit.RemoveListener(OnRoomNameEndEdit);
        }
    }

    private void Update()
    {
        bool leftPressed = false;
        bool rightPressed = false;
        bool enterOrActionPressed = false;
        bool escPressed = false;

        if (Keyboard.current != null)
        {
            leftPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            rightPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
            enterOrActionPressed |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            escPressed |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            leftPressed |= Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame;
            rightPressed |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
        }

        // ESC 입력 처리 (에러 팝업 -> 호스트 패널 순서로 역방향 닫기)
        if (escPressed)
        {
            if (errorPopupPanel != null && errorPopupPanel.activeSelf)
            {
                OnClick_CloseErrorPopup();
                return;
            }
            else if (hostPanel != null && hostPanel.activeSelf)
            {
                OnClick_ReturnToMain();
                return;
            }
        }

        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;

        bool isChapterSelected = (chapterSelectObject != null && selected == chapterSelectObject) ||
                                 (chapterDisplayText != null && (selected == chapterDisplayText.gameObject || selected == chapterDisplayText.transform.parent.gameObject)) ||
                                 (prevChapterButton != null && selected == prevChapterButton.gameObject) ||
                                 (nextChapterButton != null && selected == nextChapterButton.gameObject);

        if (isChapterSelected)
        {
            if (leftPressed) OnClick_PrevChapter();
            else if (rightPressed) OnClick_NextChapter();
            return;
        }

        bool isRoomTypeSelected = (roomTypeSelectObject != null && selected == roomTypeSelectObject) ||
                                  (roomTypeDisplayText != null && (selected == roomTypeDisplayText.gameObject || selected == roomTypeDisplayText.transform.parent.gameObject)) ||
                                  (prevRoomTypeButton != null && selected == prevRoomTypeButton.gameObject) ||
                                  (nextRoomTypeButton != null && selected == nextRoomTypeButton.gameObject);

        if (isRoomTypeSelected)
        {
            if (leftPressed) OnClick_PrevRoomType();
            else if (rightPressed) OnClick_NextRoomType();
            return;
        }

        if (isPublicRoom && roomNameInputField != null)
        {
            bool isInputFieldSelected = (selected == roomNameInputField.gameObject) ||
                                        (selected == roomNameInputField.transform.parent?.gameObject);

            if (isInputFieldSelected && enterOrActionPressed)
            {
                if (!roomNameInputField.interactable)
                {
                    roomNameInputField.interactable = true;
                    roomNameInputField.ActivateInputField();
                    roomNameInputField.Select();
                }
            }
        }
    }

    private void OnRoomNameEndEdit(string text)
    {
        if (roomNameInputField != null)
        {
            Selectable nextSelectable = roomNameInputField.FindSelectableOnDown();
            roomNameInputField.interactable = false;
            roomNameInputField.DeactivateInputField();
            if (nextSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
            }
        }
    }

    public void OnClick_MainHost()
    {
        SubscribeEvents();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (logo != null) logo.SetActive(false);
        if (hostPanel != null) hostPanel.SetActive(true);

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(hostPanel);
        }
    }

    public void OnClick_PrevChapter()
    {
        selectedChapterIndex--;
        if (selectedChapterIndex < 1) selectedChapterIndex = maxChapterCount;
        UpdateChapterUI();
    }

    public void OnClick_NextChapter()
    {
        selectedChapterIndex++;
        if (selectedChapterIndex > maxChapterCount) selectedChapterIndex = 1;
        UpdateChapterUI();
    }

    private void UpdateChapterUI()
    {
        int displayChapter = selectedChapterIndex;
        int arrayIndex = selectedChapterIndex - 1;

        if (chapterDisplayText != null) chapterDisplayText.text = $"Chapter {displayChapter}";
        if (chapterPreviewImage != null && chapterSprites != null && chapterSprites.Length > arrayIndex) chapterPreviewImage.sprite = chapterSprites[arrayIndex];

        bool isUnlocked = true;
        if (GameSaveManager.Instance != null)
        {
            int maxCleared = GameSaveManager.Instance.currentData.maxClearedChapter;
            if (maxCleared < arrayIndex) isUnlocked = false;
        }

        if (chapterLockObject != null) chapterLockObject.SetActive(!isUnlocked);
        if (makeRoomButton != null) makeRoomButton.interactable = isUnlocked;
        if (chapterPreviewImage != null) chapterPreviewImage.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    public void OnClick_PrevRoomType() { isPublicRoom = !isPublicRoom; UpdateRoomTypeUI(); }
    public void OnClick_NextRoomType() { isPublicRoom = !isPublicRoom; UpdateRoomTypeUI(); }

    private void UpdateRoomTypeUI()
    {
        if (roomTypeDisplayText != null) roomTypeDisplayText.text = isPublicRoom ? "Public" : "Private";
        if (roomNameInputField != null)
        {
            roomNameInputField.interactable = false;
            roomNameInputField.DeactivateInputField();
        }
    }

    public void OnClick_MakeRoom()
    {
        SubscribeEvents();
        if (isCreatingLobby) return;

        selectedChapter = selectedChapterIndex;
        var lobby = GetEOSLobby();
        if (lobby == null) { ShowErrorPopup("네트워크 시스템이 준비되지 않았습니다."); return; }

        string roomTitle = (roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text))
     ? roomNameInputField.text
     : GetRandomDefaultRoomName();

        lastCreatedRoomName = roomTitle;

        if (isPublicRoom) currentShortCode = "";
        else currentShortCode = GenerateShortCode();

        isCreatingLobby = true;
        SetAllButtonsInteractable(false);
        currentPanel = GetLoadingPanel();
        if (currentPanel != null) currentPanel.SetActive(true);

        lobby.CreateLobby(4, LobbyPermissionLevel.Publicadvertised, false, null);
        StartCoroutine(CreateLobbyAndSetAttributesRoutine(roomTitle));
    }

    private void OnCreateLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        createLobbySuccess = true;
        isCreatingLobby = false;
    }

    private void OnCreateLobbyFailed(string errorMessage)
    {
        createLobbySuccess = false;
        isCreatingLobby = false;
        SetAllButtonsInteractable(true);
        if (currentPanel != null) currentPanel.SetActive(false);
        ShowErrorPopup("방 생성 실패: " + errorMessage);
    }
    private void OnLobbyAttributesUpdateSucceeded() { attributeUpdateDone = true; attributeUpdateFailed = false; }
    private void OnLobbyAttributesUpdateFailed(string errorMessage) { attributeUpdateDone = true; attributeUpdateFailed = true; }

    private IEnumerator CreateLobbyAndSetAttributesRoutine(string roomTitle)
    {
        float timeout = 5f;
        // 결과(성공 또는 실패)가 나올 때까지 대기
        while (isCreatingLobby && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }

        // 타임아웃 처리
        if (isCreatingLobby)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("방 생성 시간이 초과되었습니다.");
            yield break;
        }

        // ★ 실패했다면 여기서 코루틴을 중단해야 합니다!
        if (!createLobbySuccess)
        {
            yield break;
        }

        attributeUpdateDone = false; attributeUpdateFailed = false;
        List<AttributeData> attrDataList = new List<AttributeData>();
        attrDataList.Add(new AttributeData { Key = "ROOM_NAME", Value = roomTitle });
        attrDataList.Add(new AttributeData { Key = "CHAPTER", Value = selectedChapterIndex.ToString() });
        attrDataList.Add(new AttributeData { Key = "IS_PUBLIC", Value = isPublicRoom ? "1" : "0" });

        if (!isPublicRoom) attrDataList.Add(new AttributeData { Key = "SHORTCODE", Value = currentShortCode });

        var lobby = GetEOSLobby();
        if (lobby != null) lobby.UpdateLobbyAttributes(attrDataList.ToArray());

        float attributeTimeout = 5f;
        while (!attributeUpdateDone && attributeTimeout > 0f) { attributeTimeout -= Time.unscaledDeltaTime; yield return null; }

        if (attributeUpdateFailed || attributeTimeout <= 0f)
        {
            if (lobby != null && lobby.ConnectedToLobby) lobby.DestroyLobby();
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

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (mainHostButton != null) mainHostButton.interactable = interactable;
        if (makeRoomButton != null) makeRoomButton.interactable = interactable;
        if (prevChapterButton != null) prevChapterButton.interactable = interactable;
        if (nextChapterButton != null) nextChapterButton.interactable = interactable;
        if (prevRoomTypeButton != null) prevRoomTypeButton.interactable = interactable;
        if (nextRoomTypeButton != null) nextRoomTypeButton.interactable = interactable;
    }

    private GameObject GetLoadingPanel()
    {
        if (loadingPanel != null) return loadingPanel;
        if (WalkingLoadingPanel.Instance != null) { loadingPanel = WalkingLoadingPanel.Instance.gameObject; return loadingPanel; }
        return null;
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
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (errorCloseButton != null) EventSystem.current.SetSelectedGameObject(errorCloseButton.gameObject);
                else EventSystem.current.SetSelectedGameObject(errorPopupPanel);
            }
        }
    }

    public void OnClick_CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (GlobalSceneInputManager.Instance != null)
        {
            if (hostPanel != null && hostPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(hostPanel);
            }
            else if (mainPanel != null && mainPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }
    }

    public void OnClick_ReturnToMain()
    {
        var lobby = GetEOSLobby();
        if (lobby != null && lobby.ConnectedToLobby)
        {
            if (NetworkServer.active && NetworkClient.active) lobby.DestroyLobby();
            else if (NetworkClient.active) lobby.LeaveLobby();
            else lobby.DestroyLobby();
        }

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopHost();

            EosTransport transport = NetworkManager.singleton.GetComponent<EosTransport>();
            if (transport != null)
            {
                transport.Shutdown();
            }
        }

        if (hostPanel != null) hostPanel.SetActive(false);
        if (logo != null) logo.SetActive(true);
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
        }
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }

    private string GetRandomDefaultRoomName()
    {
        if (defaultRoomNames != null && defaultRoomNames.Length > 0)
        {
            int randomIndex = Random.Range(0, defaultRoomNames.Length);
            return defaultRoomNames[randomIndex];
        }
        return "Back Together!";
    }
}