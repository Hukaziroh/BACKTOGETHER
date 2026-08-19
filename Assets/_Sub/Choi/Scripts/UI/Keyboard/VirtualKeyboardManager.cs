using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class VirtualKeyboardManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject keyboardPanel;      // 키보드 전체 패널
    [SerializeField] private Button firstSelectedButton;    // 키보드 열릴 때 맨 처음 포커스될 버튼

    private TMP_InputField targetInputField;

    private void Start()
    {
        if (keyboardPanel != null) keyboardPanel.SetActive(false);
    }

    public void OpenKeyboard(TMP_InputField inputField)
    {
        targetInputField = inputField;

        if (keyboardPanel != null)
        {
            keyboardPanel.SetActive(true);

            // 중요: GlobalSceneInputManager에게 포커스 범위 제어권 요청
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(keyboardPanel);
            }

            // 포커스 강제 이동 (Manager가 갱신한 직후에 실행되도록 딜레이)
            StartCoroutine(FocusCoroutine());
        }
    }

    private IEnumerator FocusCoroutine()
    {
        yield return null; // 1프레임 대기하여 Manager의 갱신이 끝난 후 처리

        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }

    public void InputCharacter(string character)
    {
        if (targetInputField != null)
        {
            targetInputField.text += character;
            targetInputField.caretPosition = targetInputField.text.Length;
        }
    }

    public void OnClickBackspace()
    {
        if (targetInputField != null && targetInputField.text.Length > 0)
        {
            targetInputField.text = targetInputField.text.Substring(0, targetInputField.text.Length - 1);
        }
    }

    public void OnClickConfirm()
    {
        // 닫을 때 기존 매니저의 포커스 범위 해제
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }

        if (keyboardPanel != null) keyboardPanel.SetActive(false);

        // 다시 인풋필드로 포커스 복귀
        if (targetInputField != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(targetInputField.gameObject);
        }

        targetInputField = null;
    }
}