using UnityEngine;

public class AudioTest : MonoBehaviour
{
    [SerializeField] AudioClip sfxClip;
    [SerializeField] AudioClip musicClip;

    [ContextMenu("Play sfx")]
    void PlaySfx()
    {
        AudioManager.Instance.PlaySfx(sfxClip);
    }

    [ContextMenu("Play music")]
    void PlayMusic()
    {
        AudioManager.Instance.PlayMusic(musicClip);
    }
}