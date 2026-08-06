using UnityEngine;
using Mirror;

public class ZoneTrigger : MonoBehaviour
{
    public GameObject echoManager; // 씬에 있는 EchoManager 오브젝트를 드래그해서 넣으세요.

    void Start()
    {
        // 시작할 때는 파동이 꺼져 있어야 합니다.
        if (echoManager != null)
            echoManager.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 진입하면 파동 관리자를 켭니다.
            echoManager.SetActive(true);
            UpdateEchoWaveOrigin();
        }
    }

    // 파동은 "내 번호 + 1번" 플레이어 위치를 중심으로 나간다.
    // (1번은 2번을, 2번은 3번을... 4번은 다시 1번을 - 랜덤 없이 항상 고정된 다른 사람 기준)
    private void UpdateEchoWaveOrigin()
    {
        if (echoManager == null) return;
        if (NetworkClient.localPlayer == null) return;

        CoopPlayerIdentity myIdentity = NetworkClient.localPlayer.GetComponent<CoopPlayerIdentity>();
        if (myIdentity == null) return;

        int partnerIndex = (myIdentity.playerIndex + 1) % 4;
        if (!CoopPlayerIdentity.players.TryGetValue(partnerIndex, out CoopPlayerIdentity partner)) return;
        if (partner == null) return;

        EchoManager manager = echoManager.GetComponent<EchoManager>();
        if (manager != null) manager.SetWaveOrigin(partner.transform);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 나가면 파동 관리자를 끕니다.
            echoManager.SetActive(false);
        }
    }
}