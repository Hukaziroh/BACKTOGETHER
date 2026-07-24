using UnityEngine;
using TMPro;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class IntervalTrigger : NetworkBehaviour
{
    [Header("설정")]
    public float toggleInterval = 5f;

    [Header("UI 연결")]
    public TMP_Text timerText;

    [SyncVar]
    public bool isForward = true;

    [SyncVar]
    private double nextChangeNetworkTime;

    private bool isLocalPlayerInside = false;

    private Dictionary<Collider2D, Coroutine> exitRoutines = new Dictionary<Collider2D, Coroutine>();

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
        if (collision.CompareTag("Player"))
        {
            if (exitRoutines.ContainsKey(collision))
            {
                StopCoroutine(exitRoutines[collision]);
                exitRoutines.Remove(collision);
                return; 
            }

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
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!exitRoutines.ContainsKey(collision))
            {
                exitRoutines[collision] = StartCoroutine(ExitDelayRoutine(collision));
            }
        }
    }

    private IEnumerator ExitDelayRoutine(Collider2D collision)
    {
        // 0.15초 대기 (이 시간 동안은 계속 구역 안에 있는 것으로 판정)
        yield return new WaitForSeconds(0.15f);

        if (collision != null)
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

        exitRoutines.Remove(collision);
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