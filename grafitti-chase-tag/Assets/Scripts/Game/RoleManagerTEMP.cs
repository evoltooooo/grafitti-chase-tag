using UnityEngine;

public class RoleManagerTEMP : MonoBehaviour
{
    public enum StartingRole
    {
        PlayerChaser,
        AIChaser,
        Random
    }

    [Header("Characters")]
    [SerializeField] private CharacterRole playerRole;
    [SerializeField] private CharacterRole aiRole;

    [Header("Starting Role")]
    [SerializeField] private StartingRole startingRole = StartingRole.PlayerChaser;

    private void Awake()
    {
        AssignInitialRoles();
    }

    public void AssignInitialRoles()
    {
        CharacterRoleType playerAssignedRole;

        switch (startingRole)
        {
            case StartingRole.PlayerChaser:
                playerAssignedRole = CharacterRoleType.Chaser;
                break;

            case StartingRole.AIChaser:
                playerAssignedRole = CharacterRoleType.Evader;
                break;

            case StartingRole.Random:
                playerAssignedRole =
                    Random.value < 0.5f
                        ? CharacterRoleType.Chaser
                        : CharacterRoleType.Evader;
                break;

            default:
                playerAssignedRole = CharacterRoleType.None;
                break;
        }

        CharacterRoleType aiAssignedRole =
            playerAssignedRole == CharacterRoleType.Chaser
                ? CharacterRoleType.Evader
                : CharacterRoleType.Chaser;

        playerRole.SetRole(playerAssignedRole);
        aiRole.SetRole(aiAssignedRole);
    }

    public void SetRoles(
        CharacterRoleType playerAssignedRole,
        CharacterRoleType aiAssignedRole)
    {
        playerRole.SetRole(playerAssignedRole);
        aiRole.SetRole(aiAssignedRole);
    }
}
