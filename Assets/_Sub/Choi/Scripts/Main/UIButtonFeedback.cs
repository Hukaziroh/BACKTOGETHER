using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Image _image;
    private Vector3 _originalScale;
    private Color _originalColor;

    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public float pressDuration = 0.1f; // ★ 추가: 버튼이 눌려있을 시간 (초)

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null) _originalColor = _image.color;
        _originalScale = transform.localScale;
    }

    // 마우스 클릭용
    public void OnPointerDown(PointerEventData eventData) => PlayPressEffect();
    public void OnPointerUp(PointerEventData eventData) => ResetVisuals();
    public void OnPointerExit(PointerEventData eventData) => ResetVisuals();

    // 외부(키보드 입력)에서 호출할 함수
    public void PlayPressEffect()
    {
        // ★ 추가: 연타할 경우를 대비해 기존 예약된 Reset을 취소합니다.
        CancelInvoke("ResetVisuals");

        if (_image != null) _image.color = pressedColor;
        transform.localScale = _originalScale * 0.95f;

        if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();

        // ★ 추가: pressDuration 초 뒤에 ResetVisuals를 실행합니다.
        Invoke("ResetVisuals", pressDuration);
    }

    public void ResetVisuals()
    {
        // 예약된 호출이 있다면 취소 (안전장치)
        CancelInvoke("ResetVisuals");

        if (_image != null) _image.color = _originalColor;
        transform.localScale = _originalScale;
    }
}