using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelProgressBar : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    private Vector3 startPos;
    private Vector3 endPos;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();

    // 🌟 프레임 드랍 방지용 캐싱 변수
    private GameObject[] cachedPlayers = new GameObject[0];
    private float nextSearchTime = 0f;

    private void Start()
    {
        InitializePoints();
    }

    private void InitializePoints()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        if (startObj != null) startPos = startObj.transform.position;

        var doorObj = Object.FindAnyObjectByType<StageDoor>();
        if (doorObj != null) endPos = doorObj.transform.position;
    }

    private void Update()
    {
        // 처음 시작 시 오브젝트를 못 찾았을 경우 대비 (안전 장치)
        if (startPos == Vector3.zero || endPos == Vector3.zero)
        {
            InitializePoints();
            if (startPos == Vector3.zero || endPos == Vector3.zero) return;
        }

        // 🌟 무거운 Find 검색을 매 프레임이 아닌 0.5초마다 1번만 실행 (렉 해결 핵심)
        if (Time.unscaledTime >= nextSearchTime)
        {
            cachedPlayers = GameObject.FindGameObjectsWithTag("Player");
            nextSearchTime = Time.unscaledTime + 0.5f;
        }

        // 🌟 완전히 나간 플레이어의 아이콘만 조용히 삭제
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in playerIcons)
        {
            if (kvp.Key == null) // 게임오브젝트가 완전히 파괴되었을 때만
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove) playerIcons.Remove(k);

        // --- 여기서부터는 질문자님의 원래 100% 작동하던 로직 그대로 사용 ---
        float totalDist = Vector3.Distance(endPos, startPos);
        if (totalDist <= 0) return;

        foreach (GameObject player in cachedPlayers)
        {
            if (player == null) continue;

            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            float currentDist = Vector3.Distance(player.transform.position, startPos);
            float progress = Mathf.Clamp01(currentDist / totalDist);

            RectTransform iconRect = playerIcons[player];
            if (iconRect != null)
            {
                float containerWidth = iconContainer.rect.width;
                iconRect.anchoredPosition = new Vector2(progress * containerWidth, 0);
            }
        }

        // 슬라이더 바 값 업데이트
        if (cachedPlayers.Length > 0 && cachedPlayers[0] != null && progressBar != null)
        {
            float myDist = Vector3.Distance(cachedPlayers[0].transform.position, startPos);
            progressBar.value = Mathf.Clamp01(myDist / totalDist);
        }
    }
}