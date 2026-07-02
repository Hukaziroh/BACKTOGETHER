using UnityEngine;

public class ReverseZone : MonoBehaviour
{
    [Header("반전 지속시간")]
    public float reverseDuration = 5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if(player != null)
            {
                player.StartReverseControl(reverseDuration);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if(player != null)
            {
                player.StopReverseControl();
            }
        }
    }
}
