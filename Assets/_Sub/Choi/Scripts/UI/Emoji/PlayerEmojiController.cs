using UnityEngine;
using System.Collections;
using Mirror;
using DG.Tweening;

public class PlayerEmojiController : NetworkBehaviour
{
    [Header("컴포넌트 연결")]
    [Tooltip("플레이어 머리 위에 이모지를 보여줄 SpriteRenderer 오브젝트")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("일반 이모지 스프라이트 리스트")]
    [SerializeField] private Sprite[] emojiSprites;

    [System.Serializable]
    public class AnimatedEmojiData
    {
        public Sprite[] frames; // 순차적으로 보여줄 스프라이트들
    }
    [SerializeField] private AnimatedEmojiData[] animatedEmojis;
    [SerializeField] private float frameInterval = 0.8f; // 프레임 간 전환 시간

    [Header("크기 및 위치 설정")]
    [SerializeField] private float targetSize = 0.5f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float displayDuration = 2.0f;

    private Transform parentTransform;
    private Transform emojiTransform;
    private Coroutine activeEmojiCoroutine;

    private void OnEnable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected += HandleLocalEmojiIndexSelected;
        EmojiRadialMenu.OnAnimatedEmojiIndexSelected += HandleLocalAnimatedEmojiIndexSelected;
    }

    private void OnDisable()
    {
        EmojiRadialMenu.OnEmojiIndexSelected -= HandleLocalEmojiIndexSelected;
        EmojiRadialMenu.OnAnimatedEmojiIndexSelected -= HandleLocalAnimatedEmojiIndexSelected;
        HideEmojiImmediately();
    }

    private void Awake()
    {
        parentTransform = transform;

        if (emojiSpriteRenderer == null)
        {
            emojiSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (emojiSpriteRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] EmojiSpriteRenderer가 연결되지 않았습니다! 인스펙터를 확인하세요.");
            }
        }

        if (emojiSpriteRenderer != null)
        {
            emojiTransform = emojiSpriteRenderer.transform;
            emojiTransform.SetParent(null);
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null) return;

        if (emojiSpriteRenderer != null && emojiSpriteRenderer.gameObject.activeSelf)
        {
            float facingDir = (parentTransform.localScale.x < 0) ? -1f : 1f;
            Vector3 currentOffset = new Vector3(headOffset.x * facingDir, headOffset.y, headOffset.z);
            emojiTransform.position = parentTransform.position + currentOffset;
            emojiTransform.rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        HideEmojiImmediately();

        if (emojiSpriteRenderer != null)
        {
            Destroy(emojiSpriteRenderer.gameObject);
        }
    }

    private void HideEmojiImmediately()
    {
        if (activeEmojiCoroutine != null)
        {
            StopCoroutine(activeEmojiCoroutine);
            activeEmojiCoroutine = null;
        }

        if (emojiTransform != null)
        {
            emojiTransform.DOKill();
        }

        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    // --- 일반 이모지 처리 ---
    private void HandleLocalEmojiIndexSelected(int emojiIndex)
    {
        if (Time.timeScale == 0) return;
        if (!isLocalPlayer) return;
        CmdShowEmoji(emojiIndex);
    }

    [Command]
    private void CmdShowEmoji(int emojiIndex)
    {
        RpcShowEmoji(emojiIndex);
    }

    [ClientRpc]
    private void RpcShowEmoji(int emojiIndex)
    {
        ShowSingleEmoji(emojiIndex);
    }

    // --- 애니메이션 이모지 처리 ---
    private void HandleLocalAnimatedEmojiIndexSelected(int emojiIndex)
    {
        if (Time.timeScale == 0) return;
        if (!isLocalPlayer) return;
        CmdShowAnimatedEmoji(emojiIndex);
    }

    [Command]
    private void CmdShowAnimatedEmoji(int emojiIndex)
    {
        RpcShowAnimatedEmoji(emojiIndex);
    }

    [ClientRpc]
    private void RpcShowAnimatedEmoji(int emojiIndex)
    {
        ShowAnimatedEmoji(emojiIndex);
    }

    // 실제 단일 이모지 띄우기
    private void ShowSingleEmoji(int emojiIndex)
    {
        if (emojiSpriteRenderer == null || emojiSprites == null || emojiIndex < 0 || emojiIndex >= emojiSprites.Length) return;
        if (emojiSprites[emojiIndex] == null) return;

        if (activeEmojiCoroutine != null) StopCoroutine(activeEmojiCoroutine);

        emojiSpriteRenderer.sprite = emojiSprites[emojiIndex];
        emojiSpriteRenderer.gameObject.SetActive(true);

        PlayPopAnimation();

        activeEmojiCoroutine = StartCoroutine(HideEmojiRoutine(displayDuration));
    }

    // 실제 애니메이션(다중 프레임) 이모지 띄우기
    private void ShowAnimatedEmoji(int emojiIndex)
    {
        if (emojiSpriteRenderer == null || animatedEmojis == null || emojiIndex < 0 || emojiIndex >= animatedEmojis.Length) return;
        var animData = animatedEmojis[emojiIndex];
        if (animData.frames == null || animData.frames.Length == 0) return;

        if (activeEmojiCoroutine != null) StopCoroutine(activeEmojiCoroutine);

        emojiSpriteRenderer.gameObject.SetActive(true);

        activeEmojiCoroutine = StartCoroutine(PlayAnimationRoutine(animData.frames));
    }

    private IEnumerator PlayAnimationRoutine(Sprite[] frames)
    {
        foreach (var frame in frames)
        {
            if (frame != null)
            {
                emojiSpriteRenderer.sprite = frame;
                // 🌟 애니메이션 프레임이 전환될 때마다 띠용띠용 튀는 효과 적용
                PlayPopAnimation();
            }
            yield return new WaitForSeconds(frameInterval);
        }

        yield return new WaitForSeconds(displayDuration);

        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideEmojiRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    // 🌟 띠용띠용 커지는 팝업 애니메이션 메서드 (DOTween 사용)
    private void PlayPopAnimation()
    {
        if (emojiTransform == null) return;

        float finalSize = Mathf.Abs(targetSize);

        emojiTransform.DOKill();
        emojiTransform.localScale = Vector3.zero;
        emojiTransform.DOScale(finalSize, 0.35f).SetEase(Ease.OutBack);
    }
}
