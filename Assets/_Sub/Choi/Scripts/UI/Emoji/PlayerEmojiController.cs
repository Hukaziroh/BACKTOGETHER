using UnityEngine;
using System.Collections;
using Mirror;

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
        public Sprite[] frames; // 순차적으로 보여줄 스프라이트들 (예: 3 -> 2 -> 1)
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
        if (emojiSpriteRenderer != null)
        {
            Destroy(emojiSpriteRenderer.gameObject);
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
        SetEmojiScale();

        emojiSpriteRenderer.gameObject.SetActive(true);
        activeEmojiCoroutine = StartCoroutine(HideEmojiRoutine(displayDuration));
    }

    // 실제 애니메이션(다중 프레임) 이모지 띄우기
    private void ShowAnimatedEmoji(int emojiIndex)
    {
        if (emojiSpriteRenderer == null || animatedEmojis == null || emojiIndex < 0 || emojiIndex >= animatedEmojis.Length) return;
        var animData = animatedEmojis[emojiIndex];
        if (animData.frames == null || animData.frames.Length == 0) return;

        if (activeEmojiCoroutine != null) StopCoroutine(activeEmojiCoroutine);

        SetEmojiScale();
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
            }
            yield return new WaitForSeconds(frameInterval);
        }

        // 애니메이션 재생이 모두 끝난 후 일정 시간 유지하다가 끔 (혹은 바로 끄려면 frameInterval만 유지)
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

    private void SetEmojiScale()
    {
        float scaleX = Mathf.Abs(targetSize);
        float scaleY = Mathf.Abs(targetSize);
        emojiTransform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}