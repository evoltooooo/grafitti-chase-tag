using System;
using UnityEngine;

public class CharacterRole : MonoBehaviour
{
    public CharacterRoleType CurrentRole { get; private set; }

    public bool IsChaser => CurrentRole == CharacterRoleType.Chaser;
    public bool IsEvader => CurrentRole == CharacterRoleType.Evader;

    public event Action<CharacterRoleType> RoleChanged;

    public void SetRole(CharacterRoleType role)
    {
        if (CurrentRole == role)
            return;

        CurrentRole = role;
        RoleChanged?.Invoke(CurrentRole);
    }
}