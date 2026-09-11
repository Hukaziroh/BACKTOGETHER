using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class EyeRoomTrigger : NetworkBehaviour
{
    [Header("방 설정")]
    [Tooltip("기믹을 시작하기 위해 필요한 최소 플레이어 수 (4명)")]
    public int requiredPlayers = 4;
    public LayerMask playerLayer;

    [Header("연결할 오브젝트")]
    [Tooltip("플레이어가 모두 진입하면 활성화되어 입구를 막을 벽")]
    public GameObject blockingWall;
    [Tooltip("방에 다 들어오면 깨울 눈(Eye) 오브젝트")]
    public EyeGimmick eyeGimmick;

    private HashSet<NetworkIdentity> playersInZone = new HashSet<NetworkIdentity>();

    // 문이 닫혔는지 여부 (서버가 닫으면 클라이언트 화면에도 닫힘)
    [SyncVar(hook = nameof(OnRoomClosed))]
    private bool isRoomClosed = false;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isRoomClosed) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            NetworkIdentity identity = collision.GetComponent<NetworkIdentity>();
            if (identity != null)
            {
                playersInZone.Add(identity);
                CheckPlayers();
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isRoomClosed) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            NetworkIdentity identity = collision.GetComponent<NetworkIdentity>();
            if (identity != null)
            {
                playersInZone.Remove(identity);
            }
        }
    }

    [Server]
    private void CheckPlayers()
    {
        playersInZone.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);

        // 정원이 다 차면 문을 닫고 기믹 시작!
        if (playersInZone.Count >= requiredPlayers)
        {
            isRoomClosed = true;

            if (eyeGimmick != null)
            {
                eyeGimmick.StartEyeGimmick(); // 눈을 깨운다!
            }
        }
    }

    // 문 닫기 동기화 훅
    private void OnRoomClosed(bool oldVal, bool newVal)
    {
        if (newVal && blockingWall != null)
        {
            blockingWall.SetActive(true);
            // Debug.Log("방 문이 닫혔습니다!");
        }
    }
}