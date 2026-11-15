using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const string MusicKey = "volume.music";
    const string SfxKey = "volume.sfx";

    [Header("Volumes (0 to 1)")]
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

    AudioSource music;
    AudioSource sfx;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    void Awake()
    {
        Instance = this;

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        musicVolume = PlayerPrefs.GetFloat(MusicKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, sfxVolume);
        Apply();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfx.PlayOneShot(clip, volumeScale);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || music.clip == clip) return;   
        music.clip = clip;
        music.Play();
    }

    public void StopMusic()
    {
        music.Stop();
        music.clip = null;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        Apply();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        Apply();
    }

    void Apply()
    {
        music.volume = musicVolume;
        sfx.volume = sfxVolume;
    }

    void OnValidate()
    {
        if (music != null) Apply();
    }
}