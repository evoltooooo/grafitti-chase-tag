
using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStamina characterStamina;
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        if (characterStamina == null)
        {
            Debug.LogError(
                $"STAMINA UI | CharacterStamina is missing on {name}.",
                this
            );
        }

        if (fillImage == null)
        {
            Debug.LogError(
                $"STAMINA UI | Fill Image is missing on {name}.",
                this
            );
        }
    }

    private void Update()
    {
        if (characterStamina == null || fillImage == null)
            return;

        fillImage.fillAmount =
            Mathf.Clamp01(characterStamina.NormalizedStamina);
    }
}
