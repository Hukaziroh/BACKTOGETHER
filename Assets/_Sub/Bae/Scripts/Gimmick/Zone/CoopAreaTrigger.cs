using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Mirror;

public class CoopAreaTrigger : NetworkBehaviour
{
    [Header("설정")]
    [SerializeField] private int requiredPlayers = 1; 
    [SerializeField] private LayerMask playerLayer;    

    [Header("이벤트 (서버 트리거 시 실행)")]
    public UnityEvent OnAllPlayersEntered;
    public UnityEvent OnPlayersDeficit;
    private readonly HashSet<NetworkIdentity> playersInTrigger = new HashSet<NetworkIdentity>();
    private bool isTriggered = false;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            NetworkIdentity networkIdentity = collision.GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                playersInTrigger.Add(networkIdentity);
                CheckPlayerCount();
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            NetworkIdentity networkIdentity = collision.GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                if (playersInTrigger.Contains(networkIdentity))
                {
                    playersInTrigger.Remove(networkIdentity);
                    CheckPlayerCount();
                }
            }
        }
    }

    [Server]
    private void CheckPlayerCount()
    {
        if (playersInTrigger.Count >= requiredPlayers && !isTriggered)
        {
            isTriggered = true;
            RpcExecuteEnteredEvent();
        }
        else if (playersInTrigger.Count < requiredPlayers && isTriggered)
        {
            // 이 부분을 주석 해제하면 일회성이 아닌, 플레이어가 나가면 다시 벽이 닫히는 구조로 쓸 수 있습니다.
            // isTriggered = false;
            // RpcExecuteDeficitEvent();
        }
    }

    [ClientRpc]
    private void RpcExecuteEnteredEvent()
    {
        OnAllPlayersEntered?.Invoke();
    }

    [ClientRpc]
    private void RpcExecuteDeficitEvent()
    {
        OnPlayersDeficit?.Invoke();
    }
    private void OnDisable()
    {
        if (isServer)
        {
            playersInTrigger.Clear();
            isTriggered = false;
        }
    }
}