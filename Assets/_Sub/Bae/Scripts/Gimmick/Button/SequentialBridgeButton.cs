using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SequentialBridgeButton : NetworkBehaviour
{
    [Header("다리 설정")]
    public GameObject bridgePrefab;
    public Transform startPoint;
    public int maxBridgeLength = 5;
    public float buildDelay = 0.15f;
    public float tileWidth = 1f;

    [Header("버튼 비주얼 설정")]
    public GameObject unpressedVisual;
    public GameObject pressedVisual;

    private HashSet<GameObject> playersOnButton = new HashSet<GameObject>();
    private Coroutine bridgeCoroutine;
    private List<GameObject> spawnedBridges = new List<GameObject>();

    [SyncVar(hook = nameof(OnButtonStateChanged))]
    private bool isPressed = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisual(isPressed);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnButton.Add(other.gameObject);
            Debug.Log($"[버튼 밟음] 현재 버튼 위 플레이어 수: {playersOnButton.Count}");
            UpdateBridgeState();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnButton.Remove(other.gameObject);
            Debug.Log($"[버튼 뗌] 현재 버튼 위 플레이어 수: {playersOnButton.Count}");
            UpdateBridgeState();
        }
    }

    [Server]
    private void UpdateBridgeState()
    {
        playersOnButton.RemoveWhere(go => go == null || !go.activeInHierarchy);

        bool currentlyPressed = (playersOnButton.Count > 0);

        if (isPressed != currentlyPressed)
        {
            isPressed = currentlyPressed;
            Debug.Log($"[상태 변경] 버튼 눌림 상태: {isPressed}");

            if (bridgeCoroutine != null)
            {
                StopCoroutine(bridgeCoroutine);
            }

            bridgeCoroutine = StartCoroutine(ManageBridgeSequence(isPressed));
        }
    }

    [Server]
    private IEnumerator ManageBridgeSequence(bool build)
    {
        if (build)
        {
            Debug.Log("[코루틴] 다리 생성 시작");
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
            Debug.Log("[코루틴] 다리 삭제 시작");
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
            Debug.Log("[코루틴] 다리 삭제 완료");
        }
    }

    private void OnButtonStateChanged(bool oldState, bool newState)
    {
        UpdateVisual(newState);
    }

    private void UpdateVisual(bool pressed)
    {
        if (unpressedVisual != null) unpressedVisual.SetActive(!pressed);
        if (pressedVisual != null) pressedVisual.SetActive(pressed);
    }
}