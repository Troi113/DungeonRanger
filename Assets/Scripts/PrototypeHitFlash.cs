using UnityEngine;

/// <summary>
/// Presentation only: when an enemy takes damage it flashes bright white and squashes for a
/// moment. Saves each renderer's property block (sleep/awake tints) and restores it after.
/// Goo already wobbles from its own face script, so only its flash is applied.
/// </summary>
[RequireComponent(typeof(PrototypeEnemy))]
public sealed class PrototypeHitFlash : MonoBehaviour
{
    [SerializeField] private float flashSeconds = 0.09f;
    [SerializeField] private float squashSeconds = 0.18f;
    [SerializeField] private float squashAmount = 0.25f;
    [SerializeField] private Color flashColor = new(3f, 3f, 3f, 1f);

    private PrototypeEnemy enemy;
    private Renderer[] renderers;
    private MaterialPropertyBlock[] saved;
    private MaterialPropertyBlock flashBlock;
    private Transform squashTarget;
    private Vector3 squashBase;
    private float flashTimer;
    private float squashTimer;
    private bool flashing;

    private void Start()
    {
        enemy = GetComponent<PrototypeEnemy>();
        enemy.Damaged += OnDamaged;
        // Squash the model child when there is one (the root carries the hit box).
        if (GetComponent<PrototypeGooFace>() == null)
        {
            var model = transform.childCount > 0 ? transform.GetChild(0) : null;
            squashTarget = model != null && model.GetComponentInChildren<Renderer>() != null ? model : null;
            if (squashTarget != null) squashBase = squashTarget.localScale;
        }
        flashBlock = new MaterialPropertyBlock();
        flashBlock.SetColor("_BaseColor", flashColor);
        flashBlock.SetColor("_Color", flashColor);
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.Damaged -= OnDamaged;
    }

    private void OnDamaged()
    {
        if (!flashing)
        {
            renderers = GetComponentsInChildren<Renderer>();
            saved = new MaterialPropertyBlock[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                saved[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(saved[i]);
                renderers[i].SetPropertyBlock(flashBlock);
            }
            flashing = true;
        }
        flashTimer = flashSeconds;
        squashTimer = squashSeconds;
    }

    private void Update()
    {
        // Unscaled so the flash reads clearly during hit-stop.
        var dt = Time.unscaledDeltaTime;
        if (flashing)
        {
            flashTimer -= dt;
            if (flashTimer <= 0f) EndFlash();
        }

        if (squashTarget == null || squashTimer <= 0f) return;
        squashTimer -= dt;
        var k = Mathf.Clamp01(squashTimer / squashSeconds);
        var wobble = Mathf.Sin((1f - k) * Mathf.PI * 2.5f) * squashAmount * k;
        squashTarget.localScale = new Vector3(squashBase.x * (1f + wobble), squashBase.y * (1f - wobble), squashBase.z * (1f + wobble));
        if (squashTimer <= 0f) squashTarget.localScale = squashBase;
    }

    private void EndFlash()
    {
        flashing = false;
        if (renderers == null) return;
        for (var i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].SetPropertyBlock(saved[i]);
    }
}
