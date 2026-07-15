using UnityEngine;
using UnityEngine.Tilemaps;

public class AutoSpikeLine : MonoBehaviour
{
    public Tilemap spikeTilemap;     // 가시 타일맵
    public Material lineMaterial;    // 벽과 같은 글로우 머티리얼
    public float lineWidth = 0.1f;

    void Start()
    {
        // 1. 타일맵의 모든 위치를 순회
        foreach (var pos in spikeTilemap.cellBounds.allPositionsWithin)
        {
            if (spikeTilemap.HasTile(pos))
            {
                CreateSpikeLine(pos);
            }
        }
    }

    void CreateSpikeLine(Vector3Int gridPos)
    {
        GameObject lineObj = new GameObject("AutoSpike");
        lineObj.transform.SetParent(transform);

        // 타일의 월드 위치 계산
        Vector3 worldPos = spikeTilemap.CellToWorld(gridPos);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 3; // 삼각형 가시 모양 (3점)

        // 가시 모양 포인트 지정 (0~1 사이)
        lr.SetPosition(0, worldPos + new Vector3(0, 0, 0));       // 왼쪽 아래
        lr.SetPosition(1, worldPos + new Vector3(0.5f, 1f, 0));   // 꼭대기
        lr.SetPosition(2, worldPos + new Vector3(1f, 0, 0));      // 오른쪽 아래
    }
}