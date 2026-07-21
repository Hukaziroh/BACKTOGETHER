using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// System.Drawing과의 충돌(모호한 참조)을 방지합니다.
using Color = UnityEngine.Color;

public class EmojiRadialMenu : MonoBehaviour
{
    public static EmojiRadialMenu Instance;

    // --- 이벤트 정의 (선택된 이모지 스프라이트를 전달) ---
    public static event System.Action<Sprite> OnEmojiSelected;

    [Header("UI 연결 - 배경 조각 (하이라이트용)")]
    [Tooltip("이모티콘 선택 패널 UI 오브젝트")]
    public GameObject radialPanel;
    [Tooltip("방사형 메뉴에 들어갈 바깥쪽 조각(배경) 이미지 리스트 (시계방향)")]
    public List<Image> menuItemImages;
    [Tooltip("가장 중앙에 있는 배경/테두리 이미지")]
    [SerializeField] private Image centerItemImage;

    [Header("UI 연결 - 실제 이모지 아이콘 이미지 컴포넌트들")]
    [Tooltip("12시 방향부터 시계 방향 순서대로 배치된 이모지들의 UI Image 컴포넌트")]
    public List<Image> emojiIconImages;
    [Tooltip("가장 중앙 아이콘의 UI Image 컴포넌트")]
    [SerializeField] private Image centerEmojiIconImage;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private int currentIndex = -1;
    private bool isCenterSelected = false;
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

    // --- UIManager(관제탑)로부터 받는 신호 메서드들 ---

    public void OpenMenu()
    {
        isOpen = true;
        if (radialPanel != null)
        {
            radialPanel.SetActive(true);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 메뉴가 열릴 때는 기본적으로 아무것도 선택되지 않은 깨끗한 상태로 시작합니다.
        currentIndex = -1;
        isCenterSelected = false;
        ResetHighlights();
    }

    public void OnMenuStay()
    {
        if (!isOpen) return;

        CalculateSelectedSectorByRaycast();

        // New Input System을 이용한 마우스 좌클릭 감지
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectCurrentItem();
            CloseMenu();
        }
    }

    public void CloseMenu()
    {
        if (!isOpen) return;

        SelectCurrentItem(); // 키를 뗄 때 선택 실행
        isOpen = false;

        if (radialPanel != null)
        {
            radialPanel.SetActive(false);
        }
        currentIndex = -1;
        isCenterSelected = false;
        ResetHighlights();
    }

    // --- 내부 연산 로직 (UI Raycast 방식) ---

    private void CalculateSelectedSectorByRaycast()
    {
        if (EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool hitCenter = false;
        int hitIndex = -1;
        bool hitAnyPanelItem = false;

        foreach (var result in results)
        {
            Image hitImage = result.gameObject.GetComponent<Image>();
            if (hitImage == null) continue;

            // 1. 중앙 선택 확인 (배경 또는 아이콘)
            if ((centerItemImage != null && hitImage == centerItemImage) ||
                (centerEmojiIconImage != null && hitImage == centerEmojiIconImage))
            {
                hitCenter = true;
                hitAnyPanelItem = true;
                break;
            }

            // 2. 바깥쪽 배경 조각 확인
            int bgIndex = menuItemImages.IndexOf(hitImage);
            if (bgIndex != -1)
            {
                hitIndex = bgIndex;
                hitAnyPanelItem = true;
                break;
            }

            // 3. 바깥쪽 아이콘 이미지 확인
            int iconIndex = emojiIconImages.IndexOf(hitImage);
            if (iconIndex != -1)
            {
                hitIndex = iconIndex;
                hitAnyPanelItem = true;
                break;
            }
        }

        // 마우스가 패널 안쪽(조각이나 중앙)에 있을 때만 실시간으로 선택을 변경합니다.
        if (hitAnyPanelItem)
        {
            if (hitCenter)
            {
                isCenterSelected = true;
                currentIndex = -1;
                UpdateHighlight(-1);
            }
            else if (hitIndex != -1)
            {
                isCenterSelected = false;
                currentIndex = hitIndex;
                UpdateHighlight(hitIndex);
            }
        }
        // 마우스가 패널 바깥으로 나가면(hitAnyPanelItem == false), 
        // 코드가 이 블록을 타지 않으므로 직전에 선택되어 있던 조각의 하이라이트 상태가 그대로 유지됩니다!
    }

    private void UpdateHighlight(int selectedIndex)
    {
        // 1. 바깥쪽 조각들 하이라이트 처리
        for (int i = 0; i < menuItemImages.Count; i++)
        {
            if (menuItemImages[i] != null)
            {
                if (!isCenterSelected && i == selectedIndex)
                    menuItemImages[i].color = highlightColor;
                else
                    menuItemImages[i].color = normalColor;
            }
        }

        // 2. 중앙 아이콘 하이라이트 처리
        if (centerItemImage != null)
        {
            if (isCenterSelected)
                centerItemImage.color = highlightColor;
            else
                centerItemImage.color = normalColor;
        }
    }

    private void ResetHighlights()
    {
        foreach (var img in menuItemImages)
        {
            if (img != null) img.color = normalColor;
        }
        if (centerItemImage != null)
        {
            centerItemImage.color = normalColor;
        }
    }

    private void SelectCurrentItem()
    {
        if (isCenterSelected)
        {
            Debug.Log("중앙 아이콘 선택됨!");
            if (centerEmojiIconImage != null && centerEmojiIconImage.sprite != null)
            {
                OnEmojiSelected?.Invoke(centerEmojiIconImage.sprite);
            }
        }
        else if (currentIndex != -1 && currentIndex < emojiIconImages.Count)
        {
            Debug.Log($"선택된 이모티콘 번호: {currentIndex}");
            if (emojiIconImages[currentIndex] != null && emojiIconImages[currentIndex].sprite != null)
            {
                Sprite selectedSprite = emojiIconImages[currentIndex].sprite;
                OnEmojiSelected?.Invoke(selectedSprite);
            }
        }
    }
}