using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class LanguageSelectorUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public TMP_Text languageText;
    public UIButtonFeedback leftFeedback;
    public UIButtonFeedback rightFeedback;

    // 10개 언어 Enum 순서와 일치하는 표시 이름 배열
    private readonly string[] languageNames = {
        "English", "한국어", "日本語", "中文", "Русский",
        "Italiano", "Français", "Deutsch", "Español", "Português"
    };

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                ChangeLanguageOffset(-1);
                if (leftFeedback != null) leftFeedback.PlayPressEffect();
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                ChangeLanguageOffset(1);
                if (rightFeedback != null) rightFeedback.PlayPressEffect();
            }
        }

        UpdateDisplay();
    }

    private void ChangeLanguageOffset(int delta)
    {
        if (LocalizationController.Instance == null) return;

        int langCount = System.Enum.GetValues(typeof(Language)).Length;
        int currentIndex = (int)LocalizationController.Instance.currentLanguage;

        int newIndex = (currentIndex + delta + langCount) % langCount;

        LocalizationController.Instance.ChangeLanguage((Language)newIndex);
    }

    public void UpdateDisplay()
    {
        if (LocalizationController.Instance == null || languageText == null) return;

        int currentIndex = (int)LocalizationController.Instance.currentLanguage;
        if (currentIndex >= 0 && currentIndex < languageNames.Length)
        {
            languageText.text = $"{languageNames[currentIndex]}";
        }
    }
}