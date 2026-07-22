using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    [Header("UI Reference")] // 👈 연결할 패널을 인스펙터에서 지정하세요
    public GameObject optionsPanel;

    [Header("Settings")]
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

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
        // 초기값 설정
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    // --- 옵션창을 열 때: 포커스를 옵션 패널로 격리하여 키보드/패드 조작 활성화 ---
    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);

        // GlobalSceneInputManager를 통해 옵션 패널 내부 요소들만 조작 가능하게 포커스 전환
        if (GlobalSceneInputManager.Instance != null && optionsPanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
        }
    }

    // --- 옵션창을 닫을 때: 퍼즈 패널로 포커스를 복구하거나 격리 해제 ---
    public void Close()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // 만약 퍼즈 매니저가 존재하고 퍼즈 패널이 있다면 포커스를 퍼즈 패널 쪽으로 안전하게 복구
        if (PauseManager.instance != null && PauseManager.instance.pausePanel != null)
        {
            PauseManager.instance.pausePanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(PauseManager.instance.pausePanel);
            }
        }
        else
        {
            // 퍼즈 패널이 없다면 일반 격리 해제
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.ClearFocusScope();
            }
        }
    }
    // -------------------------------------------------------------

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
    }
}