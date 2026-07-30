using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Mirror;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    [Header("UI Reference")]
    public GameObject optionsPanel;

    [Header("Settings")]
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("방 코드 UI (모든 플레이어 공용)")]
    public GameObject roomCodeUIContainer;
    public TextMeshProUGUI RoomCodeText;
    public Button toggleVisibilityButton;

    public bool IsCodeVisible => isCodeVisible;

    // ★ 시작할 때 코드가 드러난 상태(true)로 설정
    private bool isCodeVisible = true;
    private Coroutine fetchCodeRoutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleRoomCodeVisibility);
        }
    }

    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (PauseManager.instance != null) PauseManager.instance.ClosePause();

        UpdateRoomCodeUI();

        if (string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
        {
            if (fetchCodeRoutine != null) StopCoroutine(fetchCodeRoutine);
            fetchCodeRoutine = StartCoroutine(FetchRoomCodeAsync());
        }

        // GlobalSceneInputManager에게 옵션창 내부로 포커스 및 하이라이트 관리를 위임
        if (GlobalSceneInputManager.Instance != null && optionsPanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
        }
    }

    public void Close()
    {
        if (fetchCodeRoutine != null)
        {
            StopCoroutine(fetchCodeRoutine);
            fetchCodeRoutine = null;
        }

        if (optionsPanel != null) optionsPanel.SetActive(false);

        // ★ [핵심] 옵션창을 닫을 때 EventSystem 선택을 해제하여 잔류 하이라이트를 모두 끔
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";

        if (!isMainScene && PauseManager.instance != null && PauseManager.instance.pausePanel != null)
        {
            PauseManager.instance.pausePanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                // 퍼즈창으로 포커스 복구 및 방향키 하이라이트 정상 작동 갱신
                GlobalSceneInputManager.Instance.SetFocusScope(PauseManager.instance.pausePanel);
            }
        }
        else
        {
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.ClearFocusScope();
            }
        }
    }

    private void ToggleRoomCodeVisibility()
    {
        isCodeVisible = !isCodeVisible;
        UpdateRoomCodeUI();

        if (LobbySyncManager.instance != null)
        {
            LobbySyncManager.instance.RefreshRoomCodeUIState();
        }
    }

    private IEnumerator FetchRoomCodeAsync()
    {
        float timeout = 3.0f;
        while (timeout > 0f)
        {
            string code = GetCodeFromEOS();
            if (!string.IsNullOrEmpty(code))
            {
                PrivateLobbyManager.currentShortCode = code;
                UpdateRoomCodeUI();
                yield break;
            }

            timeout -= 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private string GetCodeFromEOS()
    {
        if (NetworkManager.singleton == null) return null;

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        if (eosLobby != null && eosLobby.ConnectedToLobby && eosLobby.ConnectedLobbyDetails != null)
        {
            try
            {
                Attribute shortCodeAttribute = new Attribute();
                Result result = eosLobby.ConnectedLobbyDetails.CopyAttributeByKey(
                    new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "SHORTCODE" },
                    out shortCodeAttribute
                );

                if (result == Result.Success)
                {
                    return shortCodeAttribute.Data.Value.AsUtf8;
                }
            }
            catch
            {
            }
        }
        return null;
    }

    public void UpdateRoomCodeUI()
    {
        if (RoomCodeText == null) return;

        string displayCode = "";

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";
        if (!isMainScene)
        {
            if (LobbySyncManager.instance != null)
            {
                displayCode = LobbySyncManager.instance.roomCode;
            }

            if (string.IsNullOrEmpty(displayCode))
            {
                displayCode = PrivateLobbyManager.currentShortCode;
            }
        }

        string prefix = "Code:\n";
        if (!string.IsNullOrEmpty(RoomCodeText.text))
        {
            int splitIndex = RoomCodeText.text.IndexOf('\n');
            if (splitIndex != -1 && RoomCodeText.text.Length >= splitIndex + 1)
            {
                prefix = RoomCodeText.text.Substring(0, splitIndex + 1);
            }
            else
            {
                int colonIndex = RoomCodeText.text.IndexOf(':');
                if (colonIndex != -1)
                {
                    prefix = RoomCodeText.text.Substring(0, colonIndex + 1) + "\n";
                }
            }
        }

        if (isMainScene || string.IsNullOrEmpty(displayCode))
        {
            RoomCodeText.text = isCodeVisible ? (prefix + "Empty") : (prefix + "******");
        }
        else
        {
            RoomCodeText.text = isCodeVisible ? prefix + displayCode : prefix + "******";
        }

        if (LobbySyncManager.instance != null)
        {
            LobbySyncManager.instance.RefreshRoomCodeUIState();
        }
    }

    public void SetVolume(float volume)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            audioMixer.SetFloat("Volume", db);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (!isFullscreen)
        {
            Screen.SetResolution(1280, 720, false);
        }
    }
}