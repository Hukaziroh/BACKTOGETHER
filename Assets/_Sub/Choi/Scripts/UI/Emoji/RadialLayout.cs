using UnityEngine;

public class RadialLayout : MonoBehaviour
{
    [Header("배치 설정")]
    [Tooltip("원의 반지름 (크기)")]
    public float radius = 100f;

    void OnValidate()
    {
        ArrangeChildren();
    }

    [ContextMenu("아이콘 원형으로 정렬하기")]
    public void ArrangeChildren()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        float angleStep = 360f / childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            // 각도를 라디안으로 변환 (12시 방향이 시작점이 되도록 계산)
            float angle = (i * angleStep - 90f) * Mathf.Deg2Rad;

            // 좌표 계산 (X, Y)
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            // RectTransform 위치 설정
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(x, -y); // 유니티 UI Y축 반전 고려
            }
        }
    }
}