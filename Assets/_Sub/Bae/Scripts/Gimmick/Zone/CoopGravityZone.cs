using UnityEngine;
using Mirror;

public class CoopGravityZone : MonoBehaviour
{
    [Header("중력 제한 구역 설정")]
    [Tooltip("이 구역을 벗어날 때 캐릭터의 중력을 원래대로(정상) 돌려놓을지 여부")]
    public bool resetGravityOnExit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerGravityController gravityModule = other.GetComponent<PlayerGravityController>();

            if (gravityModule != null)
            {
                gravityModule.canInvertGravity = true; 
                Debug.Log($"[{other.name}] 중력 반전 가능 구역 진입.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerGravityController gravityModule = other.GetComponent<PlayerGravityController>();

            if (gravityModule != null)
            {
                gravityModule.canInvertGravity = false; 
                Debug.Log($"[{other.name}] 중력 반전 가능 구역 이탈.");

                if (resetGravityOnExit)
                {
                    gravityModule.ResetGravity();
                }
            }
        }
    }
}