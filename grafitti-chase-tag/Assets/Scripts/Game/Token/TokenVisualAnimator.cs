
using UnityEngine;

public class TokenVisualAnimator : MonoBehaviour
{
    [Header("Coin Reference")]
    [SerializeField] private Transform coinSpinPivot;

    [Header("Coin Rotation")]
    [SerializeField, Min(0f)]
    private float rotationSpeed = 180f;

    [Header("Floating Animation")]
    [SerializeField, Min(0f)]
    private float floatAmplitude = 0.15f;

    [SerializeField, Min(0f)]
    private float floatFrequency = 2f;

    private Vector3 initialLocalPosition;
    private Quaternion initialCoinRotation;
    private float currentRotation;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;

        if (coinSpinPivot != null)
        {
            initialCoinRotation = coinSpinPivot.localRotation;
        }
        else
        {
            Debug.LogWarning(
                $"{name}: Coin Spin Pivot is not assigned.",
                this
            );
        }
    }

    private void Update()
    {
        AnimateFloating();
        AnimateCoinRotation();
    }

    private void AnimateFloating()
    {
        float offset = Mathf.Sin(
            Time.time * floatFrequency
        ) * floatAmplitude;

        transform.localPosition =
            initialLocalPosition + Vector3.up * offset;
    }

    private void AnimateCoinRotation()
    {
        if (coinSpinPivot == null)
            return;

        currentRotation += rotationSpeed * Time.deltaTime;

        coinSpinPivot.localRotation =
            initialCoinRotation *
            Quaternion.Euler(0f, currentRotation, 0f);
    }
}
