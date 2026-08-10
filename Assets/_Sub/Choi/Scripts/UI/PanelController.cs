using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PanelController : MonoBehaviour
{
    [Header("등장 방향 설정 (기본값)")]
    [SerializeField] private bool slideFromRight = true;

    [Header("우측 이동 설정 (오른쪽 화살표 / D키 -> 다음 패널)")]
    [SerializeField] private bool useRightKey = true;
    [SerializeField] private GameObject nextPanel;

    [Header("좌측 이동 설정 (왼쪽 화살표 / A키 -> 이전 패널)")]
    [SerializeField] private bool useLeftKey = true;
    [SerializeField] private GameObject prevPanel;

    [Header("ESC 키 설정")]
    [SerializeField] private bool useEscapeKey = true;
    [SerializeField] private GameObject escapeTargetPanel;

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

        float startX = slideFromRight ? slideOffset : -slideOffset;
        rectTransform.anchoredPosition = new Vector2(startX, 0f);
        canvasGroup.alpha = 0f;

        // 등장 트윈 실행
        DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutQuad))
            .Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                // 등장 완료 후 GlobalSceneInputManager를 통해 이 패널로 포커스 스코프 설정 및 첫 버튼 선택
                ApplyFocusScope();
            });
    }

    private void Update()
    {
        if (isTransitioning) return;

        // 1. ESC 키 / 게임패드 B(O) 버튼 처리
        bool escapePressed = false;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escapePressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) escapePressed = true;

        if (useEscapeKey && escapePressed && escapeTargetPanel != null)
        {
            TransitionToEscapePanel();
            return;
        }

        // 2. 우측 넘기기 (키보드: 방향키/D키 | 패드: 십자키 우측/RB/왼쪽 스틱 우측)
        bool rightPressed = false;
        if (Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)) rightPressed = true;
        if (Gamepad.current != null && (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame)) rightPressed = true;

        // 3. 좌측 넘기기 (키보드: 방향키/A키 | 패드: 십자키 좌측/LB/왼쪽 스틱 좌측)
        bool leftPressed = false;
        if (Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)) leftPressed = true;
        if (Gamepad.current != null && (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftShoulder.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame)) leftPressed = true;

        // 패널 전환 실행
        if (useRightKey && rightPressed && nextPanel != null)
        {
            TransitionToPanel(nextPanel, -slideOffset, fromRight: true);
        }
        else if (useLeftKey && leftPressed && prevPanel != null)
        {
            TransitionToPanel(prevPanel, slideOffset, fromRight: false);
        }
    }

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
                if (targetPanel.TryGetComponent<PanelController>(out var targetController))
                {
                    targetController.OpenPanel(fromRight);
                }
                else
                {
                    targetPanel.SetActive(true);
                    // PanelController가 없는 일반 패널일 경우에도 GlobalSceneInputManager로 포커스 스코프 지정
                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.SetFocusScope(targetPanel);
                    }
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
                escapeTargetPanel.SetActive(true);

                // ESC로 타겟 패널이 켜질 때 GlobalSceneInputManager에 스코프 요청
                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(escapeTargetPanel);
                }

                gameObject.SetActive(false);
            });
    }

    // GlobalSceneInputManager에 현재 패널을 포커스 스코프로 등록하는 함수
    private void ApplyFocusScope()
    {
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(gameObject);
        }
    }
}