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
            // 방향키 위 또는 W 키를 누르면 값 증가
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                mainUI.ChangeValue(slotIndex, 1);
                upFeedback.PlayPressEffect();
            }
            // 방향키 아래 또는 S 키를 누르면 값 감소
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                mainUI.ChangeValue(slotIndex, -1);
                downFeedback.PlayPressEffect();
            }
        }
    }
}