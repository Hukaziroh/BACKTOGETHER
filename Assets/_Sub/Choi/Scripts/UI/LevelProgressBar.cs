using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelProgressBar : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private RectTransform iconContainer; // 아이콘들이 배치될 패널
    [SerializeField] private GameObject playerIconPrefab; // 1단계에서 만든 아이콘 프리팹

    private Vector3 startPos;
    private Vector3 endPos;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();

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
        if (startPos == Vector3.zero || endPos == Vector3.zero) return;

        // 1. 씬에 있는 모든 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float totalDist = Vector3.Distance(endPos, startPos);

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            // 2. 아이콘이 없으면 생성
            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            // 3. 거리 계산 후 위치 업데이트
            float currentDist = Vector3.Distance(player.transform.position, startPos);
            float progress = Mathf.Clamp01(currentDist / totalDist);

            // 슬라이더 바 위치에 맞춰 아이콘 이동
            RectTransform iconRect = playerIcons[player];
            float containerWidth = iconContainer.rect.width;
            iconRect.anchoredPosition = new Vector2(progress * containerWidth, 0);
        }

        // 4. 슬라이더 자체 값 업데이트 (내 캐릭터 중심 또는 평균 등으로 조정 가능)
        // 여기선 첫 번째 플레이어(보통 나 자신) 기준으로 슬라이더 채움
        if (players.Length > 0 && players[0] != null)
        {
            float myDist = Vector3.Distance(players[0].transform.position, startPos);
            progressBar.value = Mathf.Clamp01(myDist / totalDist);
        }
    }
}