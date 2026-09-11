using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopBlockWall : NetworkBehaviour
{
    [Header("생성될 벽 오브젝트")]
    [Tooltip("막고 싶은 위치에 만들어둔 벽(비활성화 상태)을 끌어다 넣으세요.")]
    public GameObject wallObject;

    [Header("작동에 필요한 인원")]
    public int requiredPlayers = 4;

    [SyncVar(hook = nameof(OnWallActivated))]
    private bool isWallActive = false;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isWallActive && wallObject != null)
        {
            wallObject.SetActive(true);
        }
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isWallActive) return;

        if (other.CompareTag("Player"))
        {
            playersInZone.Add(other.gameObject);
            CheckPlayers();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (isWallActive) return;

        if (other.CompareTag("Player"))
        {
            playersInZone.Remove(other.gameObject);
        }
    }

    [Server]
    private void CheckPlayers()
    {
        playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy);

        if (playersInZone.Count >= requiredPlayers)
        {
            isWallActive = true; 
        }
    }
    private void OnWallActivated(bool oldVal, bool newVal)
    {
        if (newVal && wallObject != null)
        {
            wallObject.SetActive(true);
            // Debug.Log("모든 플레이어가 진입하여 퇴로가 차단되었습니다!");
        }
    }
}