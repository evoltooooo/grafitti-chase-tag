using UnityEngine;

public class CharacterInteractionResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private float interactionBufferTime = 0.20f;

    private float interactionBufferTimer;

    private void Awake()
    {
        if (actionController == null)
            actionController = GetComponent<CharacterActionController>();

        if (characterMotor == null)
            characterMotor = GetComponent<CharacterMotor>();
    }

    private void Update()
    {
        if (interactionBufferTimer <= 0f)
            return;

        ResolveBufferedInteraction();

        interactionBufferTimer -= Time.deltaTime;
    }

    public void TryResolveInteraction()
    {
        interactionBufferTimer = interactionBufferTime;

        ResolveBufferedInteraction();
    }

    private void ResolveBufferedInteraction()
    {
        if (!ValidateReferences())
            return;
        

        // ---------------------------------------------
        // VAULT & POLE SPIN & SLIDE
        // ---------------------------------------------

        if (characterMotor.IsSprinting &&
            characterMotor.IsGrounded)
        {
            // Pole Spin has priority over Vault and Slide.
            if (TryResolveParkourAction(ParkourActionType.PoleSpin))
                return;

            // Vault has priority over Slide.
            if (TryResolveParkourAction(ParkourActionType.Vault))
                return;

            // No valid vault target, so try Slide.
            TryResolveSlide();
            return;
        }

        // ---------------------------------------------
        // CLIMB
        // ---------------------------------------------

        if (characterMotor.CanAttemptClimb)
        {
            if (TryResolveParkourAction(
                    ParkourActionType.Climb))
            {
                interactionBufferTimer = 0f;
                return;
            }
        }
    }

    private void TryResolveSlide()
    {
        if (actionController == null)
            return;

        if (characterMotor == null)
            return;

        if (!characterMotor.IsGrounded)
            return;

        if (!characterMotor.IsSprinting)
            return;

        CharacterActionRequest request =
            new CharacterActionRequest(
                CharacterActionType.Slide
            );

        if (actionController.RequestAction(request))
        {
            interactionBufferTimer = 0f;
        }
    }

    private bool TryResolveParkourAction(
        ParkourActionType parkourActionType)
    {
        bool resolved =
            actionController.RequestParkourAction(
                parkourActionType);

        if (resolved)
            interactionBufferTimer = 0f;

        return resolved;
    }

    private bool ValidateReferences()
    {
        if (actionController == null)
        {
            return false;
        }

        if (characterMotor == null)
        {
            return false;
        }

        return true;
    }
}