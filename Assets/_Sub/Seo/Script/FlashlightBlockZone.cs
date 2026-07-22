using UnityEngine;
using System.Collections.Generic;

public class FlashlightBlockZone : MonoBehaviour
{
    [Header("기믹 설정")]
    [Tooltip("이 공간의 락을 해제할 버튼을 드래그해서 넣으세요.")]
    public CoopButton overrideButton;

    private HashSet<PlayerFlashlight> playersInZone = new HashSet<PlayerFlashlight>();

    private bool isPermanentlyUnlocked = false;

    private void Update()
    {
        if (overrideButton == null) return;

        if (isPermanentlyUnlocked) return;

        playersInZone.RemoveWhere(pf => pf == null);

        if (overrideButton.isPressed)
        {
            isPermanentlyUnlocked = true; 
            foreach (PlayerFlashlight pf in playersInZone)
            {
                pf.SetFlashlightPermission(true); 

                pf.TurnOnFlashlight();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFlashlight pf = other.GetComponent<PlayerFlashlight>();
            if (pf != null)
            {
                playersInZone.Add(pf);

                if (!isPermanentlyUnlocked)
                {
                    pf.SetFlashlightPermission(false);
                }
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
                playersInZone.Remove(pf);

                pf.SetFlashlightPermission(true);
            }
        }
    }
}