using UnityEngine;
using UnityEngine.UI;

public class BuffNerfEffectIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image durationFill;
    [SerializeField] private Image iconImage;

    public void Initialize(BuffNerfTokenData tokenData)
    {
        if (tokenData == null)
            return;

        if (iconImage != null)
            iconImage.sprite = tokenData.Icon;

        SetRemainingDuration(1f);
    }

    public void SetRemainingDuration(float normalizedDuration)
    {
        if (durationFill == null)
            return;

        durationFill.fillAmount =
            Mathf.Clamp01(normalizedDuration);
    }
}
