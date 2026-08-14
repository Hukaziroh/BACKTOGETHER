using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

public class BossCollisionHandler : NetworkBehaviour
{
    [Header("게임 오버 설정")]
    [Tooltip("플레이어와 닿은 후 씬을 재시작할 때까지의 대기 시간(초)")]
    public float restartDelay = 2f;

    [Header("사망 사운드")]
    public AudioClip deathSound;
    [Range(0f, 3f)] public float deathSoundVolume = 1f;
    public float soundMinDistance = 9f;
    public float soundMaxDistance = 40f;

    private bool isRestarting = false;
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isRestarting) return;

        if (other.CompareTag("Player"))
        {
            TriggerGameOver(other.transform.position);
        }
    }
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isRestarting) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerGameOver(collision.transform.position);
        }
    }

    [Server]
    private void TriggerGameOver(Vector3 deathPosition)
    {
        isRestarting = true;
        Debug.Log("💀 보스가 플레이어를 잡았습니다! 2초 뒤 스테이지를 재시작합니다.");

        RpcPlayDeathSound(deathPosition);

        StartCoroutine(RestartStageRoutine());
    }

    [ClientRpc]
    private void RpcPlayDeathSound(Vector3 position)
    {
        PlayerSoundUtility.PlayPositional(position, deathSound, deathSoundVolume, soundMinDistance, soundMaxDistance);
    }

    private IEnumerator RestartStageRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        string currentSceneName = SceneManager.GetActiveScene().name;
        NetworkManager.singleton.ServerChangeScene(currentSceneName);
    }
}