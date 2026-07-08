using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        InitializeScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        // 기존 아이콘 제거
        foreach (Transform child in iconContainer)
        {
            if (child != null) Destroy(child.gameObject);
        }
        playerIcons.Clear();
        isInitialized = false;

        InitializePoints();
        spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();
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
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializePoints();
            return;
        }

        if (spectatorSystem == null) spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();

        float mapLengthX = endPos.x - startPos.x;
        if (mapLengthX <= 0) return;

        GameObject[] currentPlayers = GameObject.FindGameObjectsWithTag("Player");

        // --- 1. 잔재 및 끊김 처리 ---
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in playerIcons)
        {
            bool found = false;
            foreach (var p in currentPlayers) { if (p == kvp.Key) { found = true; break; } }
            if (!found || kvp.Key == null) toRemove.Add(kvp.Key);
        }

        foreach (var p in toRemove)
        {
            if (playerIcons.ContainsKey(p))
            {
                if (playerIcons[p] != null) Destroy(playerIcons[p].gameObject);
                playerIcons.Remove(p);
            }
        }

        // --- 2. 아이콘 생성 및 업데이트 ---
        foreach (GameObject player in currentPlayers)
        {
            if (player == null) continue;

            // 아이콘 없으면 생성
            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                newIcon.transform.localScale = Vector3.one;
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            // [수정] 해당 플레이어의 RectTransform 가져오기
            RectTransform iconRect = playerIcons[player];

            // 💡 자식 오브젝트 "Icon" 찾기
            Transform iconChild = iconRect.Find("Icon");
            if (iconChild == null)
            {
                Debug.LogError($"[오류] {player.name} 아이콘의 자식 중 'Icon'을 찾을 수 없습니다!");
                continue;
            }

            Image iconImage = iconChild.GetComponent<Image>();
            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();

            // 색상 적용
            if (identity != null && iconImage != null)
            {
                if (identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                {
                    Color targetColor = identity.playerColors[identity.playerIndex];
                    if (iconImage.color != targetColor)
                    {
                        iconImage.color = targetColor;
                    }
                }
            }

            // 관전 강조 로직
            Transform highlight = iconRect.Find("HighlightBorder");
            if (highlight != null)
            {
                bool isSpectated = (spectatorSystem != null && spectatorSystem.CurrentTarget != null && player.transform == spectatorSystem.CurrentTarget);
                if (highlight.gameObject.activeSelf != isSpectated)
                {
                    highlight.gameObject.SetActive(isSpectated);
                }
            }

            // 위치 업데이트
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = progress * iconContainer.rect.width;
            iconRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }
}