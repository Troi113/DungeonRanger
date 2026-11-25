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
    [SerializeField] private float bowRange = 8f;
    [SerializeField] private float bowCooldown = 0.35f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int bowDamage = 1;

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

        var weapon = GetComponent<PrototypeInventory>()?.GetSlot(0);
        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextMeleeTime && weapon != null &&
            weapon.EquipmentSlot == PrototypeEquipmentSlot.Weapon && PrototypeMeleeWeapons.TryGet(weapon.ItemId, out var meleeWeapon))
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

        var arrow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arrow.name = "Arrow_Projectile";
        arrow.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.15f + direction * 0.65f, Quaternion.identity);
        arrow.transform.localScale = Vector3.one * 0.18f;
        arrow.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Prototype Arrow Material", new Color(0.95f, 0.82f, 0.25f));

        var projectile = arrow.AddComponent<PrototypeProjectile>();
        projectile.Initialize(direction, projectileSpeed, OneHitBowEnabled ? int.MaxValue : bowDamage, bowRange + 4f);
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

            var offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance <= weapon.Range && Vector3.Dot(facing, offset.normalized) >= 0.25f)
            {
                enemy.TakeDamage(weapon.Damage);
            }
        }

        StartCoroutine(AnimateSlash(facing));
    }

    private IEnumerator AnimateSlash(Vector3 direction)
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
            slash.transform.position = transform.position + Vector3.up * 0.25f + direction * Mathf.Lerp(0.45f, 0.95f, progress);
            slash.transform.localScale = new Vector3(0.12f, 0.12f, Mathf.Lerp(0.1f, 0.75f, progress));
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
