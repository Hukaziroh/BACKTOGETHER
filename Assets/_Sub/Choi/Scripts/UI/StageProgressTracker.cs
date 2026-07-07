using UnityEngine;
using System.Collections.Generic;

public class StageProgressTracker : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    private Vector3 startPos;
    private Vector3 endPos;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private bool isInitialized = false; // 💡 초기화 상태 추가

    private void Start()
    {
        InitializePoints();
    }

    private void InitializePoints()
    {
        // 씬 고정 오브젝트 찾기
        GameObject startObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        var doorObj = Object.FindAnyObjectByType<StageDoor>();

        if (startObj != null) startPos = startObj.transform.position;
        if (doorObj != null) endPos = doorObj.transform.position;

        // 💡 둘 다 찾았으면 초기화 완료
        if (startPos != Vector3.zero && endPos != Vector3.zero)
        {
            isInitialized = true;
            Debug.Log($"[Tracker] 초기화 성공! Start: {startPos}, End: {endPos}");
        }
    }

    private void Update()
    {
        // 💡 초기화 안 됐으면 계속 시도
        if (!isInitialized)
        {
            InitializePoints();
            return; // 초기화 전까지 로직 중단
        }

        // 맵 길이 계산 (Y축 무시, X축만)
        float mapLengthX = endPos.x - startPos.x;

        // 예외 처리: 맵 길이가 너무 짧으면 오류 방지
        if (mapLengthX <= 0) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                newIcon.transform.localScale = Vector3.one;
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());
            }

            // 💡 핵심: 시작점(startPos.x)을 기준으로 현재 진행 거리 계산
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);

            // 아이콘 위치 적용 (Pivot과 Anchor가 (0, 0)으로 설정된 컨테이너 기준)
            float xPos = progress * iconContainer.rect.width;
            playerIcons[player].anchoredPosition = new Vector2(xPos, 0);
        }
    }
}