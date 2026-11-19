using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Health Target;
    public Slider Slider;
    public Text Label;

    public void Bind(Health health)
    {
        if (Target != null)
            Target.OnHealthChanged -= Handle;

        Target = health;
        if (Target != null)
        {
            Target.OnHealthChanged += Handle;
            Handle(Target.CurrentHealth, Target.MaxHealth);
        }
    }

    void OnDisable()
    {
        if (Target != null)
            Target.OnHealthChanged -= Handle;
    }

    void Handle(float current, float max)
    {
        if (Slider != null)
        {
            Slider.maxValue = max;
            Slider.value = current;
        }

        if (Label != null)
            Label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
