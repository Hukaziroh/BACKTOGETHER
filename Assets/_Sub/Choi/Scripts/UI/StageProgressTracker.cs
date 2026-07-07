using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Image 컴포넌트 사용을 위해 추가

public class StageProgressTracker : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    private Vector3 startPos;
    private Vector3 endPos;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private bool isInitialized = false;

    private void Start()
    {
        InitializePoints();
    }

    private void InitializePoints()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        var doorObj = Object.FindAnyObjectByType<StageDoor>(); // StageDoor 타입이 씬에 존재해야 합니다

        if (startObj != null) startPos = startObj.transform.position;
        if (doorObj != null) endPos = doorObj.transform.position;

        if (startPos != Vector3.zero && endPos != Vector3.zero)
        {
            isInitialized = true;
            Debug.Log($"[Tracker] 초기화 성공! Start: {startPos}, End: {endPos}");
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializePoints();
            return;
        }

        float mapLengthX = endPos.x - startPos.x;
        if (mapLengthX <= 0) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            // 아이콘 생성 및 초기 세팅
            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                newIcon.transform.localScale = Vector3.one;
                playerIcons.Add(player, newIcon.GetComponent<RectTransform>());

                // 💡 추가된 부분: CoopPlayerIdentity를 가져와 색상 적용
                CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();
                if (identity != null)
                {
                    Image iconImage = newIcon.GetComponent<Image>();
                    if (iconImage != null && identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                    {
                        iconImage.color = identity.playerColors[identity.playerIndex];
                    }
                }
            }

            // 위치 계산 및 업데이트
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);

            float xPos = progress * iconContainer.rect.width;
            playerIcons[player].anchoredPosition = new Vector2(xPos, 0);
        }
    }
}