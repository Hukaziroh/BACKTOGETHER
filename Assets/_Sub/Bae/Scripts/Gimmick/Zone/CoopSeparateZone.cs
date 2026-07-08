using UnityEngine;
using Mirror;

public class CoopSeparateZone : NetworkBehaviour
{
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCombineHandler combine = other.GetComponent<PlayerCombineHandler>();

            if (combine != null && combine.isCombined)
            {
                combine.StopCombineMode();
                Debug.Log($"[{other.name}] 합체 해제 존 통과 -> 강제 분리 완료!");
            }
        }
    }
}