using UnityEngine;

public class ParryWindow : MonoBehaviour
{
    [SerializeField] float duration = 0.2f;

    public bool IsActive { get; private set; }
    float remaining;

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0.05f, value);
    }

    public void Open()
    {
        remaining = duration;
        IsActive = true;
        enabled = true;
    }

    public void Close()
    {
        remaining = 0f;
        IsActive = false;
    }

    void Update()
    {
        if (!IsActive)
            return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
            Close();
    }
}
