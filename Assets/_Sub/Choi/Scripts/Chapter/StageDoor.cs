using UnityEngine;
using Mirror;
using System.Collections;

public class StageDoor : NetworkBehaviour
{
    public GameObject fanfarePrefab;
    public string lobbySceneName = "Lobby";
    public GameObject clearTextUI; // 문 자식인 Canvas 오브젝트
    public float floatSpeed = 2f;  // 텍스트 상승 속도

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("2D 충돌 감지 성공!");
            RpcTriggerClearEffect();
            StartCoroutine(WaitAndLoadScene());
        }
    }

    [ClientRpc]
    private void RpcTriggerClearEffect()
    {
        // 1. 파티클 재생
        if (fanfarePrefab != null)
            Instantiate(fanfarePrefab, transform.position, Quaternion.identity);

        // 2. 텍스트 활성화 및 상승 효과 시작
        if (clearTextUI != null)
        {
            clearTextUI.SetActive(true);
            StartCoroutine(FloatTextRoutine());
        }
    }

    // 텍스트를 위로 올리고 1.5초 뒤에 끄는 코루틴
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
        clearTextUI.transform.localPosition = startPos; // 위치 초기화 (다음에 다시 쓸 때 대비)
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(2.0f);
        if (isServer) // 씬 이동은 서버만 가능
        {
            NetworkManager.singleton.ServerChangeScene(lobbySceneName);
        }
    }
}