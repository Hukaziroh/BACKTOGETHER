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

    private int currentIndex = 0;
    private bool isOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (radialPanel != null)
            radialPanel.SetActive(false);
    }

    // --- UIManager 호환용 메서드들 ---
    public bool IsOpen()
    {
        return isOpen;
    }

    public void ToggleMenu()
    {
        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        isOpen = true;
        if (radialPanel != null)
        {
            radialPanel.SetActive(true);
        }

        // 메뉴가 열릴 때 첫 번째 항목부터 하이라이트 상태로 시작
        currentIndex = 0;
        UpdateHighlight(currentIndex);
    }

    public void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (radialPanel != null)
        {
            radialPanel.SetActive(false);
        }
        ResetHighlights();
    }

    // --- UIManager(관제탑)로부터 매 프레임 호출되는 키보드 입력 처리 메서드 ---
    public void OnMenuUpdate()
    {
        if (!isOpen || Keyboard.current == null) return;

        int totalCount = emojiIconImages != null && emojiIconImages.Count > 0
            ? emojiIconImages.Count
            : (menuItemImages != null ? menuItemImages.Count : 0);

        if (totalCount == 0) return;

        // 1. ESC 키 -> 메뉴 닫기 (취소)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
            return;
        }

        // 2. A 키 또는 왼쪽 방향키 -> 이전 항목으로 이동 (반시계/왼쪽)
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex - 1 + totalCount) % totalCount;
            UpdateHighlight(currentIndex);
        }

        // 3. D 키 또는 오른쪽 방향키 -> 다음 항목으로 이동 (시계/오른쪽)
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex + 1) % totalCount;
            UpdateHighlight(currentIndex);
        }

        // 4. Enter 키 -> 현재 선택한 항목 확정
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SelectCurrentItem();
            CloseMenu();
        }
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