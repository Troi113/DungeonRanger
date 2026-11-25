using UnityEngine;

/// <summary>
/// Base class for simple Space-bar interactions in the live prototype.
/// </summary>
public abstract class PrototypeInteractable : MonoBehaviour
{
    public virtual bool RequiresFacing => true;

    public abstract string InteractionPrompt { get; }

    public virtual bool CanInteract(PrototypePlayerMovement player)
    {
        return player != null;
    }

    public abstract void Interact(PrototypePlayerMovement player);
}
