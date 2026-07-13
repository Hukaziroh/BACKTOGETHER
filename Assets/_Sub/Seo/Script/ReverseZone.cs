using UnityEngine;

public class ReverseZone : MonoBehaviour
{
    [Header("변경 주기 (초)")]
    public float toggleInterval = 5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.StartReverseToggle(toggleInterval);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.StopReverseToggle();
            }
        }
    }
}