using UnityEngine;
using System.Collections.Generic;

public class SpikeEchoParentManager : MonoBehaviour
{
    [Header("에코 설정")]
    public Material echoMaterial;    // 'WallEcho' 머티리얼
    public float lineWidth = 0.15f;  // 라인 두께

    [Header("위치 미세 조정 (가시와 윤곽선이 어긋날 때 조절)")]
    public Vector3 spikeTriangleOffset = new Vector3(0, -0.5f, 0);

    [Header("가시 직접 등록 리스트")]
    [Tooltip("실제 가시(자식의 자식) 오브젝트들을 이 리스트에 드래그하여 넣으세요.")]
    public List<Transform> registeredSpikes = new List<Transform>();

    void Awake()
    {
        SetupSpikes();
    }

    private void SetupSpikes()
    {
        foreach (var spike in registeredSpikes)
        {
            if (spike == null) continue;

            LineRenderer lr = spike.GetComponent<LineRenderer>();
            if (lr == null)
            {
                lr = spike.gameObject.AddComponent<LineRenderer>();
            }

            lr.material = echoMaterial;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = false; // 자식 로컬 좌표 사용
            lr.loop = true;
            lr.positionCount = 3;

            // 가시 모양을 위한 로컬 좌표 설정
            Vector3 p0 = new Vector3(-0.5f, 0f, 0f) + spikeTriangleOffset; // 왼쪽 아래
            Vector3 p1 = new Vector3(0f, 1f, 0f) + spikeTriangleOffset;    // 꼭대기
            Vector3 p2 = new Vector3(0.5f, 0f, 0f) + spikeTriangleOffset;  // 오른쪽 아래

            lr.SetPosition(0, p0);
            lr.SetPosition(1, p1);
            lr.SetPosition(2, p2);

            // 초기 상태는 기본적으로 켬
            lr.enabled = true;
        }
    }

    // SpikeToggleButton에서 호출하는 함수 (active: true = 가시가 나와있음, false = 가시가 들어감)
    public void SetEchoActive(bool active)
    {
        foreach (var spike in registeredSpikes)
        {
            if (spike == null) continue;

            LineRenderer lr = spike.GetComponent<LineRenderer>();
            if (lr != null)
            {
                // 가시가 내려가면(false) 윤곽선 LineRenderer를 끄고, 올라오면(true) 켬
                lr.enabled = active;
            }
        }
    }
}