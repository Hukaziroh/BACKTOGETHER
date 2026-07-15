using UnityEngine;

// LineRenderer는 더 이상 부모에 고정하지 않고 자식으로 생성합니다.
[RequireComponent(typeof(CompositeCollider2D))]
public class MapOutline : MonoBehaviour
{
    public Material lineMaterial; // 라인 렌더러에 입힐 머티리얼을 연결하세요.
    public float lineWidth = 0.1f;

    void Start()
    {
        var col = GetComponent<CompositeCollider2D>();

        // CompositeCollider가 생성한 모든 경로를 순회합니다.
        for (int i = 0; i < col.pathCount; i++)
        {
            int pointCount = col.GetPathPointCount(i);
            Vector2[] points = new Vector2[pointCount];
            col.GetPath(i, points);

            // 경로마다 별도의 LineRenderer를 가진 자식 오브젝트 생성
            GameObject lineObj = new GameObject("Outline_Path_" + i);
            lineObj.transform.SetParent(this.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.useWorldSpace = false; // 부모 기준 좌표 사용
            line.loop = true;          // 닫힌 도형(섬/발판)이므로 루프 설정
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = pointCount;

            // 포인트 설정
            for (int j = 0; j < pointCount; j++)
            {
                line.SetPosition(j, new Vector3(points[j].x, points[j].y, 0));
            }
        }
    }
}