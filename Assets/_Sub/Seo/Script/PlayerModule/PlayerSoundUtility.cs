using UnityEngine;

public static class PlayerSoundUtility
{
    public static void PlayPositional(Vector3 position, AudioClip clip, float volume, float minDistance, float maxDistance)
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
        source.Play();

        Object.Destroy(soundObj, clip.length);
    }
}
