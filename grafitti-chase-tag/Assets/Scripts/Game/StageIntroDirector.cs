using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class StageIntroDirector : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Round Manager")]
    [SerializeField] private RoundManager roundManager;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayHUDRoot;

    [Header("Skip UI")]
    [SerializeField] private TMP_Text skipHintText;

    [Header("Cutscene Objects")]
    [SerializeField] private GameObject fadeOverlayRoot;
    [SerializeField] private CanvasGroup fadeOverlayGroup;

    private bool isPlaying;
    private bool introCompleted;
    private bool skipHintRevealed;

    private void Awake()
    {
        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();

        if (playableDirector == null)
        {
            Debug.LogError(
                "STAGE INTRO | PlayableDirector reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (roundManager == null)
        {
            Debug.LogError(
                "STAGE INTRO | RoundManager reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        // Timeline playback is controlled by this script.
        playableDirector.playOnAwake = false;

        // Listen for natural Timeline completion.
        playableDirector.stopped += OnTimelineStopped;

        if (skipHintText != null)
            skipHintText.gameObject.SetActive(false);
        
        // Hide gameplay HUD while the intro is playing.
        if (gameplayHUDRoot != null)
            gameplayHUDRoot.SetActive(false);
    }

    private void Start()
    {
        if (playableDirector.playableAsset == null)
        {
            Debug.LogError(
                "STAGE INTRO | No Timeline asset is assigned.",
                this
            );

            return;
        }

        StartIntro();
    }

    private void Update()
    {
        if (!isPlaying || introCompleted)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!skipHintRevealed)
            {
                skipHintRevealed = true;

                if (skipHintText != null)
                    skipHintText.gameObject.SetActive(true);

                Debug.Log("STAGE INTRO | Skip instruction revealed.");

                return;
            }

            SkipIntro();
        }
    }

    private void StartIntro()
    {
        isPlaying = true;
        skipHintRevealed = false;

        if (skipHintText != null)
            skipHintText.gameObject.SetActive(false);

        playableDirector.Play();

        Debug.Log("STAGE INTRO | Timeline started.");
    }

    private void SkipIntro()
    {
        Debug.Log("STAGE INTRO | Intro skipped by player.");

        CompleteIntro();
    }

    private void OnTimelineStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != playableDirector)
            return;

        if (introCompleted)
            return;

        Debug.Log("STAGE INTRO | Timeline finished naturally.");

        CompleteIntro();
    }
    
    private void CompleteIntro()
    {
        if (introCompleted)
            return;

        // Prevent duplicate completion.
        introCompleted = true;
        isPlaying = false;

        // 1. Stop and disable the entire Timeline.
        if (playableDirector != null)
        {
            playableDirector.playOnAwake = false;
            playableDirector.Stop();
            playableDirector.enabled = false;
        }

        // 2. Stop and disable the FadeOverlay Animator.
        if (fadeOverlayRoot != null)
        {
            Animator fadeAnimator =
                fadeOverlayRoot.GetComponent<Animator>();

            if (fadeAnimator != null)
            {
                fadeAnimator.enabled = false;
            }

            fadeOverlayRoot.SetActive(false);
        }

        // 3. Reset CanvasGroup.
        if (fadeOverlayGroup != null)
        {
            fadeOverlayGroup.alpha = 0f;
            fadeOverlayGroup.interactable = false;
            fadeOverlayGroup.blocksRaycasts = false;
        }

        // 4. Hide skip instruction.
        if (skipHintText != null)
        {
            skipHintText.gameObject.SetActive(false);
        }

        // 5. Continue gameplay initialization.
        // HUD restoration is handled by RoleShuffleController.

        roundManager.StartGameAfterIntro();

        Debug.Log(
            "STAGE INTRO | Entire cutscene terminated. " +
            "Fade Animator disabled. Starting gameplay preparation."
        );
    }

    private void OnDestroy()
    {
        if (playableDirector != null)
            playableDirector.stopped -= OnTimelineStopped;
    }
}
