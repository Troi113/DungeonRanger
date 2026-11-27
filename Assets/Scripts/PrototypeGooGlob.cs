using UnityEngine;

/// <summary>
/// Goo's ranged attack: every few seconds, when it can see the ranger and is not charging, it
/// swells a caustic glob in front of itself (a visible wind-up) and spits it at the ranger.
/// A hit slows the ranger and burns for 1 damage per second for 3 seconds (Oozed); walls stop it.
/// </summary>
[RequireComponent(typeof(PrototypeEnemy))]
public sealed class PrototypeGooGlob : MonoBehaviour
{
    [SerializeField] private float minCooldown = 5f;
    [SerializeField] private float maxCooldown = 9f;
    [SerializeField] private float range = 34f;
    [SerializeField] private float windupSeconds = 0.8f;
    [SerializeField] private float speed = 20f; // was 13
    [SerializeField] private float effectSeconds = 3f;

    private PrototypeEnemy enemy;
    private PrototypeGooCharge charge;
    private PrototypePlayerMovement player;
    private Transform swelling;
    private Material globMaterial;
    private float cooldown;
    private float windup = -1f;

    private void Start()
    {
        enemy = GetComponent<PrototypeEnemy>();
        charge = GetComponent<PrototypeGooCharge>();
        cooldown = Random.Range(minCooldown, maxCooldown);
        globMaterial = PrototypeBootstrap.CreateMaterial("Caustic Glob", new Color(0.55f, 0.95f, 0.2f));
        if (globMaterial.HasProperty("_EmissionColor"))
        {
            globMaterial.EnableKeyword("_EMISSION");
            globMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 0.05f));
        }
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || !enemy.IsAlive) return;
        if (player == null) player = FindAnyObjectByType<PrototypePlayerMovement>();
        if (player == null) return;

        if (windup >= 0f)
        {
            windup += Time.deltaTime;
            var grow = Mathf.Clamp01(windup / windupSeconds);
            if (swelling != null)
            {
                swelling.position = MouthPosition();
                swelling.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.4f, grow);
            }
            if (windup >= windupSeconds) Fire();
            return;
        }

        cooldown -= Time.deltaTime;
        var statuses = player.GetComponent<PrototypeStatusEffects>();
        var invisible = statuses != null && statuses.Has(PrototypeStatusEffectType.Invisible);
        if (cooldown > 0f || invisible || (charge != null && charge.IsActive) || !CanSeePlayer()) return;
        windup = 0f;
        swelling = CreateGlob().transform;
    }

    private Vector3 MouthPosition()
    {
        var toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        var direction = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
        var reach = GetComponent<Renderer>() != null ? GetComponent<Renderer>().bounds.extents.x * 0.9f : 2f;
        return new Vector3(transform.position.x, 1.3f, transform.position.z) + direction * reach;
    }

    private bool CanSeePlayer()
    {
        var from = new Vector3(transform.position.x, 1.3f, transform.position.z);
        var offset = player.transform.position - from;
        var distance = offset.magnitude;
        if (distance > range || distance < 0.01f) return false;
        foreach (var hit in Physics.RaycastAll(from, offset / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider.name.StartsWith("Wall_") || hit.collider.name == "Door_Panel") return false;
        return true;
    }

    private GameObject CreateGlob()
    {
        var glob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glob.name = "Caustic_Glob";
        Destroy(glob.GetComponent<Collider>());
        glob.GetComponent<Renderer>().sharedMaterial = globMaterial;
        glob.transform.SetParent(transform.parent, true);
        glob.transform.position = MouthPosition();
        glob.transform.localScale = Vector3.one * 0.3f;
        return glob;
    }

    private void Fire()
    {
        windup = -1f;
        cooldown = Random.Range(minCooldown, maxCooldown);
        if (swelling == null) return;
        var target = player.transform.position;
        var direction = target - swelling.position;
        direction.y = 0f;
        var flight = swelling.gameObject.AddComponent<PrototypeCausticGlob>();
        flight.Launch(direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward, speed, effectSeconds);
        swelling = null;
    }

    private void OnDisable()
    {
        if (swelling != null) Destroy(swelling.gameObject);
        swelling = null;
        windup = -1f;
    }
}
