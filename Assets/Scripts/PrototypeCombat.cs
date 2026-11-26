using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary ranger combat test:
/// J fires a straight bow shot in the direction the player is facing.
/// K performs a short facing-direction melee slash.
/// </summary>
[RequireComponent(typeof(PrototypePlayerMovement))]
public sealed class PrototypeCombat : MonoBehaviour
{
    [SerializeField] private float bowRange = 40f;
    [SerializeField] private float bowCooldown = 0.35f;
    [SerializeField] private float projectileSpeed = 24f;
    [SerializeField] private int bowDamage = 1;
    [SerializeField] private int punchDamage = 1;
    [SerializeField] private float punchReach = 1f;
    [SerializeField] private float punchCooldown = 0.35f;

    private PrototypePlayerMovement movement;
    private float nextBowTime;
    private float nextMeleeTime;
    private int consecutiveCheatKeys;
    public bool OneHitBowEnabled { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PrototypePlayerMovement>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (PrototypeGameUI.IsGamePaused)
        {
            consecutiveCheatKeys = 0;
            return;
        }

        var stats = GetComponent<PrototypePlayerStats>();
        if (stats != null && stats.IsDead)
        {
            consecutiveCheatKeys = 0;
            return;
        }

        HandleCheatInput(Keyboard.current);

        if (Keyboard.current.jKey.wasPressedThisFrame && Time.time >= nextBowTime)
        {
            FireBow();
        }

        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextMeleeTime)
            TryMeleeAttack();
    }

    private void TryMeleeAttack()
    {
        var weapon = GetComponent<PrototypeInventory>()?.GetSlot(0);
        if (weapon == null)
            PerformMeleeAttack(new PrototypeMeleeWeapons.WeaponData("punch", "Punch", punchDamage, punchReach, punchCooldown, Color.white));
        else if (weapon.EquipmentSlot == PrototypeEquipmentSlot.Weapon && PrototypeMeleeWeapons.TryGet(weapon.ItemId, out var meleeWeapon))
        {
            PerformMeleeAttack(meleeWeapon);
        }
    }

    private void HandleCheatInput(Keyboard keyboard)
    {
        foreach (var key in keyboard.allKeys)
        {
            if (key != keyboard.qKey && key.wasPressedThisFrame)
            {
                consecutiveCheatKeys = 0;
                return;
            }
        }

        if (!keyboard.qKey.wasPressedThisFrame) return;
        consecutiveCheatKeys++;
        if (consecutiveCheatKeys == 5)
        {
            OneHitBowEnabled = !OneHitBowEnabled;
            consecutiveCheatKeys = 0;
        }
    }

    private void FireBow()
    {
        nextBowTime = Time.time + bowCooldown;
        var direction = movement.FacingDirection;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.forward;
        }

        var arrow = new GameObject();
        arrow.name = "Arrow_Projectile";
        arrow.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.15f + direction * 0.65f, Quaternion.LookRotation(direction));
        var shaftMaterial = CreateMaterial("Prototype Arrow Shaft", new Color(0.68f, 0.4f, 0.16f));
        var tipMaterial = CreateMaterial("Prototype Arrow Tip", new Color(0.85f, 0.88f, 0.9f));
        AddArrowPart(arrow.transform, "Shaft", Vector3.zero, new Vector3(0.12f, 0.12f, 1.15f), shaftMaterial);
        AddArrowPart(arrow.transform, "Arrowhead", new Vector3(0f, 0f, 0.65f), new Vector3(0.3f, 0.16f, 0.25f), tipMaterial);
        AddArrowPart(arrow.transform, "Fletching", new Vector3(0f, 0f, -0.48f), new Vector3(0.32f, 0.1f, 0.25f), tipMaterial);

        var projectile = arrow.AddComponent<PrototypeProjectile>();
        projectile.Initialize(direction, projectileSpeed, OneHitBowEnabled ? int.MaxValue : bowDamage, bowRange);
    }

    private static void AddArrowPart(Transform parent, string partName, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        part.GetComponent<Collider>().enabled = false;
    }

    private void PerformMeleeAttack(PrototypeMeleeWeapons.WeaponData weapon)
    {
        nextMeleeTime = Time.time + weapon.Cooldown;
        var facing = movement.FacingDirection;

        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            var directionToEnemy = enemy.transform.position - transform.position;
            directionToEnemy.y = 0f;
            var collider = enemy.GetComponent<Collider>();
            var closest = collider != null ? collider.ClosestPoint(transform.position) : enemy.transform.position;
            var offset = closest - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance <= weapon.Range && Vector3.Dot(facing, directionToEnemy.normalized) >= 0.7f && HasClearMeleePath(offset, distance))
            {
                enemy.TakeDamage(weapon.Damage, true);
            }
        }

        StartCoroutine(AnimateSlash(facing, weapon.Range));
    }

    private bool HasClearMeleePath(Vector3 offset, float distance)
    {
        if (distance < 0.001f) return true;
        foreach (var hit in Physics.RaycastAll(transform.position, offset / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider.name.StartsWith("Wall_") || hit.collider.name == "Door_Panel") return false;
        return true;
    }

    private IEnumerator AnimateSlash(Vector3 direction, float reach)
    {
        var slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slash.name = "Melee_Slash";
        slash.transform.SetPositionAndRotation(
            transform.position + Vector3.up * 0.25f + direction * 0.7f,
            Quaternion.LookRotation(direction));
        slash.transform.localScale = new Vector3(0.12f, 0.12f, 0.1f);
        slash.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Prototype Slash Material", new Color(1f, 0.55f, 0.1f));

        var elapsed = 0f;
        const float duration = 0.18f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            slash.transform.position = transform.position + Vector3.up * 0.25f + direction * Mathf.Lerp(0.45f, reach * 0.65f, progress);
            slash.transform.localScale = new Vector3(0.12f, 0.12f, Mathf.Lerp(0.1f, reach * 0.4f, progress));
            yield return null;
        }

        Destroy(slash);
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Smoothness", 0f);
        return material;
    }
}
