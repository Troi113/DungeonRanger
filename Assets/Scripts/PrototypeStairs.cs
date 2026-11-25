using UnityEngine;

public sealed class PrototypeStairs : PrototypeInteractable
{
    private PrototypeDungeonRun run;
    private bool leadsDown;
    public override bool RequiresFacing => false;
    public override string InteractionPrompt => leadsDown ? "Space: Descend to next floor" : "Space: Ascend to previous floor";

    public void Initialize(PrototypeDungeonRun dungeonRun, bool down)
    {
        run = dungeonRun;
        leadsDown = down;
    }

    public override bool CanInteract(PrototypePlayerMovement player)
    {
        return base.CanInteract(player) && run != null && (leadsDown ? run.Floor < 5 : run.Floor > 1);
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        if (run == null) return;
        if (leadsDown) run.Descend();
        else run.Ascend();
    }
}
