using UnityEngine;

public class EchoModeController : MonoBehaviour
{
    private SpriteRenderer parentSpriteRenderer;
    private SpriteRenderer outlineSpriteRenderer;

    // Face 관련 변수 추가
    private SpriteRenderer parentFaceSpriteRenderer;
    private SpriteRenderer faceOutlineSpriteRenderer;

    void Start()
    {
        // 1. 바디 Outline SpriteRenderer 설정
        outlineSpriteRenderer = GetComponent<SpriteRenderer>();
        if (outlineSpriteRenderer != null)
        {
            outlineSpriteRenderer.enabled = true;
        }

        // 2. 부모 오브젝트 및 FaceSprite 찾기
        if (transform.parent != null)
        {
            parentSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();

            // 부모 하위에 있는 FaceSprite 찾기
            Transform faceChild = transform.parent.Find("FaceSprite");
            if (faceChild != null)
            {
                parentFaceSpriteRenderer = faceChild.GetComponent<SpriteRenderer>();
            }
        }

        // 3. 자식으로 FaceOutline 오브젝트가 없다면 자동으로 생성
        Transform faceOutlineChild = transform.Find("FaceOutline");
        if (faceOutlineChild == null)
        {
            GameObject faceOutlineObj = new GameObject("FaceOutline");
            faceOutlineObj.transform.SetParent(transform);
            faceOutlineObj.transform.localPosition = Vector3.zero;
            faceOutlineObj.transform.localRotation = Quaternion.identity;
            faceOutlineObj.transform.localScale = Vector3.one;

            faceOutlineSpriteRenderer = faceOutlineObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            faceOutlineSpriteRenderer = faceOutlineChild.GetComponent<SpriteRenderer>();
        }

        // 바디 외곽선과 동일한 셰이더(마테리얼) 공유
        if (outlineSpriteRenderer != null && faceOutlineSpriteRenderer != null)
        {
            faceOutlineSpriteRenderer.sharedMaterial = outlineSpriteRenderer.sharedMaterial;
        }
    }

    void LateUpdate()
    {
        // 네트워크 스폰 타이밍에 따라 Start() 시점에 부모 쪽 SpriteRenderer가
        // 아직 준비 안 돼있을 수 있다. 못 찾았으면 매 프레임 재시도해서 자기복구한다.
        if (parentSpriteRenderer == null && transform.parent != null)
        {
            parentSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }

        if (parentFaceSpriteRenderer == null && transform.parent != null)
        {
            Transform faceChild = transform.parent.Find("FaceSprite");
            if (faceChild != null) parentFaceSpriteRenderer = faceChild.GetComponent<SpriteRenderer>();
        }

        // 1. 바디(몸통) 외곽선 동기화
        if (parentSpriteRenderer != null && outlineSpriteRenderer != null)
        {
            outlineSpriteRenderer.sprite = parentSpriteRenderer.sprite;
            outlineSpriteRenderer.flipX = parentSpriteRenderer.flipX;
            outlineSpriteRenderer.flipY = parentSpriteRenderer.flipY;
            outlineSpriteRenderer.sortingLayerID = parentSpriteRenderer.sortingLayerID;
            outlineSpriteRenderer.sortingOrder = parentSpriteRenderer.sortingOrder + 1;
            outlineSpriteRenderer.enabled = parentSpriteRenderer.enabled;
        }

        // 2. Face(표정) 외곽선 동기화
        if (parentFaceSpriteRenderer != null && faceOutlineSpriteRenderer != null)
        {
            faceOutlineSpriteRenderer.sprite = parentFaceSpriteRenderer.sprite;
            faceOutlineSpriteRenderer.flipX = parentFaceSpriteRenderer.flipX;
            faceOutlineSpriteRenderer.flipY = parentFaceSpriteRenderer.flipY;
            faceOutlineSpriteRenderer.sortingLayerID = parentFaceSpriteRenderer.sortingLayerID;

            // 표정 외곽선은 몸통 외곽선보다 살짝 위에 그려지도록 설정
            if (outlineSpriteRenderer != null)
            {
                faceOutlineSpriteRenderer.sortingOrder = outlineSpriteRenderer.sortingOrder + 1;
            }

            // 합체 등으로 인해 본체 FaceSprite가 꺼졌을 때(enabled = false) 외곽선도 같이 꺼지도록 동기화
            faceOutlineSpriteRenderer.enabled = parentFaceSpriteRenderer.enabled && (parentSpriteRenderer != null && parentSpriteRenderer.enabled);
        }
    }
}