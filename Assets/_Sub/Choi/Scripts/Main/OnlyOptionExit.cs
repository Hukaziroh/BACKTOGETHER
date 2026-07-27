using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Mirror;              
using System.Collections; 

public class OnlyOptionExit : MonoBehaviour
{
    public GameObject optionPanel;

    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    void Start()
    {
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
        Debug.Log("게임 종료 시퀀스 시작...");
        StartCoroutine(SafeQuitRoutine());
    }

    private IEnumerator SafeQuitRoutine()
    {
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
        }
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("게임 최종 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}