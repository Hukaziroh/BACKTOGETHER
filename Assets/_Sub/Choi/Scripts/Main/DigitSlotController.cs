using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 신형 입력 시스템 필수

public class DigitSlotController : MonoBehaviour
{
    public int slotIndex;
    public SixDigitCodeInputUI mainUI;
    public UIButtonFeedback upFeedback;
    public UIButtonFeedback downFeedback;

    void Update()
    {
        // 현재 이 오브젝트가 선택되어 있을 때만 입력 받음
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            // 1. 값 증가 조건 판정 (키보드: Up/W | 패드: 십자키 위 / 왼쪽 스틱 위)
            bool upPressed = false;
            if (Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
                upPressed = true;
            if (Gamepad.current != null && (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame))
                upPressed = true;

            // 2. 값 감소 조건 판정 (키보드: Down/S | 패드: 십자키 아래 / 왼쪽 스틱 아래)
            bool downPressed = false;
            if (Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame))
                downPressed = true;
            if (Gamepad.current != null && (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame))
                downPressed = true;

            // 실행
            if (upPressed)
            {
                mainUI.ChangeValue(slotIndex, 1);
                upFeedback.PlayPressEffect();
            }
            else if (downPressed)
            {
                mainUI.ChangeValue(slotIndex, -1);
                downFeedback.PlayPressEffect();
            }
        }
    }
}