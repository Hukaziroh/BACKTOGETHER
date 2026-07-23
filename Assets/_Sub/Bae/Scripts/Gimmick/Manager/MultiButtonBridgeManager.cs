using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MultiButtonBridgeManager : NetworkBehaviour
{
    [Header("연결할 발판들 (CoopButton)")]
    [Tooltip("이 다리를 제어할 양쪽 버튼을 배열에 넣어주세요.")]
    public CoopButton[] connectedButtons;

    [Header("다리 설정")]
    public GameObject bridgePrefab;
    public Transform startPoint;
    public int maxBridgeLength = 5;
    public float buildDelay = 0.15f;
    public float tileWidth = 1f;

    [SyncVar]
    private bool isBridgeActive = false;

    private List<GameObject> spawnedBridges = new List<GameObject>();
    private Coroutine bridgeCoroutine;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var btn in connectedButtons)
        {
            if (btn != null)
            {
                btn.OnButtonStateChangedEvent += CheckAllButtons;
            }
        }
    }

    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        bool isAnyPressed = false;

        foreach (var btn in connectedButtons)
        {
            if (btn != null && btn.isPressed)
            {
                isAnyPressed = true;
                break;
            }
        }
        if (isBridgeActive != isAnyPressed)
        {
            isBridgeActive = isAnyPressed;

            if (bridgeCoroutine != null) StopCoroutine(bridgeCoroutine);
            bridgeCoroutine = StartCoroutine(ManageBridgeSequence(isBridgeActive));
        }
    }

    [Server]
    private IEnumerator ManageBridgeSequence(bool build)
    {
        if (build)
        {
            while (spawnedBridges.Count < maxBridgeLength)
            {
                Vector3 spawnPos = startPoint.position + new Vector3(-(spawnedBridges.Count * tileWidth), 0, 0);
                GameObject newBridge = Instantiate(bridgePrefab, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(newBridge);
                spawnedBridges.Add(newBridge);

                yield return new WaitForSeconds(buildDelay);
            }
        }
        else
        {
            while (spawnedBridges.Count > 0)
            {
                int lastIndex = spawnedBridges.Count - 1;
                GameObject bridgeToRemove = spawnedBridges[lastIndex];
                spawnedBridges.RemoveAt(lastIndex);

                if (bridgeToRemove != null)
                {
                    NetworkServer.Destroy(bridgeToRemove);
                }

                yield return new WaitForSeconds(buildDelay);
            }
        }
    }
}