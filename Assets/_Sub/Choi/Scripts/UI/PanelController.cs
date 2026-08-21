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

        if (useEscapeKey && escapePressed && (escapeTargetPanel != null || IsAnyKeyGuidePanel(gameObject.name)))
        {
            TransitionToEscapePanel();
            return;
        }

        // 2. 우측 넘기기
        bool rightPressed = false;
        if (Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)) rightPressed = true;
        if (Gamepad.current != null && (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame)) rightPressed = true;

        // 3. 좌측 넘기기
        bool leftPressed = false;
        if (Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)) leftPressed = true;
        if (Gamepad.current != null && (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftShoulder.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame)) leftPressed = true;

        // 패널 전환 실행
        if (useRightKey && rightPressed && nextPanel != null)
        {
            if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();
            TransitionToPanel(nextPanel, -slideOffset, fromRight: true);
        }
        else if (useLeftKey && leftPressed && prevPanel != null)
        {
            if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();
            TransitionToPanel(prevPanel, slideOffset, fromRight: false);
        }
    }

    private bool IsAnyKeyGuidePanel(string objName)
    {
        return objName.Contains("Keyboard") || objName.Contains("XBOX") ||
               objName.Contains("Switch") || objName.Contains("PlayStation") ||
               objName.Contains("KeyGuide");
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
                bool isKeyGuide = IsAnyKeyGuidePanel(gameObject.name);

                // [핵심] 키 가이드 패널들 중 하나이고 존 트리거로 열렸던 경우, 옵션도 안 띄우고 퍼즈도 안 띄우고 완전 차단 후 종료
                if (isKeyGuide && OptionsManager.instance != null && OptionsManager.instance.isOpenedFromZone)
                {
                    gameObject.SetActive(false);
                    OptionsManager.instance.isOpenedFromZone = false;
                    OptionsManager.instance.blockPauseUntilTime = Time.unscaledTime + 0.25f;

                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.ClearFocusScope();
                    }
                    return;
                }

                if (escapeTargetPanel != null)
                {
                    escapeTargetPanel.SetActive(true);

                    if (escapeTargetPanel.name.Contains("Option"))
                    {
                        GameObject pauseObj = GameObject.Find("PausePanel");
                        if (pauseObj != null) pauseObj.SetActive(false);
                    }

                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.SetFocusScope(escapeTargetPanel);
                    }
                }
                else
                {
                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.ClearFocusScope();
                    }
                }

                gameObject.SetActive(false);
            });
    }

    private void ApplyFocusScope()
    {
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(gameObject);
        }
    }
}
