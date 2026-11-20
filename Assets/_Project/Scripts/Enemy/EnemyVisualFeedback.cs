using System.Collections;
using UnityEngine;

public class EnemyVisualFeedback : MonoBehaviour
{
    [Header("Referensi Renderer (Assign di Inspector)")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Warna State Combat")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphTint = new Color(1f, 0.72f, 0.65f, 1f);
    [SerializeField] private Color startupTint = new Color(1f, 0.35f, 0.25f, 1f);
    [SerializeField] private Color staggeredColorA = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color staggeredColorB = new Color(1f, 0.55f, 0.05f, 1f);
    [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.08f;

    private EnemyBrain _brain;
    private Health _health;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _flashRoutine;
    private bool _isFlashing;
    private float _lastKnownHealth;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _brain = GetComponent<EnemyBrain>();
        _health = GetComponent<Health>();
        _propBlock = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Renderer selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
                targetRenderers = new[] { selfRenderer };
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _lastKnownHealth = _health.CurrentHealth;
            _health.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (current < _lastKnownHealth)
            PlayHitFlash();

        _lastKnownHealth = current;
    }

    private void Update()
    {
        if (_isFlashing || _brain == null)
            return;

        ApplyStateColor(_brain.CurrentState);
    }

    public void PlayHitFlash()
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        _isFlashing = true;
        SetColor(hitFlashColor);

        yield return new WaitForSeconds(hitFlashDuration);

        _isFlashing = false;
        _flashRoutine = null;
    }

    private void ApplyStateColor(EnemyBrainState state)
    {
        switch (state)
        {
            case EnemyBrainState.Telegraph:
                SetColor(telegraphTint);
                break;

            case EnemyBrainState.Startup:
                SetColor(startupTint);
                break;

            case EnemyBrainState.Staggered:
                float pulse = Mathf.PingPong(Time.time * 8f, 1f);
                SetColor(Color.Lerp(staggeredColorA, staggeredColorB, pulse));
                break;

            case EnemyBrainState.Dead:
                SetColor(deadColor);
                break;

            default:
                SetColor(normalColor);
                break;
        }
    }

    private void SetColor(Color color)
    {
        if (targetRenderers == null) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, color);
            _propBlock.SetColor(LegacyColorId, color);
            r.SetPropertyBlock(_propBlock);
        }
    }
}