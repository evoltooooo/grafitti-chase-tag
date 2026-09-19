using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(
    fileName = "ParkourInputSettings",
    menuName = "Chase Tag/Character/Parkour Input Settings"
)]
public class ParkourInputSettings : ScriptableObject
{
    [Header("Parkour Interaction")]
    public InputActionReference interactionInput;

    [Header("Jump")]
    public InputActionReference jumpInput;

    [Header("Tag")]
    public InputActionReference tagInput;
}