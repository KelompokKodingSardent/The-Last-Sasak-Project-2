using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public string Prompt = "Interact";
    public UnityEvent OnInteract;

    public void Interact()
    {
        OnInteract?.Invoke();
    }
}
