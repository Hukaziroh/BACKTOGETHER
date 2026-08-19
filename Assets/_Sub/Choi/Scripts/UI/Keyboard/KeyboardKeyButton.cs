using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class KeyboardKeyButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [HideInInspector] public VirtualKeyboardManager manager;

    public enum KeyType { Char, Space, Backspace, Confirm, None }
    public KeyType keyType = KeyType.Char;
    public string characterValue = "";

    private Button _button;
    private Image _image;
    private Color _originalColor;

    [Header("포커스 시 색상 설정")]
    [SerializeField] private Color selectColor = new Color(1f, 0.92f, 0.016f, 1f); // 눈에 잘 띄는 노란색

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();

        if (_image != null)
        {
            _originalColor = _image.color;
        }

        // 중복 입력(2번 입력) 방지를 위해 코드에서의 추가 리스너 등록을 제거했습니다.
        // (Inspector의 On Click에 이미 등록되어 있으므로 중복 실행을 막습니다.)
    }

    // 패드 포커스를 받았을 때
    public void OnSelect(BaseEventData eventData)
    {
        if (_image != null)
        {
            _image.color = selectColor;
        }
    }

    // 패드 포커스가 해제될 때
    public void OnDeselect(BaseEventData eventData)
    {
        if (_image != null)
        {
            _image.color = _originalColor;
        }
    }

    public void OnClickKey()
    {
        if (manager == null)
        {
            manager = GetComponentInParent<VirtualKeyboardManager>();
        }

        if (manager == null) return;

        switch (keyType)
        {
            case KeyType.Backspace:
                manager.OnClickBackspace();
                break;
            case KeyType.Confirm:
                manager.OnClickConfirm(); // 엔터/확인 버튼 누르면 키보드 닫힘
                break;
            case KeyType.Space:
                manager.InputCharacter(" ");
                break;
            case KeyType.Char:
                if (!string.IsNullOrEmpty(characterValue))
                {
                    manager.InputCharacter(characterValue);
                }
                break;
        }
    }
}