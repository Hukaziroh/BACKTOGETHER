using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WalkingLoadingPanel : MonoBehaviour
{
    public static WalkingLoadingPanel Instance { get; private set; }

    [Header("UI 연결")]
    [Tooltip("로딩바 이미지 (Image Type = Filled, Fill Method = Horizontal 로 설정)")]
    [SerializeField] private Image progressBar;

    [Tooltip("캐릭터들이 움직일 전체 로딩바 영역의 RectTransform")]
    [SerializeField] private RectTransform barArea;

    [Tooltip("로딩 글자 또는 퍼센트를 표시할 텍스트 컴포넌트")]
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("공통 걸음마 스프라이트 (정확히 3장)")]
    [Tooltip("모든 캐릭터가 공통으로 사용할 걸음마 이미지 3개 (프레임 1, 2, 3) - 전체 움직임 애니메이션용")]
    [SerializeField] private Sprite[] sharedFrames = new Sprite[3];

    [Header("캐릭터별 아이템 리스트 (총 4명)")]
    [Tooltip("화면에 배치할 4개의 서로 다른 캐릭터 UI 루트 오브젝트 (내부에 BackIcon과 Icon이 있어야 함)")]
    [SerializeField] private List<RectTransform> characterItems = new List<RectTransform>();

    [Header("캐릭터 아이콘 표정 스프라이트 설정")]
    [Tooltip("플레이어 순서(Index)에 따라 Icon에 적용할 표정 스프라이트 리스트입니다. (0번, 1번, 2번, 3번...)")]
    [SerializeField] private List<Sprite> characterIconSprites = new List<Sprite>();

    [Header("캐릭터 색상 (틴트)")]
    [Tooltip("0번(기본 캐릭): 흰색(원본), 1번: 빨강, 2번: 노랑, 3번: 파랑")]
    [SerializeField]
    private List<Color> characterColors = new List<Color>
    {
        Color.white,   // 0번: 기본 캐릭 (원본 색상 유지)
        Color.red,     // 1번: 빨강
        Color.yellow,  // 2번: 노랑
        Color.blue     // 3번: 파랑
    };

    [Header("Animation Settings")]
    [Tooltip("캐릭터 걸음마 프레임 전환 속도 (초 단위)")]
    [SerializeField] private float frameInterval = 0.15f;
    [Tooltip("뒤따라오는 캐릭터들 사이의 간격 (픽셀)")]
    [SerializeField] private float characterSpacing = 35f;

    [Header("진행률 연출 설정")]
    [Tooltip("대기 중일 때 목표 지점(90%)까지 도달하는 속도")]
    [SerializeField] private float smoothApproachSpeed = 4.0f;
    [Tooltip("씬 전환 완료 후 100%까지 채워지는 마무리 속도")]
    [SerializeField] private float finishFillSpeed = 3.0f;

    private float currentProgress = 0f;
    private float animTimer = 0f;
    private int currentFrameIndex = 0;
    private bool isFinished = false;
    private Coroutine finishCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        currentProgress = 0f;
        SetProgress(0f);
        ApplyCharacterVisualSettings();
        isFinished = false;

        if (finishCoroutine != null)
        {
            StopCoroutine(finishCoroutine);
            finishCoroutine = null;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (finishCoroutine != null)
        {
            StopCoroutine(finishCoroutine);
            finishCoroutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isFinished) return;
        isFinished = true;

        if (finishCoroutine != null) StopCoroutine(finishCoroutine);
        finishCoroutine = StartCoroutine(FinishAndCloseRoutine());
    }

    // 100% 달성 및 시각적 연출 보장 코루틴
    private IEnumerator FinishAndCloseRoutine()
    {
        while (currentProgress < 1.0f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 1.0f, Time.unscaledDeltaTime * finishFillSpeed);
            SetProgress(currentProgress);
            yield return null;
        }

        SetProgress(1.0f);
        yield return new WaitForSecondsRealtime(0.15f);

        gameObject.SetActive(false);
    }

    private void Start()
    {
        ApplyCharacterVisualSettings();
    }

    private void ApplyCharacterVisualSettings()
    {
        for (int i = 0; i < characterItems.Count; i++)
        {
            if (characterItems[i] == null) continue;

            // 1. BackIcon 색상 적용
            Transform backIconChild = characterItems[i].Find("BackIcon");
            if (backIconChild != null)
            {
                Image backImg = backIconChild.GetComponent<Image>();
                if (backImg != null && i < characterColors.Count)
                {
                    Color col = characterColors[i];
                    col.a = 1f;
                    backImg.color = col;
                }
            }

            // 2. Icon 표정 스프라이트 적용 (인덱스 기준)
            Transform iconChild = characterItems[i].Find("Icon");
            if (iconChild != null)
            {
                Image iconImg = iconChild.GetComponent<Image>();
                if (iconImg != null && characterIconSprites != null && i < characterIconSprites.Count)
                {
                    Sprite targetSprite = characterIconSprites[i];
                    if (targetSprite != null)
                    {
                        iconImg.sprite = targetSprite;
                        Color iconColor = iconImg.color;
                        iconColor.a = 1f;
                        iconImg.color = iconColor;
                    }
                }
            }
        }
    }

    private void Update()
    {
        // 1. 걸음마 애니메이션 (공통 스프라이트 프레임 전환)
        animTimer += Time.unscaledDeltaTime;
        if (animTimer >= frameInterval)
        {
            animTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % 3;
            UpdateSpriteFrames();
        }

        // 2. 씬 전환 완료 전에는 최대 90%까지만 차오르도록 대기
        if (!isFinished)
        {
            currentProgress = Mathf.Lerp(currentProgress, 1f, Time.unscaledDeltaTime * smoothApproachSpeed);
            SetProgress(currentProgress);
        }
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.fillAmount = currentProgress;
        }

        if (loadingText != null)
        {
            int percent = Mathf.RoundToInt(currentProgress * 100f);
            loadingText.text = $"Loading... {percent}%";
        }

        UpdateCharacterVisuals(currentProgress);
    }

    // 🌟 수정된 부분: 걸음마 애니메이션 프레임(Shared Frames)을 BackIcon에 반영
    private void UpdateSpriteFrames()
    {
        if (sharedFrames == null || sharedFrames.Length < 3) return;

        foreach (var item in characterItems)
        {
            if (item == null) continue;

            Transform backIconChild = item.Find("BackIcon");
            if (backIconChild != null)
            {
                Image backImg = backIconChild.GetComponent<Image>();
                if (backImg != null && sharedFrames[currentFrameIndex] != null)
                {
                    backImg.sprite = sharedFrames[currentFrameIndex];
                }
            }
        }
    }

    private void UpdateCharacterVisuals(float progress)
    {
        if (barArea == null || characterItems == null || characterItems.Count == 0) return;

        float barWidth = barArea.rect.width;
        int totalChars = characterItems.Count;

        float minX = -barWidth * 0.5f;
        float maxX = barWidth * 0.5f;
        float frontX = Mathf.Lerp(minX, maxX, progress);

        for (int i = 0; i < totalChars; i++)
        {
            RectTransform rt = characterItems[i];
            if (rt == null) continue;

            float appearThreshold = (float)i / totalChars;

            if (progress >= appearThreshold || (progress > 0f && i == 0))
            {
                rt.gameObject.SetActive(true);

                Vector2 anchoredPos = rt.anchoredPosition;
                float targetX = frontX - (i * characterSpacing);
                targetX = Mathf.Max(minX, targetX);

                rt.anchoredPosition = new Vector2(targetX, anchoredPos.y);
            }
            else
            {
                rt.gameObject.SetActive(false);
            }
        }
    }
}