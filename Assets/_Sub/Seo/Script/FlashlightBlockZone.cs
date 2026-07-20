using UnityEngine;

public class FlashlightBlockZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFlashlight pf = other.GetComponent<PlayerFlashlight>();
            if (pf != null)
            {
                pf.SetFlashlightPermission(false); 
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFlashlight pf = other.GetComponent<PlayerFlashlight>();
            if (pf != null)
            {
                pf.SetFlashlightPermission(true); 
            }
        }
    }
}