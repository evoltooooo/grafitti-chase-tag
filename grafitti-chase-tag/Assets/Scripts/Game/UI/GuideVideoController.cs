using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class GuideVideoController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RenderTexture renderTexture;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null || videoDisplay == null ||
            renderTexture == null)
        {
            Debug.LogError(
                "GUIDE VIDEO | Missing references.",
                this
            );
            enabled = false;
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        videoDisplay.texture = renderTexture;
        videoDisplay.enabled = false;

        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnEnable()
    {
        if (videoPlayer == null || videoPlayer.clip == null)
            return;

        videoDisplay.enabled = false;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (!isActiveAndEnabled)
            return;

        videoDisplay.enabled = true;
        source.Play();
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoDisplay != null)
            videoDisplay.enabled = false;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnVideoPrepared;
    }
}
