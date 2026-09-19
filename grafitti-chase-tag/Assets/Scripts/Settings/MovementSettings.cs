using UnityEngine;

[CreateAssetMenu(
    fileName = "MovementSettings",
    menuName = "Chase Tag/Movement/Movement Settings"
)]
public class MovementSettings : ScriptableObject
{
    [Header("Movement Speed")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float sprintSpeed = 7f;
    
    [Min(0f)]
    public float sprintSpeedIncrease = 2f;

    [Header("Movement Response")]
    public float acceleration = 20f;
    public float deceleration = 25f;

    [Header("Rotation")]
    public float rotationSpeed = 15f;

    [Header("Jump")]
    public float jumpHeight = 2.5f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedVerticalVelocity = -2f;

    [Header("Air Movement")]
    [Range(0f, 1f)]
    public float airControl = 0.5f;

    [Header("Stamina Costs")]
    [Min(0f)]
    public float sprintStaminaPerSecond = 5f;

    [Min(0f)]
    public float jumpStaminaCost = 25f;

    [Header("Wall Rebound")]
    [Min(0f)]
    public float wallReboundMinSpeed = 6f;

    [Min(0f)]
    public float wallReboundSpeed = 7f;

    [Min(0f)]
    public float wallReboundCooldown = 0.2f;

    [Range(0f, 90f)]
    public float wallReboundMaxApproachAngle = 60f;
}