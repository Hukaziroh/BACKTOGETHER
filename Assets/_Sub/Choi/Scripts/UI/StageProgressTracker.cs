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
        // 1. 기존 아이콘들 전부 제거 (잔재 삭제)
        foreach (Transform child in iconContainer)
        {
            if (child != null) Destroy(child.gameObject);
        }
        playerIcons.Clear();
        isInitialized = false;

        // 2. 새로운 씬의 포인트 찾기
        InitializePoints();

        // 3. 관전 시스템 다시 찾기
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
        else
        {
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

        if (spectatorSystem == null) spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();

        float mapLengthX = endPos.x - startPos.x;
        if (mapLengthX <= 0) return;

        // --- 1. 잔재 및 끊김 처리 ---
        GameObject[] currentPlayers = GameObject.FindGameObjectsWithTag("Player");
        List<GameObject> toRemove = new List<GameObject>();

        // 딕셔너리에는 있는데 실제 게임에는 없는 플레이어 찾기
        foreach (var kvp in playerIcons)
        {
            bool found = false;
            foreach (var p in currentPlayers)
            {
                if (p == kvp.Key) { found = true; break; }
            }
            if (!found || kvp.Key == null) toRemove.Add(kvp.Key);
        }

        // 삭제 처리
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

            // 💡 색상 지속 업데이트 (네트워크 지연 보완)
            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();
            Image iconImage = playerIcons[player].GetComponent<Image>();

            // 매 프레임 올바른 색상인지 체크함 (네트워크 데이터가 늦게 도착해도 곧바로 색상이 바뀜)
            if (identity != null && iconImage != null)
            {
                if (identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                {
                    iconImage.color = identity.playerColors[identity.playerIndex];
                }
            }

            // 관전 강조 로직
            Transform highlight = playerIcons[player].Find("HighlightBorder");
            if (highlight != null)
            {
                bool isSpectated = (spectatorSystem != null && spectatorSystem.CurrentTarget != null && player.transform == spectatorSystem.CurrentTarget);
                highlight.gameObject.SetActive(isSpectated);
            }

            // 위치 업데이트
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = progress * iconContainer.rect.width;

            if (playerIcons.ContainsKey(player))
                playerIcons[player].anchoredPosition = new Vector2(xPos, 0);
        }
    }
}