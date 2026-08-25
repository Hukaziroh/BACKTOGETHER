using UnityEngine;
using TMPro;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class IntervalTrigger : NetworkBehaviour
{
    [Header("설정")]
    public float toggleInterval = 5f;

    [Min(0f)]
    [Tooltip("구역을 벗어난 뒤 좌우반전이 다시 적용되기까지의 시간")]
    public float reentryCooldown = 1f;

    [Header("UI 연결")]
    public TMP_Text timerText;

    [SyncVar]
    public bool isForward = true;

    [SyncVar]
    private double nextChangeNetworkTime;

    private bool isLocalPlayerInside = false;

    private Dictionary<Collider2D, float> reentryBlockedUntil = new Dictionary<Collider2D, float>();
    private Dictionary<Collider2D, Coroutine> reentryRoutines = new Dictionary<Collider2D, Coroutine>();

    private void Start()
    {
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        nextChangeNetworkTime = NetworkTime.time + toggleInterval;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (reentryRoutines.TryGetValue(collision, out Coroutine routine))
        {
            StopCoroutine(routine);
            reentryRoutines.Remove(collision);
        }

        if (reentryBlockedUntil.TryGetValue(collision, out float blockedUntil))
        {
            float remainingCooldown = blockedUntil - Time.time;
            if (remainingCooldown > 0f)
            {
                reentryRoutines[collision] = StartCoroutine(
                    ActivateAfterCooldown(collision, remainingCooldown)
                );
                return;
            }

            reentryBlockedUntil.Remove(collision);
        }

        ActivatePlayer(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (reentryRoutines.TryGetValue(collision, out Coroutine routine))
        {
            StopCoroutine(routine);
            reentryRoutines.Remove(collision);
        }

        reentryBlockedUntil[collision] = Time.time + Mathf.Max(0f, reentryCooldown);
        DeactivatePlayer(collision);
    }

    private IEnumerator ActivateAfterCooldown(Collider2D collision, float delay)
    {
        yield return new WaitForSeconds(delay);

        reentryRoutines.Remove(collision);
        reentryBlockedUntil.Remove(collision);

        if (collision != null)
            ActivatePlayer(collision);
    }

    private void ActivatePlayer(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetCurrentReverseZone(this);
        }

        NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isLocalPlayer)
        {
            isLocalPlayerInside = true;
            if (timerText != null) timerText.gameObject.SetActive(true);
        }
    }

    private void DeactivatePlayer(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ClearCurrentReverseZone();
        }

        NetworkIdentity netIdentity = collision.GetComponent<NetworkIdentity>();
        if (netIdentity != null && netIdentity.isLocalPlayer)
        {
            isLocalPlayerInside = false;
            if (timerText != null) timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isServer)
        {
            if (NetworkTime.time >= nextChangeNetworkTime)
            {
                isForward = !isForward;
                nextChangeNetworkTime = NetworkTime.time + toggleInterval;
            }
        }

        if (!isLocalPlayerInside || timerText == null) return;

        float timeLeft = (float)(nextChangeNetworkTime - NetworkTime.time);
        if (timeLeft < 0) timeLeft = 0;

        string arrowChar = isForward ? "->" : "<-";
        timerText.text = $"{arrowChar}";

        if (timeLeft > 3f) timerText.color = Color.green;
        else if (timeLeft > 1.5f) timerText.color = Color.yellow;
        else timerText.color = Color.red;
    }
}
