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
    [SerializeField] private float meleeRange = 1.6f;
    [SerializeField] private float meleeCooldown = 0.3f;
    [SerializeField] private int meleeDamage = 1;

    private PrototypePlayerMovement movement;
    private float nextBowTime;
    private float nextMeleeTime;

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

        if (Keyboard.current.jKey.wasPressedThisFrame && Time.time >= nextBowTime)
        {
            FireBow();
        }

        if (Keyboard.current.kKey.wasPressedThisFrame && Time.time >= nextMeleeTime)
        {
            PerformMeleeAttack();
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
        projectile.Initialize(direction, projectileSpeed, bowDamage, bowRange + 4f);
    }

    private void PerformMeleeAttack()
    {
        nextMeleeTime = Time.time + meleeCooldown;
        var facing = movement.FacingDirection;

        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            var offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance <= meleeRange && Vector3.Dot(facing, offset.normalized) >= 0.25f)
            {
                enemy.TakeDamage(meleeDamage);
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
