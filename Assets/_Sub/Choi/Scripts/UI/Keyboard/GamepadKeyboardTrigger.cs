using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class GamepadKeyboardTrigger : MonoBehaviour, ISubmitHandler
{
    [SerializeField] private VirtualKeyboardManager keyboardManager;
    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    // 인풋필드가 선택된 상태에서 엔터(키보드)나 결정 버튼(패드 Submit)을 누를 때만 키보드 오픈
    public void OnSubmit(BaseEventData eventData)
    {
        if (keyboardManager != null && inputField != null)
        {
            keyboardManager.OpenKeyboard(inputField);
        }
    }
}