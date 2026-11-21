
using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    [Header("Scene BGM")]
    [SerializeField] private AudioClip bgmClip;

    [Header("Playback Settings")]
    [SerializeField] private bool loopBGM = true;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "SCENE BGM | AudioManager not found."
            );
            return;
        }

        AudioManager.Instance.PlayBGM(
            bgmClip,
            loopBGM
        );
    }
}
