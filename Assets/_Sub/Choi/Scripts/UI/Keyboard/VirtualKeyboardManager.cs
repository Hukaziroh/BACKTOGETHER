using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class VirtualKeyboardManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject keyboardPanel;      // 키보드 전체 패널
    [SerializeField] private Button firstSelectedButton;    // 키보드 열릴 때 맨 처음 포커스될 버튼

    private TMP_InputField targetInputField;
    private Coroutine focusCoroutine;
    private int restoreFocusRequestId;
    private bool shiftActive;
    private bool capsLockActive;
    private static VirtualKeyboardManager activeKeyboard;
    private static int lastBackHandledFrame = -1;

    public bool IsOpen => keyboardPanel != null && keyboardPanel.activeSelf;
    public static bool BlocksGlobalBackInput =>
        (activeKeyboard != null && activeKeyboard.IsOpen) || lastBackHandledFrame == Time.frameCount;

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
        activeKeyboard = this;
        ConfigureModifierKeys();
        ConfigureKeyboardNavigation();
        ResetKeyVisualStates();
        ResetModifierStates();

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

    private void Update()
    {
        if (!IsOpen || !WasUICancelPressedThisFrame()) return;

        // InputSystemUIInputModule의 Cancel 액션은 Enter와 동일하게 현재 입력을 확정하고 닫는다.
        // 같은 프레임에 뒤쪽 패널까지 닫히지 않도록 처리 프레임을 기록한다.
        lastBackHandledFrame = Time.frameCount;
        OnClickConfirm();
    }

    public static bool WasUICancelPressedThisFrame()
    {
        if (EventSystem.current == null ||
            EventSystem.current.currentInputModule is not InputSystemUIInputModule inputModule)
        {
            return false;
        }

        InputActionReference cancelReference = inputModule.cancel;
        return cancelReference != null && cancelReference.action != null &&
               cancelReference.action.WasPressedThisFrame();
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
            targetInputField.text += ApplyLetterCase(character);
            targetInputField.caretPosition = targetInputField.text.Length;

            // 가상 키보드의 Shift는 다음 문자 한 번에만 적용한다.
            if (shiftActive)
            {
                shiftActive = false;
                RefreshModifierVisuals();
            }
        }
    }

    public void OnClickShift()
    {
        shiftActive = !shiftActive;
        RefreshModifierVisuals();
    }

    public void OnClickCapsLock()
    {
        capsLockActive = !capsLockActive;
        RefreshModifierVisuals();
    }

    private string ApplyLetterCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        bool useUppercase = capsLockActive ^ shiftActive;
        return useUppercase
            ? value.ToUpper(CultureInfo.CurrentCulture)
            : value.ToLower(CultureInfo.CurrentCulture);
    }

    private void ResetModifierStates()
    {
        shiftActive = false;
        capsLockActive = false;
        RefreshModifierVisuals();
    }

    private void RefreshModifierVisuals()
    {
        if (keyboardPanel == null) return;

        KeyboardKeyButton[] keyButtons = keyboardPanel.GetComponentsInChildren<KeyboardKeyButton>(true);
        foreach (KeyboardKeyButton keyButton in keyButtons)
        {
            if (keyButton == null) continue;

            if (keyButton.keyType == KeyboardKeyButton.KeyType.Shift)
            {
                keyButton.SetModifierActive(shiftActive);
            }
            else if (keyButton.keyType == KeyboardKeyButton.KeyType.CapsLock)
            {
                keyButton.SetModifierActive(capsLockActive);
            }
        }
    }

    private void ConfigureModifierKeys()
    {
        if (keyboardPanel == null) return;

        Transform[] children = keyboardPanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == null || child == keyboardPanel.transform) continue;

            string normalizedName = child.name.Replace("_", "").Replace("-", "").Replace(" ", "").ToUpperInvariant();
            KeyboardKeyButton.KeyType keyType;

            if (normalizedName == "SHIFT" || normalizedName == "LEFTSHIFT" || normalizedName == "RIGHTSHIFT")
            {
                keyType = KeyboardKeyButton.KeyType.Shift;
            }
            else if (normalizedName == "CAP" || normalizedName == "CAPS" || normalizedName == "CAPSLOCK")
            {
                keyType = KeyboardKeyButton.KeyType.CapsLock;
            }
            else
            {
                continue;
            }

            Button button = child.GetComponent<Button>();
            if (button == null)
            {
                button = child.gameObject.AddComponent<Button>();
            }

            KeyboardKeyButton keyButton = child.GetComponent<KeyboardKeyButton>();
            bool needsRuntimeListener = keyButton == null;
            if (keyButton == null)
            {
                keyButton = child.gameObject.AddComponent<KeyboardKeyButton>();
            }

            keyButton.manager = this;
            keyButton.keyType = keyType;
            keyButton.characterValue = string.Empty;

            if (needsRuntimeListener)
            {
                button.onClick.AddListener(keyButton.OnClickKey);
            }
        }
    }

    public void ConfigureKeyboardNavigation()
    {
        if (keyboardPanel == null) return;

        KeyboardKeyButton[] keyButtons = keyboardPanel.GetComponentsInChildren<KeyboardKeyButton>(true);
        List<KeyboardKeyButton> navigationKeys = new List<KeyboardKeyButton>();

        foreach (KeyboardKeyButton keyButton in keyButtons)
        {
            if (keyButton == null) continue;

            Button button = keyButton.GetComponent<Button>();
            KeyboardKeyButton[] nestedKeys = keyButton.GetComponentsInChildren<KeyboardKeyButton>(true);

            // 행 컨테이너가 문자 키로 잘못 자동 등록된 이전 씬 데이터를 탐색에서 제외한다.
            if (nestedKeys.Length > 1)
            {
                if (button != null) button.interactable = false;
                continue;
            }

            if (button != null && button.enabled && button.interactable && keyButton.gameObject.activeSelf &&
                keyButton.keyType != KeyboardKeyButton.KeyType.None)
            {
                navigationKeys.Add(keyButton);
            }
        }

        navigationKeys.Sort((a, b) =>
        {
            Vector2 positionA = GetKeyboardLocalPosition(a.transform);
            Vector2 positionB = GetKeyboardLocalPosition(b.transform);
            int verticalOrder = positionB.y.CompareTo(positionA.y);
            return verticalOrder != 0 ? verticalOrder : positionA.x.CompareTo(positionB.x);
        });

        const float rowTolerance = 25f;
        List<List<KeyboardKeyButton>> rows = new List<List<KeyboardKeyButton>>();

        foreach (KeyboardKeyButton keyButton in navigationKeys)
        {
            float keyY = GetKeyboardLocalPosition(keyButton.transform).y;
            List<KeyboardKeyButton> targetRow = null;

            foreach (List<KeyboardKeyButton> row in rows)
            {
                float rowY = GetKeyboardLocalPosition(row[0].transform).y;
                if (Mathf.Abs(rowY - keyY) <= rowTolerance)
                {
                    targetRow = row;
                    break;
                }
            }

            if (targetRow == null)
            {
                targetRow = new List<KeyboardKeyButton>();
                rows.Add(targetRow);
            }

            targetRow.Add(keyButton);
        }

        rows.Sort((a, b) => GetKeyboardLocalPosition(b[0].transform).y
            .CompareTo(GetKeyboardLocalPosition(a[0].transform).y));

        foreach (List<KeyboardKeyButton> row in rows)
        {
            row.Sort((a, b) => GetKeyboardLocalPosition(a.transform).x
                .CompareTo(GetKeyboardLocalPosition(b.transform).x));
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            List<KeyboardKeyButton> row = rows[rowIndex];

            for (int keyIndex = 0; keyIndex < row.Count; keyIndex++)
            {
                KeyboardKeyButton keyButton = row[keyIndex];
                Button button = keyButton.GetComponent<Button>();
                float keyX = GetKeyboardLocalPosition(keyButton.transform).x;

                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.wrapAround = false;
                navigation.selectOnLeft = row.Count > 1
                    ? row[(keyIndex - 1 + row.Count) % row.Count].GetComponent<Button>()
                    : null;
                navigation.selectOnRight = row.Count > 1
                    ? row[(keyIndex + 1) % row.Count].GetComponent<Button>()
                    : null;
                navigation.selectOnUp = rowIndex > 0
                    ? FindClosestHorizontalButton(rows[rowIndex - 1], keyX)
                    : null;
                navigation.selectOnDown = rowIndex < rows.Count - 1
                    ? FindClosestHorizontalButton(rows[rowIndex + 1], keyX)
                    : null;
                button.navigation = navigation;
            }
        }
    }

    private Vector2 GetKeyboardLocalPosition(Transform target)
    {
        return keyboardPanel.transform.InverseTransformPoint(target.position);
    }

    private Button FindClosestHorizontalButton(List<KeyboardKeyButton> row, float targetX)
    {
        KeyboardKeyButton closest = null;
        float closestDistance = float.MaxValue;

        foreach (KeyboardKeyButton keyButton in row)
        {
            float distance = Mathf.Abs(GetKeyboardLocalPosition(keyButton.transform).x - targetX);
            if (distance < closestDistance)
            {
                closest = keyButton;
                closestDistance = distance;
            }
        }

        return closest != null ? closest.GetComponent<Button>() : null;
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
        if (activeKeyboard == this) activeKeyboard = null;

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
        if (activeKeyboard == this) activeKeyboard = null;
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
