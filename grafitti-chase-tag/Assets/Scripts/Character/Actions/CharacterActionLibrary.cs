using System.Collections.Generic;
using UnityEngine;

public class CharacterActionLibrary : MonoBehaviour
{
    [Header("Actions")]
    [SerializeField]
    private List<CharacterActionData> actions = new List<CharacterActionData>();

    private Dictionary<CharacterActionType, CharacterActionData>
        actionLookup;

    private void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        actionLookup = new Dictionary<CharacterActionType, CharacterActionData>();

        foreach (CharacterActionData action in actions)
        {
            if (action == null)
                continue;

            if (actionLookup.ContainsKey(action.actionType))
            {
                Debug.LogWarning(
                    $"{name}: Duplicate ActionData for " +
                    $"{action.actionType}.",
                    this
                );

                continue;
            }

            actionLookup.Add(
                action.actionType,
                action
            );
        }
    }

    public bool TryGetActionData(
        CharacterActionType actionType,
        out CharacterActionData actionData)
    {
        if (actionLookup != null &&     
            actionLookup.TryGetValue(
                actionType,
                out actionData))
        {
            return true;
        }

        actionData = null;
        return false;
    }
}