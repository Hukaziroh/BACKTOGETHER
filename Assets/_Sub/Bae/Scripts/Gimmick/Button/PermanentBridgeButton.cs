using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PermanentBridgeButton : NetworkBehaviour
{
    [Header("다리 설정")]
    public GameObject bridgePrefab;
    public Transform startPoint;
    public int maxBridgeLength = 5;
    public float buildDelay = 0.15f;
    public float tileWidth = 1f;

    [Header("비주얼 설정")]
    public GameObject unpressedVisual;
    public GameObject pressedVisual;

    [SyncVar(hook = nameof(OnBridgeBuiltChanged))]
    private bool isBridgeBuilt = false;

    private List<GameObject> spawnedBridges = new List<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisual(isBridgeBuilt); 
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBridgeBuilt || !other.CompareTag("Player")) return;
        isBridgeBuilt = true;
        StartCoroutine(BuildBridgeSequence());
    }

    [Server]
    private IEnumerator BuildBridgeSequence()
    {
        for (int i = 0; i < maxBridgeLength; i++)
        {
            Vector3 spawnPos = startPoint.position + new Vector3(-(i * tileWidth), 0, 0);
            GameObject newBridge = Instantiate(bridgePrefab, spawnPos, Quaternion.identity);

            NetworkServer.Spawn(newBridge);
            spawnedBridges.Add(newBridge);

            yield return new WaitForSeconds(buildDelay);
        }
    }

    private void OnBridgeBuiltChanged(bool oldState, bool newState)
    {
        UpdateVisual(newState);
    }

    private void UpdateVisual(bool built)
    {
        if (unpressedVisual != null) unpressedVisual.SetActive(!built);
        if (pressedVisual != null) pressedVisual.SetActive(built);
    }
}