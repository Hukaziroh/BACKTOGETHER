using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    [Header("UI Reference")]
    public GameObject optionsPanel;

    [Header("Settings")]
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Slider sfxVolumeSlider;
    public Slider bgmVolumeSlider;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;

    [Header("옵션 글자 외곽선")]
    [SerializeField] private Color optionTextOutlineColor = Color.black;
    [SerializeField, Range(0f, 2f)] private float optionOutlineThickness = 1f;
    [SerializeField] private Vector2 optionLegacyOutlineDistance = new Vector2(2f, -2f);

    [Header("Volume Panel UI")]
    public GameObject volumePanel;
    public Button volumePanelToggleButton;
    public Button volumePanelBackButton;

    [Header("방 코드 UI (모든 플레이어 공용)")]
    public GameObject roomCodeUIContainer;

    [Header("★ 다국어 영향 안 받는 완전 독립형 TMPro")]
    public TextMeshProUGUI independentRoomCodeText;

    public Button toggleVisibilityButton;

    [Header("★ 방 코드 가리기 토글용 이미지 슬롯 (인스펙터에서 드래그)")]
    public GameObject visibilityIconObject;

    [Header("Key Guide Panels (모든 키 가이드 패널 배열 통일)")]
    public GameObject[] allKeyGuidePanels; // [0]: Keyboard, [1]: XBOX, [2]: Switch, [3]: PlayStation 등 순서대로 등록
    public Button keyGuideToggleButton;

    [HideInInspector]
    public bool isOpenedFromZone = false;

    [HideInInspector]
    public float blockPauseUntilTime = 0f;

    public bool IsCodeVisible => isCodeVisible;

    public bool IsSettingsOpen => (optionsPanel != null && optionsPanel.activeSelf) ||
                                  (volumePanel != null && volumePanel.activeSelf) ||
                                  IsAnyKeyGuideActive() ||
                                  (Time.unscaledTime < blockPauseUntilTime);

    public bool ShouldBlockPauseOrOptions => IsAnyKeyGuideActive() ||
                                             isOpenedFromZone ||
                                             Time.unscaledTime < blockPauseUntilTime;

    private bool isCodeVisible = true;
    private Coroutine fetchCodeRoutine;
    private bool wasOptionsOpenBeforeKeyGuide = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioMixer != null)
            {
                AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
                if (groups.Length > 0) PlayerSoundUtility.DefaultSfxGroup = groups[0];
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyOptionTextOutlines();

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (vSyncToggle != null)
        {
            int savedVSync = PlayerPrefs.GetInt("VSync", 1);
            QualitySettings.vSyncCount = savedVSync;
            if (savedVSync == 0) Application.targetFrameRate = 144;
            else Application.targetFrameRate = -1;

            vSyncToggle.isOn = (savedVSync == 1);
            vSyncToggle.onValueChanged.AddListener(SetVSync);
        }

        if (volumeSlider != null)
        {
            float loadedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            if (PlayerPrefs.HasKey("MasterVolume") && audioMixer != null)
            {
                float db = Mathf.Log10(Mathf.Max(loadedVolume, 0.0001f)) * 20;
                audioMixer.SetFloat("Volume", db);
            }
            else if (audioMixer != null && audioMixer.GetFloat("Volume", out float currentDb))
            {
                loadedVolume = Mathf.Pow(10f, currentDb / 20f);
            }

            volumeSlider.SetValueWithoutNotify(loadedVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (sfxVolumeSlider != null)
        {
            float loadedSfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (PlayerPrefs.HasKey("SFXVolume") && audioMixer != null)
            {
                float db = Mathf.Log10(Mathf.Max(loadedSfxVolume, 0.0001f)) * 20;
                audioMixer.SetFloat("SFXVolume", db);
            }
            else if (audioMixer != null && audioMixer.GetFloat("SFXVolume", out float currentSfxDb))
            {
                loadedSfxVolume = Mathf.Pow(10f, currentSfxDb / 20f);
            }

            sfxVolumeSlider.SetValueWithoutNotify(loadedSfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (bgmVolumeSlider != null)
        {
            float loadedBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
            if (PlayerPrefs.HasKey("BGMVolume") && audioMixer != null)
            {
                float db = Mathf.Log10(Mathf.Max(loadedBgmVolume, 0.0001f)) * 20;
                audioMixer.SetFloat("BGMVolume", db);
            }
            else if (audioMixer != null && audioMixer.GetFloat("BGMVolume", out float currentBgmDb))
            {
                loadedBgmVolume = Mathf.Pow(10f, currentBgmDb / 20f);
            }

            bgmVolumeSlider.SetValueWithoutNotify(loadedBgmVolume);
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleRoomCodeVisibility);
        }

        if (keyGuideToggleButton != null)
        {
            keyGuideToggleButton.onClick.AddListener(ToggleKeyGuide);
        }

        if (volumePanelToggleButton != null)
        {
            volumePanelToggleButton.onClick.AddListener(ToggleVolumePanel);
        }

        if (volumePanelBackButton != null)
        {
            volumePanelBackButton.onClick.AddListener(CloseVolumePanel);
        }

        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }

        UpdateVisibilityButtonGraphic();
    }

    private void ApplyOptionTextOutlines()
    {
        if (optionsPanel == null) return;

        // TMP의 재질 Outline Width는 0~1 비율이라 1을 그대로 넣으면 글자가 뭉친다.
        // 인스펙터의 두께 1을 안전한 재질 값 0.1로 환산한다.
        float safeTmpOutlineWidth = Mathf.Clamp(optionOutlineThickness, 0f, 2f) * 0.1f;

        Text[] legacyTexts = optionsPanel.GetComponentsInChildren<Text>(true);
        foreach (Text legacyText in legacyTexts)
        {
            if (legacyText == null) continue;

            Outline outline = legacyText.GetComponent<Outline>();
            if (outline == null)
            {
                outline = legacyText.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = optionTextOutlineColor;
            outline.effectDistance = optionLegacyOutlineDistance;
            outline.useGraphicAlpha = true;
        }

        TextMeshProUGUI[] tmpTexts = optionsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmpText in tmpTexts)
        {
            if (tmpText == null) continue;

            Material material = tmpText.fontMaterial;
            if (material == null) continue;

            SetMaterialFloatIfExists(material, "_FaceDilate", 0f);
            SetMaterialColorIfExists(material, "_OutlineColor", optionTextOutlineColor);
            SetMaterialFloatIfExists(material, "_OutlineWidth", safeTmpOutlineWidth);
            SetMaterialFloatIfExists(material, "_OutlineSoftness", 0f);
            SetMaterialColorIfExists(material, "_UnderlayColor", Color.clear);
            SetMaterialFloatIfExists(material, "_UnderlayOffsetX", 0f);
            SetMaterialFloatIfExists(material, "_UnderlayOffsetY", 0f);
            SetMaterialFloatIfExists(material, "_UnderlayDilate", 0f);
            SetMaterialFloatIfExists(material, "_UnderlaySoftness", 0f);
            SetMaterialFloatIfExists(material, "_GlowPower", 0f);

            material.DisableKeyword("UNDERLAY_ON");
            material.DisableKeyword("UNDERLAY_INNER");
            material.DisableKeyword("GLOW_ON");
            ShaderUtilities.UpdateShaderRatios(material);

            tmpText.fontMaterial = material;
            tmpText.SetMaterialDirty();
            tmpText.SetVerticesDirty();
        }
    }

    private static void SetMaterialFloatIfExists(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetMaterialColorIfExists(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private void ShowOptionsPanel()
    {
        if (optionsPanel == null) return;

        // LocalizedText.OnEnable에서 재질을 갱신한 다음 외곽선을 최종 적용한다.
        optionsPanel.SetActive(true);
        ApplyOptionTextOutlines();
    }

    public bool IsAnyKeyGuideActive()
    {
        if (allKeyGuidePanels != null && allKeyGuidePanels.Length > 0)
        {
            foreach (var panel in allKeyGuidePanels)
            {
                if (panel != null && panel.activeSelf) return true;
            }
        }
        return false;
    }

    public void CloseAllKeyGuides()
    {
        if (allKeyGuidePanels != null && allKeyGuidePanels.Length > 0)
        {
            foreach (var panel in allKeyGuidePanels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
    }

    public void Open()
    {
        // 키 가이드를 닫은 ESC/B 입력이 같은 프레임에 옵션까지 여는 것을 막는다.
        if (ShouldBlockPauseOrOptions) return;

        ShowOptionsPanel();
        CloseAllKeyGuides();
        if (volumePanel != null) volumePanel.SetActive(false);

        if (PauseManager.instance != null && PauseManager.instance.pausePanel != null)
        {
            PauseManager.instance.pausePanel.SetActive(false);
        }

        UpdateRoomCodeUI();

        if (string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
        {
            if (fetchCodeRoutine != null) StopCoroutine(fetchCodeRoutine);
            fetchCodeRoutine = StartCoroutine(FetchRoomCodeAsync());
        }

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

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        CloseAllKeyGuides();

        if (volumePanel != null)
            volumePanel.SetActive(false);

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";

        if (!isMainScene && PauseManager.instance != null)
        {
            PauseManager.instance.isPaused = true;

            if (PauseManager.instance.pausePanel != null)
            {
                PauseManager.instance.pausePanel.SetActive(true);

                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(
                        PauseManager.instance.pausePanel
                    );
                }

                Button firstButton = PauseManager.instance.pausePanel.GetComponentInChildren<Button>();
                if (firstButton != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
                }
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

    public void ToggleVolumePanel()
    {
        if (volumePanel != null)
        {
            bool isActive = volumePanel.activeSelf;
            volumePanel.SetActive(!isActive);

            if (!isActive && optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
            else if (isActive && optionsPanel != null)
            {
                ShowOptionsPanel();
            }

            if (!isActive && GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(volumePanel);
            }
            else if (isActive && GlobalSceneInputManager.Instance != null && optionsPanel != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
            }
        }
    }

    public void CloseVolumePanel()
    {
        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            ShowOptionsPanel();
            UpdateRoomCodeUI();

            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
            }

            Button firstButton = optionsPanel.GetComponentInChildren<Button>();
            if (firstButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }
    }

    private void ToggleRoomCodeVisibility()
    {
        isCodeVisible = !isCodeVisible;
        UpdateRoomCodeUI();
        UpdateVisibilityButtonGraphic();

        if (LobbySyncManager.instance != null)
        {
            LobbySyncManager.instance.RefreshRoomCodeUIState();
        }
    }

    private void UpdateVisibilityButtonGraphic()
    {
        if (visibilityIconObject != null)
        {
            visibilityIconObject.SetActive(!isCodeVisible);
        }
    }

    public void ToggleKeyGuide()
    {
        bool isAnyActive = IsAnyKeyGuideActive();

        if (!isAnyActive)
        {
            isOpenedFromZone = false;
            wasOptionsOpenBeforeKeyGuide = (optionsPanel != null && optionsPanel.activeSelf);

            if (allKeyGuidePanels != null && allKeyGuidePanels.Length > 0 && allKeyGuidePanels[0] != null)
            {
                allKeyGuidePanels[0].SetActive(true);
                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(allKeyGuidePanels[0]);
                }
            }

            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (volumePanel != null) volumePanel.SetActive(false);
        }
        else
        {
            CloseAllKeyGuides();

            if (wasOptionsOpenBeforeKeyGuide && optionsPanel != null)
            {
                ShowOptionsPanel();
                UpdateRoomCodeUI();

                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
                }

                Button firstButton = optionsPanel.GetComponentInChildren<Button>();
                if (firstButton != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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
        if (independentRoomCodeText == null) return;

        string displayCode = "";
        bool isMainScene = SceneManager.GetActiveScene().name == "Main";

        if (!isMainScene)
        {
            if (LobbySyncManager.instance != null)
            {
                if (string.IsNullOrEmpty(LobbySyncManager.instance.roomCode))
                {
                    displayCode = string.IsNullOrEmpty(LobbySyncManager.instance.roomName) ? "Public Room" : LobbySyncManager.instance.roomName;
                }
                else
                {
                    displayCode = LobbySyncManager.instance.roomCode;
                }
            }

            if (string.IsNullOrEmpty(displayCode))
            {
                displayCode = PrivateLobbyManager.currentShortCode;
            }
        }

        if (isMainScene || string.IsNullOrEmpty(displayCode))
        {
            independentRoomCodeText.text = isCodeVisible ? "Empty" : "******";
        }
        else
        {
            independentRoomCodeText.text = isCodeVisible ? displayCode : "******";
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
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            audioMixer.SetFloat("SFXVolume", db);
        }
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float volume)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            audioMixer.SetFloat("BGMVolume", db);
        }
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (!isFullscreen)
        {
            Screen.SetResolution(1280, 720, false);
        }
    }

    public void SetVSync(bool isVSync)
    {
        if (isVSync)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            PlayerPrefs.SetInt("VSync", 1);
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 144;
            PlayerPrefs.SetInt("VSync", 0);
        }
        PlayerPrefs.Save();
    }
}
