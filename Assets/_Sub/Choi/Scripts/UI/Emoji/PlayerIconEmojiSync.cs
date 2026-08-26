using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;

public class PlayerIconEmojiSync : MonoBehaviour
{
    [Header("크기 설정 (PlayerEmojiController 방식)")]
    [SerializeField] private float baseSize = 20f;          // 기본 크기 (높이 기준)
    [SerializeField] private Vector2 emojiOffset = new Vector2(0f, 12f); // 아이콘 위 위치 오프셋

    private Image emojiImage;
    private RectTransform emojiRect;
    private bool lastActiveState = false;
    private Sprite lastSprite = null;
    private float lastPlayerScaleX = -1f;

    private void Awake()
    {
        Transform emojiChild = transform.Find("EmojiUI");
        if (emojiChild == null)
        {
            GameObject obj = new GameObject("EmojiUI", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(transform, false);

            emojiRect = obj.GetComponent<RectTransform>();
            emojiRect.anchorMin = new Vector2(0.5f, 1f);
            emojiRect.anchorMax = new Vector2(0.5f, 1f);
            emojiRect.pivot = new Vector2(0.5f, 0f);
            emojiRect.anchoredPosition = emojiOffset;

            emojiImage = obj.GetComponent<Image>();
            obj.SetActive(false);
        }
        else
        {
            emojiImage = emojiChild.GetComponent<Image>();
            emojiRect = emojiChild.GetComponent<RectTransform>();
            emojiRect.anchoredPosition = emojiOffset;
            emojiChild.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        GameObject targetPlayer = FindCorrespondingPlayer();
        if (targetPlayer == null)
        {
            if (lastActiveState)
            {
                HideUIEmoji();
            }

            lastActiveState = false;
            lastSprite = null;
            lastPlayerScaleX = -1f;
            return;
        }

        SpriteRenderer playerSr = GetPlayerEmojiSpriteRenderer(targetPlayer);
        if (playerSr != null)
        {
            bool isActive = playerSr.gameObject.activeSelf;
            Sprite currentSprite = playerSr.sprite;
            float currentPlayerScaleX = playerSr.transform.localScale.x;

            bool isRetriggered = false;

            if (isActive)
            {
                if (lastActiveState)
                {
                    if (currentSprite != lastSprite)
                    {
                        isRetriggered = true;
                    }
                    else if (lastPlayerScaleX > 0.15f && currentPlayerScaleX < 0.05f)
                    {
                        isRetriggered = true; // 연타 시 재동작 감지
                    }
                }
                else
                {
                    isRetriggered = true;
                }

                if (isRetriggered)
                {
                    ShowUIEmoji(currentSprite);
                }
            }
            else if (!isActive && lastActiveState)
            {
                HideUIEmoji();
            }

            lastActiveState = isActive;
            lastSprite = currentSprite;
            lastPlayerScaleX = currentPlayerScaleX;
        }
    }

    private GameObject FindCorrespondingPlayer()
    {
        CoopPlayerIdentity localIdentity = GetLocalPlayerIdentity();
        if (localIdentity == null) return null;

        int myIconIndex = transform.GetSiblingIndex();
        if (myIconIndex != localIdentity.playerIndex) return null;

        return localIdentity.gameObject;
    }

    private CoopPlayerIdentity GetLocalPlayerIdentity()
    {
        if (NetworkClient.localPlayer != null &&
            NetworkClient.localPlayer.TryGetComponent(out CoopPlayerIdentity localIdentity))
        {
            return localIdentity;
        }

        if (CoopPlayerIdentity.players == null) return null;

        foreach (CoopPlayerIdentity playerIdentity in CoopPlayerIdentity.players.Values)
        {
            if (playerIdentity != null && playerIdentity.isLocalPlayer)
            {
                return playerIdentity;
            }
        }

        return null;
    }

    private GameObject lastTargetPlayer = null;
    private SpriteRenderer cachedPlayerEmojiSr = null;

    private SpriteRenderer GetPlayerEmojiSpriteRenderer(GameObject player)
    {
        if (lastTargetPlayer == player && cachedPlayerEmojiSr != null)
            return cachedPlayerEmojiSr;

        lastTargetPlayer = player;
        cachedPlayerEmojiSr = null;

        Component emojiController = player.GetComponent("PlayerEmojiController");
        if (emojiController == null) return null;

        System.Type type = emojiController.GetType();
        var field = type.GetField("emojiSpriteRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            cachedPlayerEmojiSr = field.GetValue(emojiController) as SpriteRenderer;
            return cachedPlayerEmojiSr;
        }
        return null;
    }

    private void ShowUIEmoji(Sprite sprite)
    {
        if (emojiImage == null || sprite == null) return;

        emojiImage.sprite = sprite;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;
        if (spriteHeight > 0)
        {
            float aspect = spriteWidth / spriteHeight;
            emojiRect.sizeDelta = new Vector2(baseSize * aspect, baseSize);
        }
        else
        {
            emojiRect.sizeDelta = new Vector2(baseSize, baseSize);
        }

        emojiImage.gameObject.SetActive(true);

        emojiImage.transform.DOKill();
        emojiImage.transform.localScale = Vector3.zero;
        emojiImage.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
    }

    private void HideUIEmoji()
    {
        if (emojiImage == null) return;

        emojiImage.transform.DOKill();
        emojiImage.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (emojiImage != null) emojiImage.gameObject.SetActive(false);
        });
    }
}
