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

            // 1) 생성
            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                newIcon.transform.localScale = Vector3.one;
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            RectTransform iconRect = playerIcons[player];
            Image iconImage = iconRect.GetComponent<Image>();
            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();

            // 2) 색상 업데이트 (생성 시 + 데이터 수신 시 딱 한 번씩만 수행)
            if (identity != null && iconImage != null)
            {
                if (identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                {
                    Color targetColor = identity.playerColors[identity.playerIndex];
                    // 💡 현재 색상과 다를 때만 변경 (효율적인 갱신)
                    if (iconImage.color != targetColor)
                    {
                        iconImage.color = targetColor;
                    }
                }
            }

            // 3) 관전 강조 로직
            Transform highlight = iconRect.Find("HighlightBorder");
            if (highlight != null)
            {
                bool isSpectated = (spectatorSystem != null && spectatorSystem.CurrentTarget != null && player.transform == spectatorSystem.CurrentTarget);
                // 💡 여기도 매 프레임 SetActive 하지 않도록 최적화 가능
                if (highlight.gameObject.activeSelf != isSpectated)
                {
                    highlight.gameObject.SetActive(isSpectated);
                }
            }

            // 4) 위치 업데이트
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = progress * iconContainer.rect.width;
            iconRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }
}