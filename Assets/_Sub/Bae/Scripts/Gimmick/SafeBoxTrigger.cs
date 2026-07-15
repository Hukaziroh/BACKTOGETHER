using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class SafeBoxTrigger : MonoBehaviour
{
    public readonly HashSet<GameObject> playersInBox = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkServer.active) return;

        if (other.CompareTag("Player"))
        {
            playersInBox.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!NetworkServer.active) return;

        if (other.CompareTag("Player"))
        {
            playersInBox.Remove(other.gameObject);
        }
    }

    private void Update()
    {
        if (!NetworkServer.active) return;

        playersInBox.RemoveWhere(p => p == null || !p.activeInHierarchy);
    }
}