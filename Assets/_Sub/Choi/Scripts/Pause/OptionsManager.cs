using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    void Start()
    {
        // 초기 설정값 로드
        fullscreenToggle.isOn = Screen.fullScreen;

        // 슬라이더 초기값 (기본 1.0으로 설정)
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        // 로그 스케일로 변환하여 자연스러운 볼륨 제어
        float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        audioMixer.SetFloat("Volume", db);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}