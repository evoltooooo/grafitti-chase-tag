using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class RoleShuffleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Image chaserTitle;
    [SerializeField] private Image evaderTitle;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject gameplayUIRoot;

    [Header("Camera Lock")]
    [SerializeField] private CinemachineInputAxisController cameraInput;

    [Header("Timing")]
    [SerializeField] private float totalDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float revealHoldDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float shuffleInterval = 0.12f;

    private Coroutine shuffleRoutine;

    private void Awake()
    {
        if (overlayGroup == null)
            overlayGroup = GetComponent<CanvasGroup>();

        SetVisible(false);
    }

    public void Play(
        CharacterRoleType playerRole,
        Action onComplete)
    {
        if (shuffleRoutine != null)
            StopCoroutine(shuffleRoutine);

        shuffleRoutine = StartCoroutine(
            PlayRoutine(playerRole, onComplete));
    }

    private IEnumerator PlayRoutine(
        CharacterRoleType playerRole,
        Action onComplete)
    {
        GameplayInputLock.SetLocked(true);

        if (cameraInput != null)
            cameraInput.enabled = false;

        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        SetVisible(true);
        
        if (infoText != null)
            infoText.text = "This round you are...";

        SetTitle(null);

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;

            overlayGroup.alpha = Mathf.Clamp01(
                elapsed / fadeInDuration);

            yield return null;
        }

        overlayGroup.alpha = 1f;

        float shuffleDuration = Mathf.Max(
            0f,
            totalDuration -
            fadeInDuration -
            revealHoldDuration -
            fadeOutDuration);

        float shuffleElapsed = 0f;
        float switchTimer = 0f;
        bool showChaser = true;

        while (shuffleElapsed < shuffleDuration)
        {
            shuffleElapsed += Time.deltaTime;
            switchTimer += Time.deltaTime;

            if (switchTimer >= shuffleInterval)
            {
                switchTimer = 0f;
                showChaser = !showChaser;

                SetTitle(
                    showChaser
                        ? CharacterRoleType.Chaser
                        : CharacterRoleType.Evader);
            }

            yield return null;
        }

        SetTitle(playerRole);

        Transform titleTransform =
            playerRole == CharacterRoleType.Chaser
                ? chaserTitle.transform
                : evaderTitle.transform;

        // Save the title's actual size before animating it.
        Vector3 originalScale = titleTransform.localScale;

        // Start slightly smaller than its normal size.
        titleTransform.localScale = originalScale * 0.75f;

        float popElapsed = 0f;
        const float popDuration = 0.1f;

        while (popElapsed < popDuration)
        {
            popElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(popElapsed / popDuration);

            // Ease-out animation: fast at first, then slows down.
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            titleTransform.localScale = Vector3.Lerp(
                originalScale * 0.75f,
                originalScale,
                easedT
            );

            yield return null;
        }

        // Restore the exact original size.
        titleTransform.localScale = originalScale;

        yield return new WaitForSeconds(
            revealHoldDuration);

        float fadeElapsed = 0f;
        float fadeStartAlpha = overlayGroup.alpha;

        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;

            overlayGroup.alpha = Mathf.Lerp(
                fadeStartAlpha,
                0f,
                Mathf.Clamp01(
                    fadeElapsed / fadeOutDuration));

            yield return null;
        }

        SetVisible(false);

        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(true);

        if (cameraInput != null)
            cameraInput.enabled = true;

        GameplayInputLock.SetLocked(false);

        shuffleRoutine = null;
        onComplete?.Invoke();
    }

    private void SetTitle(CharacterRoleType? role)
    {
        if (chaserTitle != null)
        {
            chaserTitle.gameObject.SetActive(
                role == CharacterRoleType.Chaser);
        }

        if (evaderTitle != null)
        {
            evaderTitle.gameObject.SetActive(
                role == CharacterRoleType.Evader);
        }
    }

    private void SetVisible(bool visible)
    {
        if (overlayGroup == null)
            return;

        overlayGroup.alpha = visible ? 1f : 0f;
        overlayGroup.interactable = visible;
        overlayGroup.blocksRaycasts = visible;
    }
}
