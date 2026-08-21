using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PlayerControls; // 만들어둔 뉴 인풋 네임스페이스 추가

public class EmojiRadialMenu : MonoBehaviour
{
    public static EmojiRadialMenu Instance { get; private set; }
    public static event Action<int> OnEmojiIndexSelected;
    public static event Action<int> OnAnimatedEmojiIndexSelected; // 애니메이션 이모지 선택 이벤트 추가

    [System.Serializable]
    public class MenuItem
    {
        public Sprite icon;                   // 메뉴에 표시될 대표 아이콘
        public bool isAnimated;               // 애니메이션 재생 여부
        public Sprite[] animationFrames;      // 애니메이션일 때 순차적으로 보여줄 스프라이트 배열
    }

    [Header("Items")]
    [SerializeField] private MenuItem[] items;

    [Header("Layout")]
    [SerializeField] private float innerRadius = 100f;
    [SerializeField] private float outerRadius = 250f;
    [SerializeField] private float gapAngle = 4f;
    [SerializeField] private float iconSize = 60f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.25f, 0.85f);
    [SerializeField] private Color highlightColor = new Color(0.35f, 0.35f, 0.75f, 0.95f);

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.2f;

    // ── 런타임 ──────────────────────────────────────────
    private readonly List<PieSlice> _slices = new();
    private readonly List<Image> _icons = new();
    private int _selectedIndex = -1;
    private int _prevSelectedIndex = -1;
    private int _slotCount;
    private float _stepAngle;
    private bool _isOpen;
    private bool _isBuilt;

    // 🌟 뉴 인풋 시스템용 변수
    private PlayerControls.PlayerControls _inputControls;
    private bool _prevMovingLeft;
    private bool _prevMovingRight;

    // 꾹 누름 연속 입력(Hold-to-repeat) 관련 변수
    private float _keyRepeatTimer;
    private bool _isKeyHeld;
    private const float InitialRepeatDelay = 0.3f; // 처음 꾹 누를 때 대기 시간
    private const float RepeatInterval = 0.15f;    // 연속 이동 주기

    public int SelectedIndex => _selectedIndex;
    public bool IsOpen() => _isOpen;
    public event Action<int> OnSelected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 🌟 뉴 인풋 컨트롤 초기화
        _inputControls = new PlayerControls.PlayerControls();

        Build();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CloseMenuWithoutSelection();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        CloseMenuWithoutSelection();

        // 🌟 메모리 누수 방지
        _inputControls?.Dispose();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        CloseMenuWithoutSelection();
    }

    private void Build()
    {
        if (_isBuilt) return;
        _isBuilt = true;

        _slotCount = items.Length;
        if (_slotCount == 0) return;

        _stepAngle = 360f / _slotCount;

        for (int i = 0; i < _slotCount; i++)
        {
            CreateSlice(i);
            CreateIcon(i);
        }
    }

    private void CreateSlice(int index)
    {
        float menuStart = index * _stepAngle - _stepAngle / 2f + gapAngle / 2f;
        float menuEnd = index * _stepAngle + _stepAngle / 2f - gapAngle / 2f;

        float mathStart = 90f - menuEnd;
        float mathEnd = 90f - menuStart;

        var go = new GameObject($"Slice_{index}");
        go.transform.SetParent(transform, false);

        var slice = go.AddComponent<PieSlice>();
        slice.raycastTarget = false;
        slice.Setup(mathStart, mathEnd, innerRadius, outerRadius);
        slice.color = normalColor;

        _slices.Add(slice);
    }

    private void CreateIcon(int index)
    {
        float menuCenter = index * _stepAngle;
        float mathCenter = (90f - menuCenter) * Mathf.Deg2Rad;
        float midRadius = (innerRadius + outerRadius) / 2f;

        var go = new GameObject($"Icon_{index}");
        go.transform.SetParent(transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(
            Mathf.Cos(mathCenter) * midRadius,
            Mathf.Sin(mathCenter) * midRadius
        );
        rect.sizeDelta = new Vector2(iconSize, iconSize);

        var img = go.AddComponent<Image>();
        img.sprite = items[index].icon;
        img.raycastTarget = false;
        img.preserveAspect = true;

        _icons.Add(img);
    }

    public void OpenMenu()
    {
        if (Time.timeScale == 0) return; // 타임스케일이 0일 때 메뉴 오픈 차단
        if (_isOpen) return;
        _isOpen = true;

        // 🌟 메뉴가 열릴 때 인풋 감지 활성화 및 상태 초기화
        _inputControls.GamePlay.Enable();
        _prevMovingLeft = false;
        _prevMovingRight = false;

        _selectedIndex = (_slotCount > 0) ? 0 : -1;
        _prevSelectedIndex = -1;
        _isKeyHeld = false;

        UpdateVisuals();

        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(Vector3.one, openDuration).SetEase(Ease.OutBack);
    }

    public int CloseMenu()
    {
        if (!_isOpen) return -1;
        _isOpen = false;

        // 🌟 메뉴가 닫힐 때 인풋 감지 비활성화
        _inputControls.GamePlay.Disable();

        transform.DOKill();
        gameObject.SetActive(false);
        transform.localScale = Vector3.one;

        int result = _selectedIndex;
        if (result >= 0 && result < items.Length)
        {
            var selectedItem = items[result];

            // 애니메이션 이모지인지 일반 이모지인지 판별 후 적절한 이벤트 호출
            if (selectedItem.isAnimated)
            {
                // 전체 아이템 중 몇 번째 애니메이션 이모지인지 계산 (PlayerEmojiController의 Animated Emojis 인덱스와 맞춤)
                int animatedIndex = 0;
                for (int i = 0; i < result; i++)
                {
                    if (items[i].isAnimated)
                    {
                        animatedIndex++;
                    }
                }
                OnAnimatedEmojiIndexSelected?.Invoke(animatedIndex);
            }
            else
            {
                OnSelected?.Invoke(result);
                OnEmojiIndexSelected?.Invoke(result);
            }
        }

        return result;
    }

    private void CloseMenuWithoutSelection()
    {
        if (!_isOpen && !gameObject.activeSelf) return;

        _isOpen = false;
        _inputControls?.GamePlay.Disable();

        transform.DOKill();
        transform.localScale = Vector3.one;
        ResetVisuals();

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void OnMenuUpdate()
    {
        if (Time.timeScale == 0) return;
        UpdateSelection();

        if (_selectedIndex != _prevSelectedIndex)
        {
            UpdateVisuals();
            _prevSelectedIndex = _selectedIndex;
        }
    }

    // 좌우 키(스틱) 순차 이동 및 꾹 누름 반복 입력 처리
    private void UpdateSelection()
    {
        if (_slotCount == 0) return;

        // 🌟 뉴 인풋 시스템에서 좌우 아날로그/방향키 값 읽어오기
        float moveInput = _inputControls.GamePlay.Move.ReadValue<float>();

        // 데드존(0.3) 설정: 스틱을 어느 정도 기울여야 인식되도록
        bool isMovingLeft = moveInput < -0.3f;
        bool isMovingRight = moveInput > 0.3f;

        // 이전 프레임 상태와 비교하여 "이번 프레임에 막 누름(wasPressed)" 판정 구현
        bool leftPressed = isMovingLeft && !_prevMovingLeft;
        bool rightPressed = isMovingRight && !_prevMovingRight;

        bool leftHeld = isMovingLeft;
        bool rightHeld = isMovingRight;

        // 다음 프레임 비교를 위해 상태 저장
        _prevMovingLeft = isMovingLeft;
        _prevMovingRight = isMovingRight;

        // 기존의 이동 및 꾹 누름 로직
        if (leftPressed || rightPressed)
        {
            if (leftPressed && !rightPressed) MoveIndex(-1);
            else if (rightPressed && !leftPressed) MoveIndex(1);

            _keyRepeatTimer = InitialRepeatDelay;
            _isKeyHeld = true;
        }
        else if (_isKeyHeld && (leftHeld || rightHeld))
        {
            _keyRepeatTimer -= Time.unscaledDeltaTime;
            if (_keyRepeatTimer <= 0f)
            {
                if (leftHeld && !rightHeld) MoveIndex(-1);
                else if (rightHeld && !leftHeld) MoveIndex(1);

                _keyRepeatTimer = RepeatInterval;
            }
        }
        else
        {
            _isKeyHeld = false;
        }
    }

    // 인덱스를 순차적으로 증감시키고 끝과 끝을 연결(Loop)
    private void MoveIndex(int direction)
    {
        if (_selectedIndex == -1)
        {
            _selectedIndex = 0;
            return;
        }

        _selectedIndex += direction;

        if (_selectedIndex < 0)
        {
            _selectedIndex = _slotCount - 1; // 첫 번째에서 왼쪽으로 가면 마지막으로 이동
        }
        else if (_selectedIndex >= _slotCount)
        {
            _selectedIndex = 0;              // 마지막에서 오른쪽으로 가면 첫 번째로 이동
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < _slices.Count; i++)
            _slices[i].color = (i == _selectedIndex) ? highlightColor : normalColor;
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < _slices.Count; i++)
            _slices[i].color = normalColor;
    }
}
