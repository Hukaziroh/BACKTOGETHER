using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class WalkingLoadingPanel : MonoBehaviour
{
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

    private float currentProgress = 0f;
    private float animTimer = 0f;
    private int currentFrameIndex = 0;
    private bool isFinished = false;

    private void Awake()
    {
        // 씬이 넘어가도 파괴되지 않고 유지되도록 설정 (최상단 루트 오브젝트여야 함)
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        currentProgress = 0f;
        SetProgress(0f);
        ApplyCharacterColors();
        isFinished = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 100%를 채우고 잠시 뒤 패널을 파괴함
        if (isFinished) return;
        isFinished = true;

        StartCoroutine(FinishAndCloseRoutine());
    }

    private IEnumerator FinishAndCloseRoutine()
    {
        SetProgress(1.0f);
        yield return new WaitForSecondsRealtime(0.4f); // 새 씬 진입 후 잠시 대기

        Destroy(gameObject); // 영구 유지되었던 로딩 패널 파괴
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
        animTimer += Time.unscaledDeltaTime;
        if (animTimer >= frameInterval)
        {
            animTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % 3;
            UpdateSpriteFrames();
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