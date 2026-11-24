using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [Header("Click Sound")]
    [SerializeField] private bool useBackSFX;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(PlayClickSFX);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSFX);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonHover();
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance == null)
            return;

        if (useBackSFX)
            AudioManager.Instance.PlayButtonBack();
        else
            AudioManager.Instance.PlayButtonClick();
    }
}
