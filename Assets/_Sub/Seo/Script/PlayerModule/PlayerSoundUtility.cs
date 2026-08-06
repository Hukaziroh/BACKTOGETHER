using UnityEngine;
using UnityEngine.Audio;

public static class PlayerSoundUtility
{
    // OptionsManager가 시작 시점에 자신의 AudioMixer에서 Master 그룹을 찾아 여기에 등록해둔다.
    // 그러면 각 프리팹마다 따로 믹서 그룹을 안 넣어줘도 전부 설정 메뉴 볼륨의 영향을 받는다.
    public static AudioMixerGroup DefaultSfxGroup;

    public static void PlayPositional(Vector3 position, AudioClip clip, float volume, float minDistance, float maxDistance, AudioMixerGroup mixerGroup = null)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject("PlayerSound_Temp");
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f; // 3D: 거리에 따라 감쇠되도록
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.outputAudioMixerGroup = mixerGroup != null ? mixerGroup : DefaultSfxGroup;
        source.Play();

        Object.Destroy(soundObj, clip.length);
    }
}
