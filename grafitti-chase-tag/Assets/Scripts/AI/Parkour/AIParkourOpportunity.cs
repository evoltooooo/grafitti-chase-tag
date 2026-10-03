using UnityEngine;

public class AIParkourOpportunity
{
    public bool TryFindVault(
        CharacterActionController actionController,
        out ParkourTarget target)
    {
        return actionController.TryFindParkourTarget(
            ParkourActionType.Vault,
            out target
        );
    }

    public bool TryFindClimb(
        CharacterActionController actionController,
        out ParkourTarget target)
    {
        return actionController.TryFindParkourTarget(
            ParkourActionType.Climb,
            out target
        );
    }
}