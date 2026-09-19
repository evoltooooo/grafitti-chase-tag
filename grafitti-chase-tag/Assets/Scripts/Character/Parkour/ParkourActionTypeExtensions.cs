public static class ParkourActionTypeExtensions
{
    public static CharacterActionType ToActionType(
        this ParkourActionType parkourActionType)
    {
        switch (parkourActionType)
        {
            case ParkourActionType.Vault:
                return CharacterActionType.Vault;

            case ParkourActionType.Climb:
                return CharacterActionType.Climb;

            case ParkourActionType.TicTac:
                return CharacterActionType.TicTac;

            case ParkourActionType.PoleSpin:
                return CharacterActionType.PoleSpin;

            case ParkourActionType.WallRebound:
                return CharacterActionType.WallRebound;

            case ParkourActionType.Slide:
                return CharacterActionType.Slide;

            default:
                return CharacterActionType.None;
        }
    }
}