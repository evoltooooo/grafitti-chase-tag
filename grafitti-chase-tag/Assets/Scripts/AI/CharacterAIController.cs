using UnityEngine;

public class CharacterAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterMotor characterMotor;
    [SerializeField] private CharacterActionRuntime actionRuntime;

    [Header("Target")]
    [SerializeField] private Transform target;

    private void Awake()
    {
        if (characterMotor == null)
        {
            characterMotor =
                GetComponent<CharacterMotor>();
        }

        if (actionRuntime == null)
        {
            actionRuntime =
                GetComponent<CharacterActionRuntime>();
        }
    }

    private void Update()
    {
        if (characterMotor == null)
            return;

        if (target == null)
        {
            characterMotor.Tick(
                Vector2.zero,
                false
            );

            if (actionRuntime != null)
            {
                actionRuntime.SetSteeringInput(
                    Vector2.zero
                );
            }

            return;
        }

        Vector3 toTarget =
            target.position -
            transform.position;

        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.01f)
        {
            characterMotor.Tick(
                Vector2.zero,
                false
            );

            if (actionRuntime != null)
            {
                actionRuntime.SetSteeringInput(
                    Vector2.zero
                );
            }

            return;
        }

        Vector3 direction =
            toTarget.normalized;

        Vector2 movementInput =
            new Vector2(
                Vector3.Dot(
                    transform.right,
                    direction
                ),
                Vector3.Dot(
                    transform.forward,
                    direction
                )
            );

        movementInput =
            Vector2.ClampMagnitude(
                movementInput,
                1f
            );

        if (actionRuntime != null)
        {
            actionRuntime.SetSteeringInput(
                movementInput
            );
        }

        characterMotor.Tick(
            movementInput,
            false
        );
    }
}