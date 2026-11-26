using UnityEngine;

public sealed class PrototypeStairs : PrototypeInteractable
{
    private PrototypeDungeonRun run;
    private bool leadsDown;
    public override bool RequiresFacing => false;
    public override string InteractionPrompt => leadsDown ? "Space: Descend to next floor"
        : run != null && run.Floor <= 1 ? "Space: Leave the dungeon" : "Space: Ascend to previous floor";

    public void Initialize(PrototypeDungeonRun dungeonRun, bool down)
    {
        run = dungeonRun;
        leadsDown = down;
    }

    public override bool CanInteract(PrototypePlayerMovement player)
    {
        // Floor 1's up stairs (the spawn point) can be used, but only to be told you cannot leave.
        return base.CanInteract(player) && run != null && (!leadsDown || run.Floor < 5);
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        if (run == null) return;
        if (leadsDown) run.Descend();
        else if (run.Floor <= 1) PrototypeGameUI.ShowNotice("You cannot leave the dungeon!");
        else run.Ascend();
    }
}
