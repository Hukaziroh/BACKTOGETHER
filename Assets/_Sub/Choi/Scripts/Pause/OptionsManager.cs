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
        fullscreenToggle.isOn = Screen.fullScreen;
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    // --- 추가된 부분: 스스로 켜고 끄는 함수 ---
    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void Close()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
    // -------------------------------------

    public void SetVolume(float volume)
    {
        float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        audioMixer.SetFloat("Volume", db);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}