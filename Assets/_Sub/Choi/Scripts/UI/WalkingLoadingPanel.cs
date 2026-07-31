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
    [Tooltip("모든 캐릭터가 공통으로 사용할 걸음마 이미지 3개 (프레임 1, 2, 3)")]
    [SerializeField] private Sprite[] sharedFrames = new Sprite[3];

    [Header("캐릭터 이미지 리스트 (총 4명)")]
    [Tooltip("화면에 배치할 4개의 캐릭터 UI Image 컴포넌트 (맨 앞부터 순서대로)")]
    [SerializeField] private List<Image> characterImages = new List<Image>();

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
        ApplyCharacterColors();
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
        // 1. 현재 진행률에서 정확히 1.0f(100%)까지 완주
        while (currentProgress < 1.0f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 1.0f, Time.unscaledDeltaTime * finishFillSpeed);
            SetProgress(currentProgress);
            yield return null; // 매 프레임 UI 갱신
        }

        // 2. 명시적 100% 고정
        SetProgress(1.0f);

        // 3. 사용자가 100% 연출을 확인할 수 있도록 최소한의 시간 동안 화면 유지 후 닫기
        yield return new WaitForSecondsRealtime(0.15f);

        gameObject.SetActive(false);
    }

    private void Start()
    {
        ApplyCharacterColors();
    }

    private void ApplyCharacterColors()
    {
        for (int i = 0; i < characterImages.Count; i++)
        {
            if (characterImages[i] != null && i < characterColors.Count)
            {
                Color col = characterColors[i];
                col.a = 1f;
                characterImages[i].color = col;
            }
        }
    }

    private void Update()
    {
        // 1. 걸음마 애니메이션
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

    private void UpdateSpriteFrames()
    {
        if (sharedFrames == null || sharedFrames.Length < 3) return;

        foreach (var img in characterImages)
        {
            if (img != null && sharedFrames[currentFrameIndex] != null)
            {
                img.sprite = sharedFrames[currentFrameIndex];
            }
        }
    }

    private void UpdateCharacterVisuals(float progress)
    {
        if (barArea == null || characterImages == null || characterImages.Count == 0) return;

        float barWidth = barArea.rect.width;
        int totalChars = characterImages.Count;

        float minX = -barWidth * 0.5f;
        float maxX = barWidth * 0.5f;
        float frontX = Mathf.Lerp(minX, maxX, progress);

        for (int i = 0; i < totalChars; i++)
        {
            Image charImg = characterImages[i];
            if (charImg == null) continue;

            float appearThreshold = (float)i / totalChars;

            if (progress >= appearThreshold || (progress > 0f && i == 0))
            {
                charImg.gameObject.SetActive(true);

                RectTransform rt = charImg.rectTransform;
                Vector2 anchoredPos = rt.anchoredPosition;

                float targetX = frontX - (i * characterSpacing);
                targetX = Mathf.Max(minX, targetX);

                rt.anchoredPosition = new Vector2(targetX, anchoredPos.y);

                Color col = (i < characterColors.Count) ? characterColors[i] : Color.white;
                col.a = 1f;
                charImg.color = col;
            }
            else
            {
                charImg.gameObject.SetActive(false);
            }
        }
    }
}