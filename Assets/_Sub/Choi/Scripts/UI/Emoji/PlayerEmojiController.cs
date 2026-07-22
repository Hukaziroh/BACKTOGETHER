using UnityEngine;

public class PlayerEmojiController : MonoBehaviour
{
    [Header("이모지 스프라이트 리스트 (인덱스 매칭용)")]
    public Sprite[] emojiSprites;

    [Header("머리 위에 띄울 SpriteRenderer")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("위치 및 크기 설정")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 2f, 0f); // 머리 위 오프셋 위치
    [SerializeField] private float emojiSize = 1f;                         // 이모지 크기

    [Header("표시 지속 시간")]
    public float displayDuration = 2f;
    private float hideTimer;

    private Transform parentTransform;
    private Transform emojiTransform;

    void Awake()
    {
        parentTransform = transform;

        if (emojiSpriteRenderer == null)
        {
            emojiSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (emojiSpriteRenderer != null)
        {
            emojiTransform = emojiSpriteRenderer.transform;

            // ★ 핵심: 부모-자식 관계를 끊어버려 캐릭터의 좌우 반전(스케일 변경) 영향 받지 않음
            emojiTransform.SetParent(null);

            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (parentTransform == null)
        {
            if (emojiSpriteRenderer != null)
                Destroy(emojiSpriteRenderer.gameObject);
            return;
        }

        // 이모지가 켜져있는 동안 위치 및 방향 실시간 추적
        if (emojiSpriteRenderer != null && emojiSpriteRenderer.gameObject.activeSelf)
        {
            UpdateEmojiTransform();
        }

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                if (emojiSpriteRenderer != null)
                {
                    emojiSpriteRenderer.gameObject.SetActive(false);
                }
            }
        }
    }

    // 월드 좌표 기반으로 위치를 따라가되, 캐릭터가 바라보는 방향에 맞춰 머리 위 오프셋 보정
    private void UpdateEmojiTransform()
    {
        if (emojiTransform == null || parentTransform == null) return;

        // 캐릭터가 좌우 반전(-1)되었을 때 머리 위 오프셋도 제 위치를 잡도록 X축 반전 반영
        float facingDir = (parentTransform.localScale.x < 0) ? -1f : 1f;
        Vector3 currentOffset = new Vector3(headOffset.x * facingDir, headOffset.y, headOffset.z);

        // 월드 좌표계에서 캐릭터 위치 + 오프셋 적용
        emojiTransform.position = parentTransform.position + currentOffset;

        // 부모의 스케일 영향을 받지 않으므로 크기와 반전이 절대 깨지지 않고 고정됨
        emojiTransform.localScale = Vector3.one * emojiSize;
        emojiTransform.rotation = Quaternion.identity;
    }

    void OnDestroy()
    {
        // 캐릭터가 사라질 때 독립시킨 이모지 오브젝트도 함께 안전하게 제거
        if (emojiSpriteRenderer != null)
        {
            Destroy(emojiSpriteRenderer.gameObject);
        }
    }

    // 정수형 인덱스 기반 이모지 출력
    public void ShowEmoji(int index)
    {
        if (emojiSprites == null || index < 0 || index >= emojiSprites.Length)
        {
            Debug.LogWarning($"[PlayerEmojiController] 유효하지 않은 이모지 인덱스입니다: {index}");
            return;
        }

        ShowEmoji(emojiSprites[index]);
    }

    // 스프라이트를 직접 받아 화면에 띄우는 오버로드 메서드
    public void ShowEmoji(Sprite sprite)
    {
        if (sprite == null || emojiSpriteRenderer == null) return;

        emojiSpriteRenderer.sprite = sprite;
        emojiSpriteRenderer.gameObject.SetActive(true);
        UpdateEmojiTransform();
        hideTimer = displayDuration;
    }
}