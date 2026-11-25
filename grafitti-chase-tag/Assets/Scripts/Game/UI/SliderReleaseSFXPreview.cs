using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderReleaseSFXPreview : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    ISubmitHandler
{
    private Slider slider;
    private float lastPreviewedValue;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        lastPreviewedValue = slider.value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // The slider is about to be adjusted.
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayPreviewIfChanged();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // Supports Submit through the UI Input Actions
        // when adjusting the slider with keyboard/gamepad.
        PlayPreviewIfChanged();
    }

    private void PlayPreviewIfChanged()
    {
        if (slider == null)
            return;

        if (Mathf.Approximately(
            slider.value,
            lastPreviewedValue))
            return;

        lastPreviewedValue = slider.value;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySettingsSFXPreview();
    }
}
