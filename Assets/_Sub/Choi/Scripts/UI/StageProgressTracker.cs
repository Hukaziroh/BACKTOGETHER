using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 전환 이벤트를 위해 반드시 필요

public class StageProgressTracker : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    private Vector3 startPos;
    private Vector3 endPos;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private bool isInitialized = false;

    // 관전 시스템 참조
    private SpectatorSystem spectatorSystem;

    // 💡 씬이 로드될 때 이벤트를 구독합니다.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 💡 오브젝트 파괴 시 이벤트 구독을 해제합니다.
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 첫 시작 시 초기화
        InitializeScene();
    }

    // 💡 씬 로드 시 호출될 메서드
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        // 1. 기존 아이콘들 전부 제거
        foreach (Transform child in iconContainer)
        {
            Destroy(child.gameObject);
        }
        playerIcons.Clear();
        isInitialized = false; // 재초기화 필요 플래그

        // 2. 새로운 씬의 포인트 찾기
        InitializePoints();

        // 3. 관전 시스템 다시 찾기
        spectatorSystem = FindFirstObjectByType<SpectatorSystem>();
    }

    private void InitializePoints()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        var doorObj = Object.FindAnyObjectByType<StageDoor>();

        if (startObj != null && doorObj != null)
        {
            startPos = startObj.transform.position;
            endPos = doorObj.transform.position;
            isInitialized = true;
        }
        else
        {
            // 아직 씬 오브젝트가 로드되지 않았을 수 있음
            isInitialized = false;
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializePoints();
            return;
        }

        // 시스템 유실 시 다시 캐싱
        if (spectatorSystem == null) spectatorSystem = FindFirstObjectByType<SpectatorSystem>();

        float mapLengthX = endPos.x - startPos.x;
        if (mapLengthX <= 0) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            // 1. 아이콘 생성
            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                newIcon.transform.localScale = Vector3.one;
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());

                // 색상 적용
                CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();
                Image iconImage = newIcon.GetComponent<Image>();
                if (iconImage != null && identity != null)
                {
                    // 인덱스 범위 체크 추가
                    if (identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                        iconImage.color = identity.playerColors[identity.playerIndex];
                }
            }

            // 2. 관전 강조 로직
            Transform highlight = playerIcons[player].Find("HighlightBorder");
            if (highlight != null)
            {
                bool isSpectated = (spectatorSystem != null && spectatorSystem.CurrentTarget != null && player.transform == spectatorSystem.CurrentTarget);
                highlight.gameObject.SetActive(isSpectated);
            }

            // 3. 위치 업데이트
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = progress * iconContainer.rect.width;

            // 안전한 널 체크 후 위치 할당
            if (playerIcons.ContainsKey(player))
                playerIcons[player].anchoredPosition = new Vector2(xPos, 0);
        }
    }
}