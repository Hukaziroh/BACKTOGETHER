using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// System.Drawing과의 충돌(모호한 참조)을 방지합니다.
using Color = UnityEngine.Color;

public class EmojiRadialMenu : MonoBehaviour
{
    public static EmojiRadialMenu Instance;

    // --- 이벤트 정의 ---
    public static event System.Action<Sprite> OnEmojiSelected;
    public static event System.Action<int> OnEmojiIndexSelected; // PlayerMovement 네트워크 동기화 호환용

    [Header("UI 연결 - 메뉴 패널 및 조각")]
    [Tooltip("이모티콘 선택 패널 UI 오브젝트")]
    public GameObject radialPanel;
    [Tooltip("방사형 메뉴에 들어갈 바깥쪽 조각(배경) 이미지 리스트 (시계방향)")]
    public List<Image> menuItemImages;

    [Header("UI 연결 - 실제 이모지 아이콘 이미지 컴포넌트들")]
    [Tooltip("시계 방향 순서대로 배치된 이모지들의 UI Image 컴포넌트")]
    public List<Image> emojiIconImages;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("지속 이동 설정 (홀드)")]
    [Tooltip("키를 꾹 누르고 있을 때 다음 항목으로 이동하는 간격 (초 단위)")]
    public float holdInterval = 0.2f;
    private float holdTimer = 0.0f;
    private int holdDirection = 0; // -1: 왼쪽, 1: 오른쪽, 0: 없음

    private int currentIndex = 0;
    private bool isOpen = false;

    // --- 좌우 반전 방지용 부모 기준 스케일 저장 ---
    private Vector3 originalPanelScale;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (radialPanel != null)
        {
            originalPanelScale = radialPanel.transform.localScale;
        }
    }

    void Start()
    {
        if (radialPanel != null)
            radialPanel.SetActive(false);
    }

    // --- ★ [추가] 부모(플레이어)가 좌우 반전되더라도 UI가 반전되지 않고 일정한 방향을 유지하도록 처리 ---
    void LateUpdate()
    {
        if (radialPanel == null || !radialPanel.activeSelf) return;

        // 부모가 있더라도 부모의 localScale(양수/음수)에 상관없이
        // 월드 기준 항상 '양수(정방향)' 스케일이 유지되도록 강제 고정합니다.
        Vector3 currentLossyScale = radialPanel.transform.lossyScale;

        // 스케일의 부호가 음수(-1)라면 즉시 양수(+1)로 되돌림
        if (radialPanel.transform.parent != null)
        {
            Vector3 parentScale = radialPanel.transform.parent.lossyScale;
            radialPanel.transform.localScale = new Vector3(
                Mathf.Abs(originalPanelScale.x) * (parentScale.x < 0 ? -1f : 1f),
                originalPanelScale.y,
                originalPanelScale.z
            );
        }
    }

    // --- UIManager 호환용 메서드들 ---
    public bool IsOpen()
    {
        return isOpen;
    }

    // --- ★ [변경] 꾹 누르는(Hold) 방식을 위한 토글/열기 제어 ---
    // 외부에서 키를 누르기 시작할 때 OpenMenu(), 뗄 때 CloseMenu()를 호출하도록 연동하세요.
    public void OpenMenu()
    {
        if (isOpen) return;
        isOpen = true;
        if (radialPanel != null)
        {
            radialPanel.SetActive(true);
        }

        currentIndex = 0;
        UpdateHighlight(currentIndex);
        holdTimer = 0f;
        holdDirection = 0;
    }

    public void CloseMenu()
    {
        if (!isOpen) return;

        // 닫히기 직전 현재 선택된 항목 확정 발송
        SelectCurrentItem();

        isOpen = false;

        if (radialPanel != null)
        {
            radialPanel.SetActive(false);
        }
        ResetHighlights();
        holdDirection = 0;
    }

    // --- UIManager(관제탑)로부터 매 프레임 호출되는 키보드 입력 처리 메서드 ---
    public void OnMenuUpdate()
    {
        if (!isOpen || Keyboard.current == null) return;

        int totalCount = emojiIconImages != null && emojiIconImages.Count > 0
            ? emojiIconImages.Count
            : (menuItemImages != null ? menuItemImages.Count : 0);

        if (totalCount == 0) return;

        // 1. ESC 키 -> 메뉴 취소 및 닫기 (선택 안 함)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isOpen = false;
            if (radialPanel != null) radialPanel.SetActive(false);
            ResetHighlights();
            return;
        }

        // 2. 입력 방향 감지 (누르기 시작 / 꾹 누를 때 지속 이동)
        int currentInputDirection = 0;

        bool leftPressed = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;
        bool rightPressed = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
        bool leftDown = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
        bool rightDown = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;

        if (leftPressed) currentInputDirection = -1;
        else if (rightPressed) currentInputDirection = 1;

        // 방향이 바뀌었거나 처음 눌렀을 때 즉시 반응
        if (leftDown)
        {
            MoveIndex(-1, totalCount);
            holdDirection = -1;
            holdTimer = 0f;
        }
        else if (rightDown)
        {
            MoveIndex(1, totalCount);
            holdDirection = 1;
            holdTimer = 0f;
        }
        else if (currentInputDirection != 0)
        {
            // 꾹 누르고 있을 때의 지속 이동 처리
            if (holdDirection == currentInputDirection)
            {
                holdTimer += Time.unscaledDeltaTime;
                if (holdTimer >= holdInterval)
                {
                    MoveIndex(holdDirection, totalCount);
                    holdTimer = 0f; // 타이머 초기화 후 연속 이동
                }
            }
            else
            {
                holdDirection = currentInputDirection;
                holdTimer = 0f;
            }
        }
        else
        {
            holdDirection = 0;
            holdTimer = 0f;
        }
    }

    private void MoveIndex(int direction, int totalCount)
    {
        currentIndex = (currentIndex + direction + totalCount) % totalCount;
        UpdateHighlight(currentIndex);
    }

    // --- 하이라이트 시각 효과 업데이트 ---
    private void UpdateHighlight(int selectedIndex)
    {
        for (int i = 0; i < menuItemImages.Count; i++)
        {
            if (menuItemImages[i] != null)
            {
                if (i == selectedIndex)
                    menuItemImages[i].color = highlightColor;
                else
                    menuItemImages[i].color = normalColor;
            }
        }
    }

    private void ResetHighlights()
    {
        foreach (var img in menuItemImages)
        {
            if (img != null) img.color = normalColor;
        }
    }

    // --- 선택 확정 및 이벤트 발송 ---
    private void SelectCurrentItem()
    {
        if (currentIndex >= 0 && currentIndex < emojiIconImages.Count)
        {
            Debug.Log($"선택된 이모티콘 번호: {currentIndex}");
            if (emojiIconImages[currentIndex] != null && emojiIconImages[currentIndex].sprite != null)
            {
                Sprite selectedSprite = emojiIconImages[currentIndex].sprite;

                // 1. 스프라이트 기반 이벤트 호출
                OnEmojiSelected?.Invoke(selectedSprite);

                // 2. 정수형 인덱스 기반 이벤트 호출 (PlayerMovement 네트워크 동기화 연동)
                OnEmojiIndexSelected?.Invoke(currentIndex);
            }
        }
    }
}