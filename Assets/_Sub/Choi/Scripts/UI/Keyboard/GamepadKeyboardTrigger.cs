using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class GamepadKeyboardTrigger : MonoBehaviour, ISubmitHandler
{
    [SerializeField] private VirtualKeyboardManager keyboardManager;
    private TMP_InputField inputField;
    private int lastOpenedFrame = -1;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        if (keyboardManager == null)
        {
            keyboardManager = FindAnyObjectByType<VirtualKeyboardManager>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
        if (!WasGamepadSubmitPressedThisFrame() || !IsInputFieldSelected()) return;

        TryOpenKeyboard();
    }

    // 물리 키보드의 Enter는 TMP_InputField가 그대로 처리하고,
    // 게임패드의 Submit 버튼으로 선택했을 때만 가상 키보드를 연다.
    public void OnSubmit(BaseEventData eventData)
    {
        if (!WasGamepadSubmitPressedThisFrame()) return;

        if (TryOpenKeyboard())
        {
            eventData?.Use();
        }
    }

    private bool TryOpenKeyboard()
    {
        if (lastOpenedFrame == Time.frameCount || keyboardManager == null || inputField == null ||
            !inputField.isActiveAndEnabled || keyboardManager.IsOpen)
        {
            return false;
        }

        lastOpenedFrame = Time.frameCount;
        inputField.interactable = true;
        keyboardManager.OpenKeyboard(inputField);
        return true;
    }

    private bool IsInputFieldSelected()
    {
        if (EventSystem.current == null || inputField == null) return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected == inputField.gameObject ||
               (selected != null && selected.transform.IsChildOf(inputField.transform));
    }

    private static bool WasGamepadSubmitPressedThisFrame()
    {
        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }
}
