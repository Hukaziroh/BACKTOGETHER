using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 신형 입력 시스템 필수
using TMPro;

public class LanguageSelectorUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public TMP_Text languageText;            // "< English >" 등이 표시될 텍스트
    public UIButtonFeedback leftFeedback;    // 왼쪽 이동 시 재생될 피드백
    public UIButtonFeedback rightFeedback;   // 오른쪽 이동 시 재생될 피드백

    // 언어 Enum 순서와 일치하는 배열
    private readonly string[] languageNames = { "English", "한국어", "日本語", "中文", "Русский" };

    void Start()
    {
        // ★ 수정됨: 강제로 영어를 세팅하던 코드를 제거하고, 
        // 씬이 로드될 때 이미 저장/설정되어 있는 현재 언어 상태를 화면에 바로 반영합니다.
        UpdateDisplay();
    }

    void Update()
    {
        // 현재 이 UI 오브젝트가 포커스(선택)되어 있을 때만 좌우 입력 감지
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            // 왼쪽 화살표 또는 A 키 -> 이전 언어로 변경
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                ChangeLanguageOffset(-1);
                if (leftFeedback != null) leftFeedback.PlayPressEffect();
            }
            // 오른쪽 화살표 또는 D 키 -> 다음 언어로 변경
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                ChangeLanguageOffset(1);
                if (rightFeedback != null) rightFeedback.PlayPressEffect();
            }
        }

        // 실시간으로 LocalizationController의 언어 상태와 텍스트 동기화
        UpdateDisplay();
    }

    // 좌우 입력에 따라 언어 인덱스를 순환 변경 (모듈러 연산 활용)
    private void ChangeLanguageOffset(int delta)
    {
        if (LocalizationController.Instance == null) return;

        int langCount = System.Enum.GetValues(typeof(Language)).Length;
        int currentIndex = (int)LocalizationController.Instance.currentLanguage;

        // 범위를 벗어나지 않도록 순환
        int newIndex = (currentIndex + delta + langCount) % langCount;

        // LocalizationController를 통해 전체 언어 변경 실행
        LocalizationController.Instance.ChangeLanguage((Language)newIndex);
    }

    // 텍스트 UI에 현재 언어를 꺾쇠 형식으로 반영
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