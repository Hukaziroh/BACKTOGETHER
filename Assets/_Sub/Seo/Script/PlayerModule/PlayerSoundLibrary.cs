using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSoundLibrary", menuName = "Sound/Player Sound Library")]
public class PlayerSoundLibrary : ScriptableObject
{
    [Header("점프")]
    public AudioClip jumpClip;

    [Header("피격")]
    public AudioClip hitClip;

    [Header("발소리")]
    public AudioClip footstepClip;

    [Header("버튼")]
    public AudioClip buttonPressSound;
    public AudioClip buttonReleaseSound;

    [Header("체크포인트 팡파레")]
    public AudioClip checkpointFanfare;
}
