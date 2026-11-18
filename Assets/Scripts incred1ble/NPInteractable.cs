using UnityEngine;

public abstract class NPInteractable : MonoBehaviour
{
    public abstract string Prompt { get; }
    public virtual bool IsAvailable => true;
    public abstract void Interact();
}
