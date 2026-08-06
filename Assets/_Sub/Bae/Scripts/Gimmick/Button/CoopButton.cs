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

    [Header("버튼 사운드")]
    // 클립 자체는 PlayerSoundLibrary 애셋 한 곳에서만 관리 (프리팹마다 따로 안 넣음)
    public PlayerSoundLibrary soundLibrary;
    [Range(0f, 1f)] public float buttonVolume = 0.6f;
    public float soundMinDistance = 9f;
    public float soundMaxDistance = 40f;

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
        PlayButtonSound(newState);
    }

    private void UpdateVisual(bool pressed)
    {
        if (unpressedVisual != null) unpressedVisual.SetActive(!pressed);
        if (pressedVisual != null) pressedVisual.SetActive(pressed);
    }

    private void PlayButtonSound(bool pressed)
    {
        AudioClip clip = null;
        if (soundLibrary != null) clip = pressed ? soundLibrary.buttonPressSound : soundLibrary.buttonReleaseSound;
        PlayerSoundUtility.PlayPositional(transform.position, clip, buttonVolume, soundMinDistance, soundMaxDistance);
    }
}