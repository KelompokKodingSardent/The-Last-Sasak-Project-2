using UnityEngine;

public class GroundTelegraphView : MonoBehaviour
{
    [Header("Referensi Child di Scene/Prefab (Wajib Di-assign)")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform outerBoundTransform;
    [SerializeField] private Transform fillProgressTransform;
    [SerializeField] private Renderer fillRenderer;

    [Header("Warna Keterbacaan Isometrik")]
    [SerializeField] private Color parryableColor = new Color(1f, 0.25f, 0.15f, 0.35f);
    [SerializeField] private Color heavyColor = new Color(1f, 0.65f, 0.05f, 0.45f);
    [SerializeField] private Color supportColor = new Color(0.25f, 0.85f, 1f, 0.35f);
    [SerializeField] private Color flashReadyColor = new Color(1f, 1f, 1f, 0.75f);

    private MaterialPropertyBlock _propBlock;
    private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorPropId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        Hide();
    }

    public void Show(AttackSwingStep step, bool canBeParried)
    {
        Show(step, canBeParried, null);
    }

    public void Show(AttackSwingStep step, bool canBeParried, Vector3? worldOverridePos)
    {
        if (visualRoot == null) return;

        visualRoot.SetActive(true);

        if (step.telegraphShape == TelegraphShape.TargetedZone && worldOverridePos.HasValue)
        {
            Vector3 pos = worldOverridePos.Value;
            visualRoot.transform.position = new Vector3(pos.x, pos.y + 0.03f, pos.z);
            visualRoot.transform.rotation = Quaternion.identity;
        }
        else
        {
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
        }

        ApplyShapeScale(step);

        if (outerBoundTransform != null && fillProgressTransform != null)
            fillProgressTransform.localPosition = outerBoundTransform.localPosition + new Vector3(0f, 0.005f, 0f);

        SetProgress(0f);

        Color baseCol = step.deliveryType == AttackDeliveryType.SupportEmpower
            ? supportColor
            : (canBeParried ? parryableColor : heavyColor);

        ApplyColor(baseCol);
    }

    public void UpdateWorldPosition(Vector3 worldPos)
    {
        if (visualRoot == null || !visualRoot.activeSelf) return;
        visualRoot.transform.position = new Vector3(worldPos.x, worldPos.y + 0.03f, worldPos.z);
    }

    public void SetProgress(float normalizedProgress)
    {
        if (fillProgressTransform == null) return;

        float t = Mathf.Clamp01(normalizedProgress);
        Vector3 targetScale = outerBoundTransform != null ? outerBoundTransform.localScale : Vector3.one;

        fillProgressTransform.localScale = new Vector3(
            targetScale.x * Mathf.Max(0.05f, t),
            targetScale.y,
            targetScale.z * Mathf.Max(0.05f, t)
        );
    }

    public void TriggerStartupFlash()
    {
        ApplyColor(flashReadyColor);
    }

    public void Hide()
    {
        if (visualRoot != null)
        {
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.SetActive(false);
        }
    }

    private void ApplyShapeScale(AttackSwingStep step)
    {
        if (outerBoundTransform == null) return;

        switch (step.telegraphShape)
        {
            case TelegraphShape.TargetedZone:
            {
                float radius = step.deliveryType == AttackDeliveryType.SupportEmpower
                    ? step.empowerRadius
                    : step.zoneRadius;
                float diameter = radius * 2f;
                outerBoundTransform.localScale = new Vector3(diameter, 1f, diameter);
                outerBoundTransform.localPosition = Vector3.zero;
                break;
            }

            case TelegraphShape.Circle:
            {
                float diameter = step.range * 2f;
                outerBoundTransform.localScale = new Vector3(diameter, 1f, diameter);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, 0f);
                break;
            }

            case TelegraphShape.Line:
            {
                outerBoundTransform.localScale = new Vector3(step.width, 1f, step.range);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, step.range * 0.5f);
                break;
            }

            case TelegraphShape.Cone:
            default:
            {
                float arcWidth = 2f * step.range * Mathf.Sin(step.angle * 0.5f * Mathf.Deg2Rad);
                outerBoundTransform.localScale = new Vector3(Mathf.Max(0.8f, arcWidth), 1f, step.range);
                outerBoundTransform.localPosition = new Vector3(0f, 0.03f, step.range * 0.5f);
                break;
            }
        }
    }

    private void ApplyColor(Color color)
    {
        if (fillRenderer == null) return;

        fillRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(ColorPropId, color);
        _propBlock.SetColor(LegacyColorPropId, color);
        fillRenderer.SetPropertyBlock(_propBlock);
    }
}