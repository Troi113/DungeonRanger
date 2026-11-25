using UnityEngine;

/// <summary>The boss reward finishes the run even when the backpack is full.</summary>
public sealed class PrototypeVictoryAmulet : PrototypeInteractable
{
    private PrototypeDungeonRun run;

    public override bool RequiresFacing => false;
    public override string InteractionPrompt => "Space: Pick up the Amulet of Glory";

    public override bool CanInteract(PrototypePlayerMovement player)
    {
        return base.CanInteract(player) && run != null && !run.Victory;
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        if (!CanInteract(player)) return;
        gameObject.SetActive(false);
        run.CollectAmulet();
    }

    public static void Create(Transform floor, Vector3 position, PrototypeDungeonRun dungeonRun)
    {
        var amulet = new GameObject("Amulet_of_Glory");
        amulet.transform.SetParent(floor, false);
        amulet.transform.position = new Vector3(position.x, 0.1f, position.z);
        var pickup = amulet.AddComponent<PrototypeVictoryAmulet>();
        pickup.run = dungeonRun;

        var gold = PrototypeBootstrap.CreateMaterial("Amulet Gold", new Color(1f, 0.74f, 0.16f));
        var gem = PrototypeBootstrap.CreateMaterial("Amulet Gem", new Color(0.2f, 0.85f, 0.95f));
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frame.name = "Gold_Frame";
        frame.transform.SetParent(amulet.transform, false);
        frame.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        frame.transform.localScale = new Vector3(1.1f, 0.15f, 1.1f);
        frame.GetComponent<Renderer>().sharedMaterial = gold;
        frame.GetComponent<Collider>().enabled = false;

        var jewel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jewel.name = "Blue_Gem";
        jewel.transform.SetParent(amulet.transform, false);
        jewel.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        jewel.transform.localScale = new Vector3(0.56f, 0.45f, 0.56f);
        jewel.GetComponent<Renderer>().sharedMaterial = gem;
        jewel.GetComponent<Collider>().enabled = false;
    }
}
