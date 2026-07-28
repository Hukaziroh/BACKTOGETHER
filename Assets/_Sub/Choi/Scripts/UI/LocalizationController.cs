using UnityEngine;
using System.Collections.Generic;

public enum Language
{
    Korean = 0,
    English = 1,
    Japanese = 2,
    Chinese = 3,
    Russian = 4
}

public class LocalizationController : MonoBehaviour
{
    public static LocalizationController Instance;

    [Header("현재 설정된 언어")]
    public Language currentLanguage = Language.Korean;

    [Header("언어 설정 패널 오브젝트")]
    public GameObject languagePanel; // 껐다 켤 패널 UI

    private static List<LocalizedText> allLocalizedTexts = new List<LocalizedText>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 유니티 버튼 OnClick에서 Int 값으로 호출할 수 있는 함수
    public void ChangeLanguage(int langIndex)
    {
        ChangeLanguage((Language)langIndex);
    }

    public void ChangeLanguage(Language newLang)
    {
        currentLanguage = newLang;
        RefreshAllTexts();
    }

    // ==========================================
    // 💡 패널 제어 함수 (열기 / 닫기 / 토글)
    // ==========================================
    public void OpenLanguagePanel()
    {
        if (languagePanel != null)
        {
            // ★ 언어 패널이 켜질 때 옵션 패널이 열려있다면 강제로 끔
            if (OptionsManager.instance != null && OptionsManager.instance.optionsPanel != null)
            {
                OptionsManager.instance.optionsPanel.SetActive(false);
            }

            languagePanel.SetActive(true);

            // 옵션 매니저처럼 GlobalSceneInputManager에 명확하게 포커스 범위를 지정해 줌
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(languagePanel);
            }
        }
    }

    public void CloseLanguagePanel()
    {
        if (languagePanel != null)
        {
            languagePanel.SetActive(false);

            // 포커스 격리 해제
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.ClearFocusScope();
            }
        }
    }

    public void ToggleLanguagePanel()
    {
        if (languagePanel != null)
        {
            if (languagePanel.activeSelf)
            {
                CloseLanguagePanel();
            }
            else
            {
                OpenLanguagePanel();
            }
        }
    }

    public static void RegisterText(LocalizedText textComp)
    {
        if (!allLocalizedTexts.Contains(textComp))
            allLocalizedTexts.Add(textComp);
    }

    public static void UnregisterText(LocalizedText textComp)
    {
        if (allLocalizedTexts.Contains(textComp))
            allLocalizedTexts.Remove(textComp);
    }

    private void RefreshAllTexts()
    {
        foreach (var textComp in allLocalizedTexts)
        {
            if (textComp != null)
                textComp.UpdateTextAndStyle();
        }
    }
}