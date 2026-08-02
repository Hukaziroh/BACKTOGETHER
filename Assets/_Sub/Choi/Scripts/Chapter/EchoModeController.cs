using UnityEngine;

public class EchoModeController : MonoBehaviour
{
    // 이제 여기에는 어떤 타이머나 페이드 계산도 필요 없습니다.
    // 셰이더가 EchoManager가 보낸 전역 변수를 알아서 가져옵니다.

    private SpriteRenderer parentSpriteRenderer;
    private SpriteRenderer outlineSpriteRenderer;

    void Start()
    {
        // 혹시라도 SpriteRenderer가 꺼져있다면 켜주는 역할만 합니다.
        outlineSpriteRenderer = GetComponent<SpriteRenderer>();
        if (outlineSpriteRenderer != null)
        {
            outlineSpriteRenderer.enabled = true;
        }

        // 부모 오브젝트의 SpriteRenderer를 가져옵니다.
        if (transform.parent != null)
        {
            parentSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }
    }

    void LateUpdate()
    {
        if (parentSpriteRenderer != null && outlineSpriteRenderer != null)
        {
            // 부모의 애니메이션 스프라이트, 반전(Flip), 레이어 순서를 실시간으로 동기화합니다.
            // (보이는 표시는 셰이더와 EchoManager가 전역 변수로 알아서 처리합니다)
            outlineSpriteRenderer.sprite = parentSpriteRenderer.sprite;
            outlineSpriteRenderer.flipX = parentSpriteRenderer.flipX;
            outlineSpriteRenderer.flipY = parentSpriteRenderer.flipY;
            outlineSpriteRenderer.sortingLayerID = parentSpriteRenderer.sortingLayerID;
            outlineSpriteRenderer.sortingOrder = parentSpriteRenderer.sortingOrder + 1;
        }
    }
}