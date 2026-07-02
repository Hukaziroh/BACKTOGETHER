using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;

public class StageDoor : NetworkBehaviour
{
    public GameObject fanfarePrefab;
    public string lobbySceneName = "Lobby"; // 로비 씬 이름
    public GameObject clearTextUI; // 텍스트 UI (미리 비활성화 해둘 것)

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RpcTriggerClearEffect(); // 모든 클라이언트에게 이펙트/텍스트 명령
            StartCoroutine(WaitAndLoadScene()); // 서버에서 씬 이동 예약
        }
    }

    [ClientRpc]
    private void RpcTriggerClearEffect()
    {
        // 1. 파티클 재생
        if (fanfarePrefab != null) Instantiate(fanfarePrefab, transform.position, Quaternion.identity);

        // 2. 텍스트 활성화
        if (clearTextUI != null)
        {
            clearTextUI.SetActive(true);
            // 텍스트가 솟아오르는 애니메이션을 실행하거나, 
            // 아래의 간단한 스크립트를 활용하세요.
        }
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(2.0f); // 2초 대기
        NetworkManager.singleton.ServerChangeScene(lobbySceneName);
    }
}