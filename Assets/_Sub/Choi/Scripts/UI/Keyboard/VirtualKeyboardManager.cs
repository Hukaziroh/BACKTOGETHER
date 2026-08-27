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
    private Coroutine focusCoroutine;
    private int restoreFocusRequestId;

    public bool IsOpen => keyboardPanel != null && keyboardPanel.activeSelf;

    private void Awake()
    {
        // Manager가 비활성 KeyboardPanel 자기 자신에 붙어 있는 경우,
        // 첫 OpenKeyboard()로 활성화되며 Awake가 호출된다. 이때 다시 끄면 열리지 않는다.
        if (keyboardPanel != null && keyboardPanel != gameObject)
        {
            keyboardPanel.SetActive(false);
        }
    }

    public void OpenKeyboard(TMP_InputField inputField)
    {
        if (inputField == null || keyboardPanel == null) return;

        targetInputField = inputField;
        targetInputField.interactable = true;
        restoreFocusRequestId++;

        keyboardPanel.SetActive(true);
        ResetKeyVisualStates();

        // 중요: GlobalSceneInputManager에게 포커스 범위 제어권 요청
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(keyboardPanel);
        }

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
        }

        // 포커스 강제 이동 (Manager가 갱신한 직후에 실행되도록 딜레이)
        focusCoroutine = StartCoroutine(FocusCoroutine());
    }

    private void LateUpdate()
    {
        KeepTargetInputFieldInteractable();
        KeepFocusInsideKeyboard();
    }

    private IEnumerator FocusCoroutine()
    {
        yield return null; // 1프레임 대기하여 Manager의 갱신이 끝난 후 처리

        SelectFirstButton();

        // 키보드 버튼으로 포커스가 이동하면서 InputField의 OnEndEdit가 호출되어도
        // 가상 키보드 사용 중에는 Interactable 상태를 유지한다.
        KeepTargetInputFieldInteractable();

        // 다른 지연 포커스 작업이 뒤늦게 선택을 지웠을 경우에만 한 번 복구한다.
        yield return null;

        if (keyboardPanel != null && keyboardPanel.activeInHierarchy && EventSystem.current != null)
        {
            KeepFocusInsideKeyboard();
        }

        KeepTargetInputFieldInteractable();

        focusCoroutine = null;
    }

    private void KeepTargetInputFieldInteractable()
    {
        if (!IsOpen || targetInputField == null) return;

        targetInputField.interactable = true;
    }

    private void KeepFocusInsideKeyboard()
    {
        if (!IsOpen || EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(keyboardPanel.transform)) return;

        SelectFirstButton();
    }

    private void SelectFirstButton()
    {
        if (firstSelectedButton != null && EventSystem.current != null &&
            firstSelectedButton.isActiveAndEnabled && firstSelectedButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
            return;
        }

        Selectable fallback = FindFirstKeyboardSelectable();
        if (fallback != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
        }
    }

    private Selectable FindFirstKeyboardSelectable()
    {
        if (keyboardPanel == null) return null;

        Selectable[] selectables = keyboardPanel.GetComponentsInChildren<Selectable>(false);
        foreach (Selectable selectable in selectables)
        {
            if (selectable != null && selectable.isActiveAndEnabled && selectable.interactable)
            {
                return selectable;
            }
        }

        return null;
    }

    private void ResetKeyVisualStates()
    {
        if (keyboardPanel == null) return;

        KeyboardKeyButton[] keyButtons = keyboardPanel.GetComponentsInChildren<KeyboardKeyButton>(true);
        foreach (KeyboardKeyButton keyButton in keyButtons)
        {
            if (keyButton != null)
            {
                keyButton.ResetVisualState();
            }
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
        TMP_InputField returnTarget = targetInputField;
        Selectable nextSelectable = returnTarget != null ? returnTarget.FindSelectableOnDown() : null;
        targetInputField = null;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        // Scope 해제 과정에서 임시 비활성 UI가 다시 interactable=true로 복원된다.
        // 따라서 InputField의 최종 비활성 처리는 Scope 해제 이후에 해야 한다.
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }

        if (returnTarget != null)
        {
            returnTarget.DeactivateInputField();
            returnTarget.interactable = false;
        }

        int requestId = ++restoreFocusRequestId;

        // KeyboardPanel이 꺼지면 이 Manager의 코루틴도 중단되므로,
        // 계속 활성 상태인 InputField를 실행 주체로 사용한다.
        if (returnTarget != null && returnTarget.isActiveAndEnabled)
        {
            returnTarget.StartCoroutine(RestoreNavigationFocusCoroutine(nextSelectable, requestId));
        }

        ResetKeyVisualStates();

        if (keyboardPanel != null) keyboardPanel.SetActive(false);
    }

    public void CloseKeyboard()
    {
        if (!IsOpen) return;

        TMP_InputField returnTarget = targetInputField;
        targetInputField = null;
        restoreFocusRequestId++;

        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }

        if (returnTarget != null)
        {
            returnTarget.DeactivateInputField();
            returnTarget.interactable = false;
        }

        ResetKeyVisualStates();
        keyboardPanel.SetActive(false);
    }

    private IEnumerator RestoreNavigationFocusCoroutine(Selectable nextSelectable, int requestId)
    {
        yield return null;

        if (requestId != restoreFocusRequestId || EventSystem.current == null) yield break;

        if (nextSelectable != null && nextSelectable.isActiveAndEnabled && nextSelectable.interactable)
        {
            EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
        }
        else if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.RefreshAllSelectables();
        }
    }
}
