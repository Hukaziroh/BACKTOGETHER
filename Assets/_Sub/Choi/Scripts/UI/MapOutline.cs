using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D), typeof(LineRenderer))]
public class MapOutline : MonoBehaviour
{
    void Start()
    {
        var col = GetComponent<CompositeCollider2D>();
        var line = GetComponent<LineRenderer>();

        // CompositeCollider에서 외곽선 데이터를 가져와서 LineRenderer에 적용
        // 경로가 여러 개일 수 있으니 첫 번째(0번) 경로를 가져옵니다
        int pointCount = col.GetPathPointCount(0);
        Vector2[] points = new Vector2[pointCount];
        col.GetPath(0, points);

        line.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            line.SetPosition(i, new Vector3(points[i].x, points[i].y, 0));
        }
    }
}