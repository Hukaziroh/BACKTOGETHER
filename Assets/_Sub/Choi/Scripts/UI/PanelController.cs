using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PanelController : MonoBehaviour
{
    [Header("등장 방향 설정 (기본값)")]
    [SerializeField] private bool slideFromRight = true; // 체크: 오른쪽에서 등장, 해제: 왼쪽에서 등장

    [Header("우측 이동 설정 (오른쪽 화살표 / D키)")]
    [SerializeField] private bool useRightKey = true;
    [SerializeField] private GameObject nextPanel;

    [Header("좌측 이동 설정 (왼쪽 화살표 / A키)")]
    [SerializeField] private bool useLeftKey = true;
    [SerializeField] private GameObject prevPanel;

    [Header("ESC 키 설정")]
    [SerializeField] private bool useEscapeKey = false;     // ESC 키 사용 여부
    [SerializeField] private GameObject escapeTargetPanel;// ESC를 눌렀을 때 이동할 특정 패널

    [Header("모션 공통 설정")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float slideOffset = 200f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (!TryGetComponent<CanvasGroup>(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        isTransitioning = false;

        // 설정된 방향에 따라 초기 위치 및 투명도 설정
        float startX = slideFromRight ? slideOffset : -slideOffset;
        rectTransform.anchoredPosition = new Vector2(startX, 0f);
        canvasGroup.alpha = 0f;

        DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutQuad))
            .Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
    }

    private void Update()
    {
        if (isTransitioning) return;
        if (Keyboard.current == null) return;

        // 1. ESC 키 처리 (특정 패널로 포커스 이동 및 현재 패널 비활성화)
        if (useEscapeKey && Keyboard.current.escapeKey.wasPressedThisFrame && escapeTargetPanel != null)
        {
            TransitionToEscapePanel();
            return;
        }

        // 입력 키 체크 (방향키 또는 A/D 키)
        bool rightPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
        bool leftPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;

        // 2. 우측 이동 (오른쪽 화살표 또는 D키) -> 다음 패널이 '오른쪽에서' 등장하도록 설정
        if (useRightKey && rightPressed && nextPanel != null)
        {
            TransitionToPanel(nextPanel, -slideOffset, fromRight: true);
        }
        // 3. 좌측 이동 (왼쪽 화살표 또는 A키) -> 이전 패널이 '왼쪽에서' 등장하도록 설정
        else if (useLeftKey && leftPressed && prevPanel != null)
        {
            TransitionToPanel(prevPanel, slideOffset, fromRight: false);
        }
    }

    // 외부에서 패널을 켤 때 등장 방향을 전달받기 위한 메서드
    public void OpenPanel(bool fromRight)
    {
        slideFromRight = fromRight;
        gameObject.SetActive(true);
    }

    private void TransitionToPanel(GameObject targetPanel, float targetExitX, bool fromRight)
    {
        isTransitioning = true;

        DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(new Vector2(targetExitX, 0f), duration).SetEase(Ease.InQuad))
            .Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                // 다음 패널에 등장 방향(fromRight)을 전달하며 활성화
                if (targetPanel.TryGetComponent<PanelController>(out var targetController))
                {
                    targetController.OpenPanel(fromRight);
                }
                else
                {
                    targetPanel.SetActive(true);
                }

                gameObject.SetActive(false);
            });
    }

    private void TransitionToEscapePanel()
    {
        isTransitioning = true;

        DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                escapeTargetPanel.SetActive(true);  // 특정 패널 켜기
                gameObject.SetActive(false);        // 자기 자신 끄기
            });
    }
}