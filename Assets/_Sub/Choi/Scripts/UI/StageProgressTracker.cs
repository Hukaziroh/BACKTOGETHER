using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageProgressTracker : MonoBehaviour
{
    [Header("UI ?∞Í≤∞")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject playerIconPrefab;

    [Header("Ï≤¥ÌÅ¨?¨Ïù∏??ÍπÉÎ∞ú ?§Ï†ï")]
    [Tooltip("Ïª§Ïä§?Ä ÍπÉÎ∞ú ?§ÌîÑ?ºÏù¥?∏Í? ?àÎã§Î©??¨Í∏∞???£Ïúº?∏Ïöî. ÎπÑÏõå?êÎ©¥ Îπ®Í∞Ñ??Í∏∞Î≥∏ ÍπÉÎ∞úÎ°??úÏãú?©Îãà??")]
    [SerializeField] private Sprite checkpointFlagSprite;

    [Header("?åÎ†à?¥Ïñ¥ ?ÑÏù¥ÏΩ??§ÌîÑ?ºÏù¥???§Ï†ï")]
    [Tooltip("?åÎ†à?¥Ïñ¥ ?úÏÑú(Index)???∞Îùº ?ÅÏö©???§ÌîÑ?ºÏù¥??Î¶¨Ïä§?∏ÏûÖ?àÎã§. (?? 0Î≤??åÎ†à?¥Ïñ¥ ?úÏ†ï, 1Î≤??åÎ†à?¥Ïñ¥ ?úÏ†ï...)")]
    [SerializeField] private List<Sprite> playerIconSprites = new List<Sprite>();

    [Header("6Ï±ïÌÑ∞ Î≥¥Ïä§ ?ÑÏù¥ÏΩ??§Ï†ï")]
    [SerializeField] private Sprite bossIconSprite;
    [SerializeField] private string bossTag = "Boss";
    [SerializeField] private List<string> bossSceneNames = new List<string> { "chapter6", "Nchapter6" };
    [SerializeField] private Vector2 bossIconSize = new Vector2(34f, 18f);
    [SerializeField] private Color bossBackIconColor = new Color(0.45f, 0f, 0f, 1f);

    [Header("?ÑÎ°úÍ∑∏Îûò??Î∞?Î≥¥Ï†ï")]
    [Tooltip("?ÑÏ≤¥ Í∏∏Ïù¥ ÎπÑÏú®??Ï°∞Ï†à?©Îãà??(Î∞ÄÎ¶???ù¥ ?êÏ†ê Ïª§Ï?Í±∞ÎÇò ?ëÏïÑÏß???Ï°∞Ï†à)")]
    [SerializeField] private float progressMultiplier = 1f;
    [Tooltip("?úÏûë ?ÑÏπò(?§ÌîÑ??Î•??µÏß∏Î°??¥Îèô?úÌÇµ?àÎã§")]
    [SerializeField] private float progressOffset = 0f;

    [Header("?∏Î°ú ÏßÑÌñâ ?êÏ†ï ?§ÌÖå?¥Ï?")]
    [Tooltip("?ÑÎ°úÍ∑∏Îûò??Î∞îÎäî Í∞ÄÎ°úÎ°ú ?†Ï??òÍ≥†, ?îÎìú YÏ∂ïÏúºÎ°?ÏßÑÌñâÎ•†ÏùÑ Í≥ÑÏÇ∞??Scene ?¥Î¶Ñ?ÖÎãà??")]
    [SerializeField] private List<string> verticalSceneNames = new List<string> { "Ex2" };

    private Vector3 startPos;
    private Vector3 endPos;
    private float mapLength;
    private Dictionary<GameObject, RectTransform> playerIcons = new Dictionary<GameObject, RectTransform>();
    private RectTransform bossIcon;
    private GameObject cachedBoss;
    private bool isInitialized = false;

    private SpectatorSystem spectatorSystem;

    // ?åü ?ÑÎ†à???úÎûç Î∞©Ï???Ï∫êÏã± Î≥Ä??    private GameObject[] cachedPlayers = new GameObject[0];
    private float nextSearchTime = 0f;

    // ?åü Í∞Ä??ÏµúÍ∑º??Ï∞çÏ? Ï≤¥ÌÅ¨?¨Ïù∏??ÍπÉÎ∞ú Í∞ùÏ≤¥ Í¥ÄÎ¶?Î≥Ä??    private GameObject activeCheckpointFlagObj;

    // ?í° ÏµúÏ†Å?? Ï≤¥ÌÅ¨?¨Ïù∏??Î™©Î°ù?????ÑÌôò ??1?åÎßå Ï∫êÏã±
    private CoopCheckpoint[] cachedCheckpoints;

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
        bossIcon = null;
        cachedBoss = null;
        activeCheckpointFlagObj = null;
        isInitialized = false;
        nextSearchTime = 0f; // ??Î°úÎìú ??Ï¶âÏãú Í∞±Ïã†

        InitializePoints();
        spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();
        cachedCheckpoints = Object.FindObjectsByType<CoopCheckpoint>(FindObjectsInactive.Exclude);
    }

    private void InitializePoints()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        var doorObj = Object.FindAnyObjectByType<StageDoor>();

        if (startObj != null && doorObj != null)
        {
            startPos = startObj.transform.position;
            endPos = doorObj.transform.position;

            mapLength = IsVerticalStage()
                ? endPos.y - startPos.y
                : endPos.x - startPos.x;

            if (!Mathf.Approximately(mapLength, 0f)) // 0?ºÎ°ú ?òÎàÑÍ∏?Î∞©Ï?
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

        // ?åü 0.5Ï¥àÎßà???åÎ†à?¥Ïñ¥ Î™©Î°ù Í∞±Ïã†
        if (Time.unscaledTime >= nextSearchTime)
        {
            cachedPlayers = GameObject.FindGameObjectsWithTag("Player");
            nextSearchTime = Time.unscaledTime + 0.5f;
        }

        // ?åü ?ÑÏ†Ñ???òÍ∞Ñ ?åÎ†à?¥Ïñ¥ ?ÑÏù¥ÏΩ???†ú
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

        // ?åü ???åÎ†à?¥Ïñ¥(?êÎäî Í¥Ä??Ï§ëÏù∏ ?Ä????Ï≤¥ÌÅ¨?¨Ïù∏??ÍπÉÎ∞ú Í∞±Ïã†
        UpdateActiveCheckpointFlag();

        foreach (GameObject player in cachedPlayers)
        {
            if (player == null) continue;

            if (!playerIcons.ContainsKey(player))
            {
                GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
                RectTransform newIconRect = newIcon.GetComponent<RectTransform>();
                PlayerIconEmojiSync emojiSync = newIcon.GetComponent<PlayerIconEmojiSync>();
                if (emojiSync != null)
                {
                    emojiSync.SetTargetPlayer(player);
                }
                playerIcons.Add(player, newIconRect);
            }

            RectTransform iconRect = playerIcons[player];
            if (iconRect == null) continue;

            CoopPlayerIdentity identity = player.GetComponent<CoopPlayerIdentity>();

            // ?åü 1. BackIcon ?âÏÉÅ Î≥ÄÍ≤?            Transform backIconChild = iconRect.Find("BackIcon");
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

            // ?åü 2. Icon ?§ÌîÑ?ºÏù¥??Î≥ÄÍ≤?            Transform iconChild = iconRect.Find("Icon");
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

            float progress = GetProgress(player.transform.position);
            iconRect.anchoredPosition = GetIconPosition(progress);
        }

        UpdateBossIcon();
    }

    private void UpdateBossIcon()
    {
        if (!ShouldTrackBossInCurrentScene())
        {
            cachedBoss = null;
            if (bossIcon != null && bossIcon.gameObject.activeSelf)
            {
                bossIcon.gameObject.SetActive(false);
            }

            return;
        }

        GameObject boss = FindBoss();
        if (boss == null)
        {
            if (bossIcon != null && bossIcon.gameObject.activeSelf)
            {
                bossIcon.gameObject.SetActive(false);
            }

            return;
        }

        if (bossIcon == null)
        {
            if (playerIconPrefab == null) return;

            GameObject newIcon = Instantiate(playerIconPrefab, iconContainer);
            newIcon.name = "BossProgressIcon";
            bossIcon = newIcon.GetComponent<RectTransform>();
            SetupBossIconVisual(newIcon);
        }

        if (!bossIcon.gameObject.activeSelf)
        {
            bossIcon.gameObject.SetActive(true);
        }

        float progress = GetProgress(boss.transform.position);
        bossIcon.anchoredPosition = GetIconPosition(progress);
    }

    private bool IsVerticalStage()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (verticalSceneNames == null) return false;

        foreach (string verticalSceneName in verticalSceneNames)
        {
            if (!string.IsNullOrEmpty(verticalSceneName) &&
                string.Equals(sceneName, verticalSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private float GetProgress(Vector3 worldPosition)
    {
        float currentDistance = IsVerticalStage()
            ? worldPosition.y - startPos.y
            : worldPosition.x - startPos.x;

        return Mathf.Clamp01(currentDistance / mapLength);
    }

    private Vector2 GetIconPosition(float progress)
    {
        float xPosition =
            progress * iconContainer.rect.width * progressMultiplier + progressOffset;

        return new Vector2(xPosition, 0f);
    }

    private bool ShouldTrackBossInCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (bossSceneNames == null || bossSceneNames.Count == 0)
        {
            return sceneName.Contains("chapter6");
        }

        foreach (string bossSceneName in bossSceneNames)
        {
            if (!string.IsNullOrEmpty(bossSceneName) &&
                string.Equals(sceneName, bossSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private GameObject FindBoss()
    {
        if (cachedBoss != null && cachedBoss.activeInHierarchy)
        {
            return cachedBoss;
        }

        if (string.IsNullOrEmpty(bossTag))
        {
            cachedBoss = null;
            return cachedBoss;
        }

        try
        {
            cachedBoss = GameObject.FindGameObjectWithTag(bossTag);
        }
        catch (UnityException)
        {
            cachedBoss = null;
        }

        return cachedBoss;
    }

    private void SetupBossIconVisual(GameObject iconObject)
    {
        if (iconObject == null) return;

        PlayerIconEmojiSync emojiSync = iconObject.GetComponent<PlayerIconEmojiSync>();
        if (emojiSync != null)
        {
            Destroy(emojiSync);
        }

        Transform backIconChild = iconObject.transform.Find("BackIcon");
        if (backIconChild != null)
        {
            Image backImage = backIconChild.GetComponent<Image>();
            if (backImage != null)
            {
                backImage.color = bossBackIconColor;
            }
            Transform iconChild = iconObject.transform.Find("Icon");
        if (iconChild != null)
        {
            Image iconImage = iconChild.GetComponent<Image>();
            RectTransform iconRect = iconChild.GetComponent<RectTransform>();

            if (iconImage != null)
            {
                iconImage.sprite = bossIconSprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            if (iconRect != null)
            {
                iconRect.sizeDelta = bossIconSize;
            }
        }

        Transform highlight = iconObject.transform.Find("HighlightBorder");
        if (highlight != null)
        {
            highlight.gameObject.SetActive(false);
        }
    }

    // ?í° ÏµúÏ†Å?? Ï≤¥ÌÅ¨?¨Ïù∏??Î™©Î°ù?????ÑÌôò ??1?åÎßå Ï∫êÏã±
    private void UpdateActiveCheckpointFlag()
    {
        Vector3 activeCpPos = Vector3.zero;
        bool foundActive = false;

        // 1. Í∏∞Ï??????åÎ†à?¥Ïñ¥ Ï∞æÍ∏∞ (Í¥Ä??Ï§ëÏù¥Î©?Í¥Ä???ÄÍ≤? ?ÑÎãàÎ©?Î°úÏª¨ ?åÎ†à?¥Ïñ¥)
        GameObject targetPlayer = null;

        if (spectatorSystem != null && spectatorSystem.CurrentTarget != null)
        {
            targetPlayer = spectatorSystem.CurrentTarget.gameObject;
        }
        else
        {
            targetPlayer = GetLocalPlayer();
        }

        // 2. ?¥Îãπ ?åÎ†à?¥Ïñ¥??Ï≤¥ÌÅ¨?¨Ïù∏???∏Îç±?§Î? Í∞Ä?∏Ïò§Í∏?        if (targetPlayer != null)
        {
            PlayerRespawn respawn = targetPlayer.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                int cpIndex = respawn.currentCheckpointIndex;
                if (cpIndex >= 0 && cachedCheckpoints != null)
                {
                    foreach (var cp in cachedCheckpoints)
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

        // 3. ÍπÉÎ∞ú UI ?ùÏÑ± Î∞??ÑÏπò Í∞±Ïã†
        if (foundActive)
        {
            float progress = GetProgress(activeCpPos);
            Vector2 progressPosition = GetIconPosition(progress);

            // ÍπÉÎ∞ú ?§Î∏å?ùÌä∏Í∞Ä ?ÜÏúºÎ©??àÎ°ú ?ùÏÑ± (?ÑÎ°úÍ∑∏Îûò??Î∞??ÅÎã®??Í±∏Ï≥êÏßÄ?ÑÎ°ù ?§Ï†ï)
            if (activeCheckpointFlagObj == null)
            {
                activeCheckpointFlagObj = new GameObject("ActiveCheckpointFlag", typeof(RectTransform));
                activeCheckpointFlagObj.transform.SetParent(iconContainer, false);

                RectTransform flagContainerRect = activeCheckpointFlagObj.GetComponent<RectTransform>();
                flagContainerRect.anchorMin = new Vector2(0f, 0.5f);
                flagContainerRect.anchorMax = new Vector2(0f, 0.5f);
                flagContainerRect.pivot = new Vector2(0.5f, 0f); // ÍπÉÎ∞ú???òÎã®??Î∞îÏùò Ï§ëÏïô Í∏∞Ï??†Ïóê ?§ÎèÑÎ°??§Ï†ï
                flagContainerRect.sizeDelta = new Vector2(16f, 24f);

                // 1. ÍπÉÎ? (?∏Î°ú Ï§?
                GameObject poleObj = new GameObject("Pole", typeof(RectTransform), typeof(Image));
                poleObj.transform.SetParent(flagContainerRect, false);
                RectTransform poleRect = poleObj.GetComponent<RectTransform>();
                poleRect.anchorMin = new Vector2(0.5f, 0f);
                poleRect.anchorMax = new Vector2(0.5f, 1f);
                poleRect.sizeDelta = new Vector2(2f, 0f);
                poleRect.anchoredPosition = Vector2.zero;
                poleObj.GetComponent<Image>().color = Color.white;

                // 2. ÍπÉÎ∞ú Ï≤?(Îπ®Í∞Ñ??
                GameObject bannerObj = new GameObject("Banner", typeof(RectTransform), typeof(Image));
                bannerObj.transform.SetParent(flagContainerRect, false);
                RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.5f, 1f);
                bannerRect.anchorMax = new Vector2(0.5f, 1f);
                bannerRect.pivot = new Vector2(0f, 1f); // ?∞Ï∏° ??Í∏∞Ï?
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
                activeRect.anchoredPosition = new Vector2(progressPosition.x, barHalfHeight);
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

    // ?í° ÏµúÏ†Å?? Reflection ?ÜÏù¥ Mirror??localPlayerÎ•?ÏßÅÏ†ë Î∞òÌôò
    private GameObject GetLocalPlayer()
    {
        if (Mirror.NetworkClient.localPlayer != null)
        {
            return Mirror.NetworkClient.localPlayer.gameObject;
        }

        // ?¥Î∞±: ?ùÎ≥Ñ?êÎ? Î™?Ï∞æÏïòÍ±∞ÎÇò ?§ÌîÑ?ºÏù∏??Í≤ΩÏö∞ Ï≤?Î≤àÏß∏ ?åÎ†à?¥Ïñ¥ Î∞òÌôò
        if (cachedPlayers != null && cachedPlayers.Length > 0) return cachedPlayers[0];
        return null;
    }

    private int GetPlayerCheckpointIndex(PlayerRespawn respawn)
    {
        if (respawn == null) return -1;
        return respawn.currentCheckpointIndex;
    }
}

