using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic; // HashSet 사용을 위해 추가

public class StageDoor : NetworkBehaviour
{
    public GameObject fanfarePrefab;
    public string lobbySceneName = "Lobby";
    public GameObject clearTextUI;
    public float floatSpeed = 2f;

    // 도착한 플레이어들을 저장할 서버 전용 리스트
    private HashSet<uint> arrivedPlayers = new HashSet<uint>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // NetworkIdentity를 통해 플레이어 고유 ID(netId) 가져오기
        NetworkIdentity identity = collision.GetComponent<NetworkIdentity>();
        if (identity != null && collision.CompareTag("Player"))
        {
            // 이미 도착한 플레이어인지 확인
            if (!arrivedPlayers.Contains(identity.netId))
            {
                arrivedPlayers.Add(identity.netId);
                Debug.Log($"플레이어 {identity.netId} 도착! ({arrivedPlayers.Count} / {NetworkServer.connections.Count})");

                // 모든 플레이어가 도착했는지 확인
                if (arrivedPlayers.Count >= NetworkServer.connections.Count)
                {
                    RpcTriggerClearEffect();
                    StartCoroutine(WaitAndLoadScene());
                }
            }
        }
    }

    [ClientRpc]
    private void RpcTriggerClearEffect()
    {
        if (fanfarePrefab != null)
            Instantiate(fanfarePrefab, transform.position, Quaternion.identity);

        if (clearTextUI != null)
        {
            clearTextUI.SetActive(true);
            StartCoroutine(FloatTextRoutine());
        }
    }

    private IEnumerator FloatTextRoutine()
    {
        float timer = 0f;
        Vector3 startPos = clearTextUI.transform.localPosition;

        while (timer < 1.5f)
        {
            timer += Time.deltaTime;
            clearTextUI.transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
            yield return null;
        }

        clearTextUI.SetActive(false);
        clearTextUI.transform.localPosition = startPos;
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(2.0f);
        if (isServer)
        {
            NetworkManager.singleton.ServerChangeScene(lobbySceneName);
        }
    }
}