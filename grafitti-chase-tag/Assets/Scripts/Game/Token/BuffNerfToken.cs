
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BuffNerfToken : MonoBehaviour
{
    [Header("Token Configuration")]
    [SerializeField] private BuffNerfTokenData tokenData;

    private bool hasBeenCollected;

    public BuffNerfTokenData TokenData => tokenData;

    private void Awake()
    {
        // Configure the token collider as a trigger.
        SphereCollider trigger =
            GetComponent<SphereCollider>();

        trigger.isTrigger = true;

        // A kinematic Rigidbody allows reliable trigger detection
        // without changing the character's movement physics.
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Allows the future spawner to assign the selected token data.
    public void Initialize(BuffNerfTokenData data)
    {
        tokenData = data;

        if (tokenData == null)
        {
            Debug.LogError(
                $"{name}: Cannot initialize token with null data.",
                this
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenCollected)
            return;

        // Find the effect component on the character or its parent.
        CharacterBuffNerf characterEffects =
            other.GetComponentInParent<CharacterBuffNerf>();

        // Ignore objects that are not Player or AI characters.
        if (characterEffects == null)
            return;

        if (tokenData == null)
        {
            Debug.LogError(
                $"{name}: Token has no BuffNerfTokenData assigned.",
                this
            );

            return;
        }

        hasBeenCollected = true;

        // Attempt to apply the effect.
        // A role-restricted token may return false, but the token
        // must still disappear after the character touches it.
        bool effectApplied =
            characterEffects.TryApplyToken(tokenData);

        Debug.Log(
            $"TOKEN COLLECTED | Character={other.transform.root.name} | " +
            $"Token={tokenData.DisplayName} | " +
            $"EffectApplied={effectApplied}",
            this
        );

        // Spawn optional pickup VFX.
        if (tokenData.PickupVfxPrefab != null)
        {
            Instantiate(
                tokenData.PickupVfxPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // Remove the token from the arena.
        Destroy(gameObject);
    }

    private void Reset()
    {
        SphereCollider trigger =
            GetComponent<SphereCollider>();

        trigger.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
