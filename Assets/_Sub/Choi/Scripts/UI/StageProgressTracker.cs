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
    private float mapLengthX;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private bool isInitialized = false;

    private SpectatorSystem spectatorSystem;

    // 🌟 프레임 드랍 방지용 캐싱 변수
    private GameObject[] cachedPlayers = new GameObject[0];
    private float nextSearchTime = 0f;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start() => InitializeScene();
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InitializeScene();

    private void InitializeScene()
    {
        foreach (Transform child in iconContainer)
        {
            if (child != null) Destroy(child.gameObject);
        }
        playerIcons.Clear();
        isInitialized = false;
        nextSearchTime = 0f; // 씬 로드 시 즉시 갱신

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

            // 질문자님의 원래 계산식 복구
            mapLengthX = endPos.x - startPos.x;

            if (mapLengthX != 0) // 0으로 나누기 방지
            {
                isInitialized = true;
            }
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializePoints();
            if (!isInitialized) return;
        }

        // 🌟 0.5초마다 플레이어 목록 갱신
        if (Time.unscaledTime >= nextSearchTime)
        {
            cachedPlayers = GameObject.FindGameObjectsWithTag("Player");
            nextSearchTime = Time.unscaledTime + 0.5f;
        }

        // 🌟 완전히 나간 플레이어 아이콘 삭제
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in playerIcons)
        {
            if (kvp.Key == null)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove) playerIcons.Remove(k);

        // --- 여기서부터는 질문자님의 원래 100% 작동하던 로직 그대로 사용 ---
        foreach (GameObject player in cachedPlayers)
        {
            if (player == null) continue;

            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            RectTransform iconRect = playerIcons[player];
            if (iconRect == null) continue;

            Transform iconChild = iconRect.Find("Icon");
            if (iconChild != null)
            {
                Image iconImage = iconChild.GetComponent<Image>();
                CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();

                if (identity != null && iconImage != null && identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                {
                    Color targetColor = identity.playerColors[identity.playerIndex];
                    if (iconImage.color != targetColor)
                    {
                        iconImage.color = targetColor;
                    }
                }
            }

            Transform highlight = iconRect.Find("HighlightBorder");
            if (highlight != null)
            {
                bool isSpectated = (spectatorSystem != null && spectatorSystem.CurrentTarget != null && player.transform == spectatorSystem.CurrentTarget);
                if (highlight.gameObject.activeSelf != isSpectated)
                {
                    highlight.gameObject.SetActive(isSpectated);
                }
            }

            // 질문자님의 원래 위치 업데이트 계산식 복구
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = progress * iconContainer.rect.width;
            iconRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }
}