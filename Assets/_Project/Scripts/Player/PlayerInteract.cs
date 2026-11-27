using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float Range = 2.2f;
    PlayerInputReader input;

    void Awake() => input = GetComponent<PlayerInputReader>();

    void Update()
    {
        if (input == null || !input.InteractPressed)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, Range);
        Interactable closest = null;
        float best = Range;
        for (int i = 0; i < hits.Length; i++)
        {
            Interactable interactable = hits[i].GetComponentInParent<Interactable>();
            if (interactable == null)
                continue;
            float d = Vector3.Distance(transform.position, interactable.transform.position);
            if (d < best)
            {
                best = d;
                closest = interactable;
            }
        }

        closest?.Interact();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Collectible"))
            return;

        CurrencyWallet.Instance?.Add(2);
        ExperienceSystem.Instance?.Add(10);
        other.gameObject.SetActive(false);
    }
}