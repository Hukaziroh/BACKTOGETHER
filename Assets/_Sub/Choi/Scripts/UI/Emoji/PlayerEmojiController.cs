using UnityEngine;
using System.Collections;

public class PlayerEmojiController : MonoBehaviour
{
    [Header("컴포넌트 연결")]
    [Tooltip("플레이어 머리 위에 이모지를 보여줄 SpriteRenderer 오브젝트")]
    [SerializeField] private SpriteRenderer emojiSpriteRenderer;

    [Header("크기 및 위치 설정")]
    [Tooltip("이미지 원본 비율과 상관없이 맞출 정사각형 표준 크기 (월드 유닛 기준)")]
    [SerializeField] private float targetSize = 0.5f;

    [Tooltip("캐릭터 머리 위로 띄울 높이 오프셋")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);

    [Header("시간 설정")]
    [Tooltip("이모지가 머리 위에 유지되는 시간 (초)")]
    [SerializeField] private float displayDuration = 2.0f;

    private Transform parentTransform;
    private Transform emojiTransform;
    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        EmojiRadialMenu.OnEmojiSelected += HandleEmojiSelected;
    }

    private void OnDisable()
    {
        EmojiRadialMenu.OnEmojiSelected -= HandleEmojiSelected;
    }

    private void Awake()
    {
        parentTransform = transform;

        if (emojiSpriteRenderer == null)
        {
            emojiSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (emojiSpriteRenderer != null)
        {
            emojiTransform = emojiSpriteRenderer.transform;

            // ★ 핵심: 부모-자식 관계를 끊어버려 캐릭터의 좌우 반전(스케일 변경) 시 깜빡임 원천 차단
            emojiTransform.SetParent(null);

            emojiSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null)
        {
            if (emojiSpriteRenderer != null)
                Destroy(emojiSpriteRenderer.gameObject);
            return;
        }

        // 이모지가 켜져있는 동안 위치 실시간 추적 (좌우 반전 시 오프셋 위치 보정)
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

    private void HandleEmojiSelected(Sprite selectedSprite)
    {
        if (selectedSprite == null || emojiSpriteRenderer == null) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 1. 스프라이트 교체
        emojiSpriteRenderer.sprite = selectedSprite;

        // 2. 원본 비율 무시하고 무조건 정사각형 크기로 설정
        float scaleX = targetSize;
        float scaleY = targetSize;

        // 3. 캐릭터가 바라보는 방향(좌우 반전)에 맞춰 X축 부호 적용
        if (parentTransform != null && parentTransform.localScale.x < 0)
        {
            scaleX = -Mathf.Abs(scaleX);
        }
        else
        {
            scaleX = Mathf.Abs(scaleX);
        }

        emojiTransform.localScale = new Vector3(scaleX, scaleY, 1f);

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