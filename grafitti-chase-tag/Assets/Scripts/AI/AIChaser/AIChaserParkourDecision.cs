using UnityEngine;

public class AIChaserParkourDecision
{
    public bool ShouldVault(
        AIContext context,
        ParkourTarget target)
    {
        if (!target.IsValid)
            return false;

        // Don't attempt parkour while already performing an action.
        if (context.IsExecutingAction)
            return false;

        // We currently only want Vaults while chasing/intercepting.
        // Search should not automatically trigger Vault.
        return true;
    }
}