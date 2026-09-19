public struct CharacterActionRequest
{
    public CharacterActionType ActionType;

    public CharacterActionRequest(
        CharacterActionType actionType)
    {
        ActionType = actionType;
    }
}