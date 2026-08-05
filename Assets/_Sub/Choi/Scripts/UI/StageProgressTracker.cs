using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageProgressTracker : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    [Header("체크포인트 깃발 설정")]
    [Tooltip("커스텀 깃발 스프라이트가 있다면 여기에 넣으세요. 비워두면 빨간색 기본 깃발로 표시됩니다.")]
    [SerializeField] private Sprite checkpointFlagSprite;

    [Header("플레이어 아이콘 스프라이트 설정")]
    [Tooltip("플레이어 순서(Index)에 따라 적용할 스프라이트 리스트입니다. (예: 0번 플레이어 표정, 1번 플레이어 표정...)")]
    [SerializeField] private List<Sprite> playerIconSprites = new List<Sprite>();

    [Header("프로그래스 바 보정")]
    [Tooltip("전체 길이 비율을 조절합니다 (밀림 폭이 점점 커지거나 작아질 때 조절)")]
    [SerializeField] private float progressMultiplier = 1f;
    [Tooltip("시작 위치(오프셋)를 통째로 이동시킵니다")]
    [SerializeField] private float progressOffset = 0f;

    private Vector3 startPos;
    private Vector3 endPos;
    private float mapLengthX;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private bool isInitialized = false;

    private SpectatorSystem spectatorSystem;

    // 🌟 프레임 드랍 방지용 캐싱 변수
    private GameObject[] cachedPlayers = new GameObject[0];
    private float nextSearchTime = 0f;

    // 🌟 가장 최근에 찍은 체크포인트 깃발 객체 관리 변수
    private GameObject activeCheckpointFlagObj;

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
        activeCheckpointFlagObj = null;
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

        // 🌟 내 플레이어(또는 관전 중인 대상)의 체크포인트 깃발 갱신
        UpdateActiveCheckpointFlag();

        float containerWidth = iconContainer.rect.width;

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

            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();

            // 🌟 1. BackIcon 색상 변경
            Transform backIconChild = iconRect.Find("BackIcon");
            if (backIconChild != null)
            {
                Image backImageIcon = backIconChild.GetComponent<Image>();
                if (identity != null && backImageIcon != null && identity.playerIndex >= 0)
                {
                    if (identity.playerColors != null && identity.playerIndex < identity.playerColors.Length)
                    {
                        Color targetColor = identity.playerColors[identity.playerIndex];
                        if (backImageIcon.color != targetColor)
                        {
                            backImageIcon.color = targetColor;
                        }
                    }
                }
            }

            // 🌟 2. Icon 스프라이트 변경
            Transform iconChild = iconRect.Find("Icon");
            if (iconChild != null)
            {
                Image iconImage = iconChild.GetComponent<Image>();
                if (identity != null && iconImage != null && identity.playerIndex >= 0)
                {
                    if (playerIconSprites != null && playerIconSprites.Count > 0 && identity.playerIndex < playerIconSprites.Count)
                    {
                        Sprite targetSprite = playerIconSprites[identity.playerIndex];
                        if (targetSprite != null && iconImage.sprite != targetSprite)
                        {
                            iconImage.sprite = targetSprite;
                            Color currentColor = iconImage.color;
                            currentColor.a = 1f;
                            iconImage.color = currentColor;
                        }
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

            // 🌟 시작점일 때 Pos X가 정확히 0이 되도록 순수 비율 계산만 적용
            float currentDistX = player.transform.position.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = (progress * containerWidth * progressMultiplier) + progressOffset;
            iconRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }

    // 🌟 내 플레이어(또는 관전 중인 대상)가 마지막으로 찍은 체크포인트를 빨간색 깃발로 표시
    private void UpdateActiveCheckpointFlag()
    {
        Vector3 activeCpPos = Vector3.zero;
        bool foundActive = false;

        // 1. 기준이 될 플레이어 찾기 (관전 중이면 관전 타겟, 아니면 내 플레이어)
        GameObject targetPlayer = null;

        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            targetPlayer = spectatorSystem.CurrentTarget.gameObject;
        }
        else
        {
            targetPlayer = GetLocalPlayer();
        }

        // 2. 해당 플레이어의 체크포인트 인덱스 가져오기
        if (targetPlayer != null)
        {
            PlayerRespawn respawn = targetPlayer.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                int cpIndex = GetPlayerCheckpointIndex(respawn);
                if (cpIndex >= 0)
                {
                    CoopCheckpoint[] allCheckpoints = Object.FindObjectsByType<CoopCheckpoint>(FindObjectsInactive.Exclude);
                    foreach (var cp in allCheckpoints)
                    {
                        if (cp != null && cp.checkpointIndex == cpIndex && cp.spawnLocation != null)
                        {
                            activeCpPos = cp.spawnLocation.position;
                            foundActive = true;
                            break;
                        }
                    }
                }
            }
        }

        // 3. 깃발 UI 생성 및 위치 갱신
        if (foundActive)
        {
            float containerWidth = iconContainer.rect.width;
            float currentDistX = activeCpPos.x - startPos.x;
            float progress = Mathf.Clamp01(currentDistX / mapLengthX);
            float xPos = (progress * containerWidth * progressMultiplier) + progressOffset;

            // 깃발 오브젝트가 없으면 새로 생성 (프로그래스 바 상단에 걸쳐지도록 설정)
            if (activeCheckpointFlagObj == null)
            {
                activeCheckpointFlagObj = new GameObject("ActiveCheckpointFlag", typeof(RectTransform));
                activeCheckpointFlagObj.transform.SetParent(iconContainer, false);

                RectTransform flagContainerRect = activeCheckpointFlagObj.GetComponent<RectTransform>();
                flagContainerRect.anchorMin = new Vector2(0, 0.5f);
                flagContainerRect.anchorMax = new Vector2(0, 0.5f);
                flagContainerRect.pivot = new Vector2(0.5f, 0f); // 깃발의 하단이 바의 중앙 기준선에 오도록 설정
                flagContainerRect.sizeDelta = new Vector2(16f, 24f);

                // 1. 깃대 (세로 줄)
                GameObject poleObj = new GameObject("Pole", typeof(RectTransform), typeof(Image));
                poleObj.transform.SetParent(flagContainerRect, false);
                RectTransform poleRect = poleObj.GetComponent<RectTransform>();
                poleRect.anchorMin = new Vector2(0.5f, 0f);
                poleRect.anchorMax = new Vector2(0.5f, 1f);
                poleRect.sizeDelta = new Vector2(2f, 0f);
                poleRect.anchoredPosition = Vector2.zero;
                poleObj.GetComponent<Image>().color = Color.white;

                // 2. 깃발 천 (빨간색)
                GameObject bannerObj = new GameObject("Banner", typeof(RectTransform), typeof(Image));
                bannerObj.transform.SetParent(flagContainerRect, false);
                RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.5f, 1f);
                bannerRect.anchorMax = new Vector2(0.5f, 1f);
                bannerRect.pivot = new Vector2(0f, 1f); // 왼쪽 위 기준
                bannerRect.sizeDelta = new Vector2(14f, 10f);
                bannerRect.anchoredPosition = new Vector2(0f, 0f);

                Image bannerImage = bannerObj.GetComponent<Image>();
                if (checkpointFlagSprite != null)
                {
                    bannerImage.sprite = checkpointFlagSprite;
                    bannerImage.color = Color.white;
                }
                else
                {
                    bannerImage.color = Color.red;
                }
            }
            else
            {
                Transform bannerTransform = activeCheckpointFlagObj.transform.Find("Banner");
                if (bannerTransform != null)
                {
                    Image bannerImage = bannerTransform.GetComponent<Image>();
                    if (bannerImage != null)
                    {
                        if (checkpointFlagSprite != null)
                        {
                            bannerImage.sprite = checkpointFlagSprite;
                            bannerImage.color = Color.white;
                        }
                        else
                        {
                            bannerImage.color = Color.red;
                        }
                    }
                }
            }

            RectTransform activeRect = activeCheckpointFlagObj.GetComponent<RectTransform>();
            if (activeRect != null)
            {
                float barHalfHeight = iconContainer.rect.height * 0.5f;
                activeRect.anchoredPosition = new Vector2(xPos, barHalfHeight);
            }

            if (!activeCheckpointFlagObj.activeSelf)
            {
                activeCheckpointFlagObj.SetActive(true);
            }
        }
        else
        {
            if (activeCheckpointFlagObj != null && activeCheckpointFlagObj.activeSelf)
            {
                activeCheckpointFlagObj.SetActive(false);
            }
        }
    }

    // 🌟 로컬 플레이어(내 캐릭터)를 리플렉션으로 안전하게 찾아내는 보조 함수
    private GameObject GetLocalPlayer()
    {
        foreach (var player in cachedPlayers)
        {
            if (player == null) continue;
            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();
            if (identity != null)
            {
                System.Type type = identity.GetType();
                string[] localNames = { "isLocal", "isLocalPlayer", "isOwner", "isLocalClient" };
                foreach (var name in localNames)
                {
                    var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        if ((bool)field.GetValue(identity)) return player;
                    }
                    var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
                    {
                        if ((bool)prop.GetValue(identity, null)) return player;
                    }
                }
            }
        }

        // 판별 필드를 찾지 못했거나 싱글플레이/테스트 환경인 경우 첫 번째 플레이어 반환
        if (cachedPlayers.Length > 0) return cachedPlayers[0];
        return null;
    }

    private int GetPlayerCheckpointIndex(PlayerRespawn respawn)
    {
        if (respawn == null) return -1;
        System.Type type = respawn.GetType();
        string[] possibleNames = { "checkpointIndex", "currentCheckpointIndex", "respawnIndex", "lastCheckpointIndex", "spawnIndex" };
        foreach (var name in possibleNames)
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
            {
                return (int)field.GetValue(respawn);
            }
            var prop = type.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(int) && prop.CanRead)
            {
                return (int)prop.GetValue(respawn, null);
            }
        }
        return -1;
    }
}