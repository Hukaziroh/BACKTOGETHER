using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

public class BossCollisionHandler : NetworkBehaviour
{
    [Header("게임 오버 설정")]
    [Tooltip("플레이어와 닿은 후 씬을 재시작할 때까지의 대기 시간(초)")]
    public float restartDelay = 2f;

    private bool isRestarting = false;
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isRestarting) return;

        if (other.CompareTag("Player"))
        {
            TriggerGameOver();
        }
    }
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isRestarting) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerGameOver();
        }
    }

    [Server]
    private void TriggerGameOver()
    {
        isRestarting = true;
        Debug.Log("💀 보스가 플레이어를 잡았습니다! 2초 뒤 스테이지를 재시작합니다.");

        // 추가 연출을 넣고 싶다면 여기에서 보스 애니메이션을 멈추거나 효과음을 재생하세요.

        StartCoroutine(RestartStageRoutine());
    }

    private IEnumerator RestartStageRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        string currentSceneName = SceneManager.GetActiveScene().name;
        NetworkManager.singleton.ServerChangeScene(currentSceneName);
    }
}