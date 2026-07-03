using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class OnlyOptionExit : MonoBehaviour
{
    public GameObject optionPanel; // 옵션창 오브젝트

    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;


    void Start()
    {
        // 초기값 설정
        fullscreenToggle.isOn = Screen.fullScreen;
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetVolume(float volume)
    {
        float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        audioMixer.SetFloat("Volume", db);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    public void OpenOption()
    {
        if (optionPanel != null) optionPanel.SetActive(true);
    }

    public void CloseOption()
    {
        if (optionPanel != null) optionPanel.SetActive(false);
    }
    public void QuitGame()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}