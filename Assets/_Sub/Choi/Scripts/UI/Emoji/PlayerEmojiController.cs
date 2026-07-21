using UnityEngine;
using System.Collections;

public class PlayerEmojiController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    [Tooltip("플레이어 머리 위에 이모지를 보여줄 SpriteRenderer 오브젝트")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("크기 및 위치 설정")]
    [Tooltip("이미지 원본 크기와 상관없이 맞출 표준 크기 (월드 유닛 기준)")]
    [SerializeField] private float targetSize = 0.5f;

    [Tooltip("캐릭터 머리 위로 띄울 높이 오프셋")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);

    [Header("시간 설정")]
    [Tooltip("이모지가 머리 위에 유지되는 시간 (초)")]
    [SerializeField] private float displayDuration = 2.0f;

    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        EmojiRadialMenu.OnEmojiSelected += HandleEmojiSelected;
    }

    private void OnDisable()
    {
        EmojiRadialMenu.OnEmojiSelected -= HandleEmojiSelected;
    }

    private void Start()
    {
        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void HandleEmojiSelected(Sprite selectedSprite)
    {
        if (selectedSprite == null || emojiSpriteRenderer == null) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 1. 스프라이트 교체
        emojiSpriteRenderer.sprite = selectedSprite;

        // 2. [핵심] 이미지 원본 크기와 비율에 구애받지 않고 항상 일정한 크기로 자동 균일화
        Vector2 spriteSize = selectedSprite.bounds.size;
        float maxDimension = Mathf.Max(spriteSize.x, spriteSize.y);
        if (maxDimension > 0f)
        {
            float scaleFactor = targetSize / maxDimension;
            emojiSpriteRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
        else
        {
            emojiSpriteRenderer.transform.localScale = Vector3.one;
        }

        // 3. 머리 위 위치(오프셋) 적용
        emojiSpriteRenderer.transform.localPosition = headOffset;

        // 4. 활성화
        emojiSpriteRenderer.gameObject.SetActive(true);

        // 5. 타이머 시작
        hideCoroutine = StartCoroutine(HideEmojiRoutine());
    }

    private IEnumerator HideEmojiRoutine()
    {
        yield return new WaitForSeconds(displayDuration);

        if (emojiSpriteRenderer != null)
        {
            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }
}