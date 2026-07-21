using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;

public class CoopButton : NetworkBehaviour
{
    [Header("버튼 비주얼 설정")]
    [Tooltip("버튼이 안 눌렸을 때 보여질 오브젝트")]
    public GameObject unpressedVisual;
    [Tooltip("버튼이 눌렸을 때 보여질 오브젝트")]
    public GameObject pressedVisual;

    [SyncVar(hook = nameof(OnButtonStateChanged))]
    public bool isPressed = false;

    private HashSet<GameObject> playersOnButton = new HashSet<GameObject>();
    public Action<CoopButton> OnButtonStateChangedEvent;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisual(isPressed);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnButton.Add(other.gameObject);
            UpdateButtonState();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnButton.Remove(other.gameObject);
            UpdateButtonState();
        }
    }

    [Server]
    private void UpdateButtonState()
    {
        playersOnButton.RemoveWhere(go => go == null || !go.activeInHierarchy);
        bool currentlyPressed = (playersOnButton.Count > 0);

        if (isPressed != currentlyPressed)
        {
            isPressed = currentlyPressed;

            OnButtonStateChangedEvent?.Invoke(this);
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