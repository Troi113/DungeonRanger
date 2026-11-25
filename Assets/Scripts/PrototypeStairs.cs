using UnityEngine;

public sealed class PrototypeStairs : PrototypeInteractable
{
    private PrototypeDungeonRun run;
    public override bool RequiresFacing => false;
    public override string InteractionPrompt => "Space: Descend to next floor";

    public void Initialize(PrototypeDungeonRun dungeonRun) => run = dungeonRun;

    public override void Interact(PrototypePlayerMovement player)
    {
        if (run != null) run.Descend();
    }
}
