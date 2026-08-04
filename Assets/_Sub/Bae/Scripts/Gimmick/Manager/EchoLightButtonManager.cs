using UnityEngine;
using Mirror;

public class EchoLightButtonManager : NetworkBehaviour
{
    [Header("연결할 버튼들 (에코 버튼 4개)")]
    public CoopButton[] connectedButtons;

    [Header("전원 누르면 같이 트리거할 로프 매니저")]
    public CoopRopeManager ropeManager;

    // 전원이 버튼을 누르면 true로 바뀌어 모든 클라이언트에 전파된다.
    // 각 클라이언트는 이 값이 true가 되는 순간 자기 화면의 불/에코 상태를 원상복구하면 된다.
    [SyncVar(hook = nameof(OnLightsRestoredChanged))]
    private bool lightsRestored = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        CoopButtonUtility.SubscribeButtons(connectedButtons, CheckAllButtons);
    }

    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        if (lightsRestored) return; // 한 번 켜지면 다시 꺼지지 않음

        if (CoopButtonUtility.AreAllPressed(connectedButtons))
        {
            lightsRestored = true;

            if (ropeManager != null)
            {
                ropeManager.StartRopeGimmick();
            }
        }
    }

    private void OnLightsRestoredChanged(bool oldState, bool newState)
    {
        if (!newState) return;

        // 이 훅은 SyncVar 값이라 모든 클라이언트에서 각자 실행된다.
        // 즉 각자 자기 화면의 EchoZoneController를 찾아서 자기 라이트를 원상복구하면
        // 결과적으로 4명 전원이 동시에 밝아지는 셈이 된다.
        EchoZoneController zoneController = FindAnyObjectByType<EchoZoneController>();
        if (zoneController != null)
        {
            zoneController.ForceLightsOn();
        }
    }
}
