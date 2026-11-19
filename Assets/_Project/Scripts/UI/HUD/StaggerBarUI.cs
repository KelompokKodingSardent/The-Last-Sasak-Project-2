using UnityEngine;
using UnityEngine.UI;

public class StaggerBarUI : MonoBehaviour
{
    public StaggerComponent Target;
    Slider slider;

    public void Bind(StaggerComponent stagger, Slider ui)
    {
        Target = stagger;
        slider = ui;
    }

    void Update()
    {
        if (slider == null || Target == null)
            return;
        slider.maxValue = Target.MaxStagger;
        slider.value = Target.Current;
    }
}
