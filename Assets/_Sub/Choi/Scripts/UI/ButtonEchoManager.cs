using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class ButtonEchoManager : MonoBehaviour
{
    [Header("에코 설정")]
    public Material echoMaterial;    // 'WallEcho' 머티리얼
    public float lineWidth = 0.12f;  // 라인 두께
    public Vector3 offset = Vector3.zero;

    [Header("모양 자동 맞춤")]
    [Tooltip("체크하면 스프라이트의 실제 크기/비율에 맞춰 외곽선이 자동으로 잡힙니다.")]
    public bool autoFitFromSprite = true;

    [Header("수동 미세 조절 (Auto Fit 해제 시 적용)")]
    public float baseHalfWidth = 0.7f;
    public float baseHeight = 0.25f;
    public float domeRadius = 0.4f;
    public float domeCenterY = 0.25f;

    void Awake()
    {
        SetupButtonOutline();
    }

    void SetupButtonOutline()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.material = echoMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false; // 로컬 좌표 기준 (버튼 움직임 동기화)
        lineRenderer.loop = true;

        // 스프라이트 바운스를 기반으로 버튼 비율 자동 계산 (하단 피벗 기준)
        if (autoFitFromSprite)
        {
            Bounds bounds = spriteRenderer.sprite.bounds;
            float spriteW = bounds.size.x;
            float spriteH = bounds.size.y;

            // 이미지 비율에 맞춘 최적의 수치 자동 할당
            baseHalfWidth = spriteW * 0.48f;
            baseHeight = spriteH * 0.45f;
            domeRadius = spriteW * 0.32f;
            domeCenterY = spriteH * 0.45f;
        }

        int domeSegments = 10;
        int totalPoints = domeSegments + 5; // 하단(2) + 어깨(2) + 돔(분할수) + 연결
        lineRenderer.positionCount = totalPoints;

        int index = 0;

        // 1. 하단 바닥선 (좌측 -> 우측)
        lineRenderer.SetPosition(index++, new Vector3(-baseHalfWidth, 0f, 0f) + offset);
        lineRenderer.SetPosition(index++, new Vector3(baseHalfWidth, 0f, 0f) + offset);

        // 2. 우측 어깨선 (베이스 높이까지 수직)
        lineRenderer.SetPosition(index++, new Vector3(baseHalfWidth, baseHeight, 0f) + offset);

        // 3. 상단 돔 곡선 (우측에서 좌측으로 반원 아치 생성)
        for (int i = 0; i <= domeSegments; i++)
        {
            float angle = Mathf.Lerp(0f, Mathf.PI, (float)i / domeSegments);
            float x = Mathf.Cos(angle) * domeRadius;
            float y = domeCenterY + (Mathf.Sin(angle) * domeRadius);
            lineRenderer.SetPosition(index++, new Vector3(x, y, 0f) + offset);
        }

        // 4. 좌측 어깨선으로 마무리
        lineRenderer.SetPosition(index++, new Vector3(-baseHalfWidth, baseHeight, 0f) + offset);
    }
}