using UnityEngine;

/// <summary>Telegraphed, straight-line boss attack for the prototype Goo.</summary>
public sealed class PrototypeGooCharge : MonoBehaviour
{
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float windupDuration = 1f;
    [SerializeField] private float distance = 18f;
    // Charge speed = chase speed x this: 6 x 3.375 = 20.25, 1.5x the original 3 x 4.5.
    [SerializeField] private float speedMultiplier = 3.375f;
    [SerializeField] private float knockbackDistance = 6f;
    [Tooltip("The charge is Goo's big move: 7-11 damage (before armor).")]
    [SerializeField] private int chargeMinDamage = 7;
    [SerializeField] private int chargeMaxDamage = 11;
    [SerializeField] private float knockbackDuration = 0.25f;

    private enum ChargeState { Ready, Windup, Charging }
    private ChargeState state;
    private PrototypePlayerMovement player;
    private Renderer bossRenderer;
    private Color originalColor;
    private Transform telegraph;
    private Material telegraphMaterial;
    private float normalSpeed;
    private float cooldownRemaining;
    private float windupRemaining;
    private float distanceRemaining;
    private float bodyRadius;
    private float hitRadius;
    private int damage;
    private bool hitPlayer;
    private Vector3 lockedDirection;

    public bool IsActive => state != ChargeState.Ready;

    public void Initialize(PrototypePlayerMovement target, float chaseSpeed, int attackDamage)
    {
        player = target;
        normalSpeed = chaseSpeed;
        damage = attackDamage;
        cooldownRemaining = cooldown;
        bossRenderer = GetComponent<Renderer>();
        if (bossRenderer != null) originalColor = bossRenderer.material.color;
        var body = GetComponent<Collider>();
        bodyRadius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) : 1.5f;

        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "Goo_Charge_Telegraph";
        indicator.transform.SetParent(transform, true);
        indicator.GetComponent<Collider>().enabled = false;
        telegraphMaterial = PrototypeBootstrap.CreateMaterial("Goo Charge Telegraph", new Color(1f, 0.38f, 0.08f));
        indicator.GetComponent<Renderer>().sharedMaterial = telegraphMaterial;
        telegraph = indicator.transform;
        indicator.SetActive(false);
    }

    public bool TryStart()
    {
        if (state != ChargeState.Ready || cooldownRemaining > 0f || player == null) return false;
        // Measured per charge: the small body stops at walls (so Goo can charge down corridors);
        // the bigger model-sized radius decides whether the ranger is run over.
        var body = GetComponent<Collider>();
        bodyRadius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) : 1.5f;
        hitRadius = bossRenderer != null ? Mathf.Max(bodyRadius, bossRenderer.bounds.extents.x * 0.6f) : bodyRadius;
        state = ChargeState.Windup;
        windupRemaining = windupDuration;
        if (bossRenderer != null) bossRenderer.material.color = new Color(1f, 0.48f, 0.08f);
        telegraph.gameObject.SetActive(true);
        UpdateTelegraph(DirectionToPlayer());
        return true;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || player == null) return;
        if (state == ChargeState.Ready)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            return;
        }
        if (state == ChargeState.Windup)
        {
            var aim = DirectionToPlayer();
            UpdateTelegraph(aim);
            windupRemaining -= Time.deltaTime;
            if (windupRemaining > 0f) return;
            lockedDirection = aim;
            distanceRemaining = distance;
            hitPlayer = false;
            telegraph.gameObject.SetActive(false);
            if (bossRenderer != null) bossRenderer.material.color = originalColor;
            transform.rotation = Quaternion.LookRotation(lockedDirection);
            state = ChargeState.Charging;
            return;
        }

        var travel = Mathf.Min(distanceRemaining, normalSpeed * speedMultiplier * Time.deltaTime);
        var wallDistance = travel;
        var hitWall = false;
        foreach (var hit in Physics.SphereCastAll(transform.position, bodyRadius * 0.95f, lockedDirection, travel, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.name.StartsWith("Wall_") && hit.collider.name != "Door_Panel") continue;
            wallDistance = Mathf.Min(wallDistance, Mathf.Max(0f, hit.distance - 0.02f));
            hitWall = true;
        }
        var start = transform.position;
        var end = start + lockedDirection * wallDistance;
        if (!hitPlayer && PlayerTouchesSegment(start, end))
        {
            hitPlayer = true;
            player.GetComponent<PrototypePlayerStats>()?.TakeAttackDamage(Random.Range(chargeMinDamage, chargeMaxDamage + 1));
            PrototypeGameFeel.Shake(0.6f, 0.35f);
            PrototypeGameFeel.HitStop(0.1f);
            player.ApplyKnockback(KnockbackDirection(start, end), knockbackDistance, knockbackDuration);
        }
        transform.position = end;
        distanceRemaining -= wallDistance;
        if (hitWall || distanceRemaining <= 0.001f) FinishCharge();
    }

    /// <summary>Forward along the charge and sideways out of its path.</summary>
    private Vector3 KnockbackDirection(Vector3 start, Vector3 end)
    {
        var position = player.transform.position;
        start.y = end.y = position.y = 0f;
        var segment = end - start;
        var t = segment.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(position - start, segment) / segment.sqrMagnitude) : 0f;
        var sideways = position - (start + segment * t);
        if (sideways.sqrMagnitude < 0.01f) sideways = Vector3.Cross(Vector3.up, lockedDirection);
        return (lockedDirection * 0.6f + sideways.normalized).normalized;
    }

    private Vector3 DirectionToPlayer()
    {
        var offset = player.transform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude > 0.0001f ? offset.normalized : transform.forward;
    }

    private void UpdateTelegraph(Vector3 direction)
    {
        telegraph.position = transform.position + direction * (distance * 0.5f);
        telegraph.position = new Vector3(telegraph.position.x, 0.15f, telegraph.position.z);
        telegraph.rotation = Quaternion.LookRotation(direction);
        // The parent is the scaled boss, so compensate to keep the marker's world size stable.
        telegraph.localScale = new Vector3(0.7f / transform.lossyScale.x, 0.06f / transform.lossyScale.y, distance / transform.lossyScale.z);
    }

    private bool PlayerTouchesSegment(Vector3 start, Vector3 end)
    {
        var position = player.transform.position;
        start.y = end.y = position.y = 0f;
        var segment = end - start;
        var t = segment.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(position - start, segment) / segment.sqrMagnitude) : 0f;
        var controller = player.GetComponent<CharacterController>();
        var radius = Mathf.Max(hitRadius, bodyRadius) + (controller != null ? controller.radius : 0.45f);
        return (position - (start + segment * t)).sqrMagnitude <= radius * radius;
    }

    private void FinishCharge()
    {
        state = ChargeState.Ready;
        cooldownRemaining = cooldown;
    }

    private void OnDestroy()
    {
        if (telegraphMaterial != null) Destroy(telegraphMaterial);
    }
}
