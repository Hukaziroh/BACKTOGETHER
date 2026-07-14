using UnityEngine;

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
        }
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