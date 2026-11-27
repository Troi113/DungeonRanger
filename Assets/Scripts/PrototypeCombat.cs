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
    [Tooltip("Chance an arrow misses an enemy within one block of the ranger.")]
    [SerializeField, Range(0f, 1f)] private float pointBlankMissChance = 0.4f;
    [SerializeField] private int punchDamage = 1;
    [Tooltip("Reach in dungeon tiles; 1 tile = PrototypeLevelGenerator.TileSize world units.")]
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

        // Paralyzed rangers cannot attack.
        var paralyzed = GetComponent<PrototypeStatusEffects>();
        if (paralyzed != null && paralyzed.IsParalyzed) return;

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
            // Strength (Pixel Dungeon rules): too weak = slower attacks; extra Strength = bonus damage.
            var strength = GetComponent<PrototypePlayerStats>()?.Strength ?? 10;
            var shortfall = PrototypeStrength.Shortfall(weapon, strength);
            var bonus = Random.Range(0, PrototypeStrength.Excess(weapon, strength) + 1);
            PerformMeleeAttack(new PrototypeMeleeWeapons.WeaponData(meleeWeapon.Id, meleeWeapon.Name,
                meleeWeapon.Damage + weapon.UpgradeLevel + bonus, meleeWeapon.Range,
                meleeWeapon.Cooldown * PrototypeStrength.WeaponDelayMultiplier(shortfall), meleeWeapon.Color));
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
        PrototypeCombatLog.Add("You shoot an arrow.");
        nextBowTime = Time.time + bowCooldown * (GetComponent<PrototypeStatusEffects>()?.ActionDelayMultiplier ?? 1f);
        EndInvisibility();
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
        // The one-hit cheat never misses.
        projectile.Initialize(direction, projectileSpeed, OneHitBowEnabled ? int.MaxValue : bowDamage, bowRange,
            transform.position, OneHitBowEnabled ? 0f : pointBlankMissChance);
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
        PrototypeCombatLog.Add(weapon.Id == "punch" ? "You throw a punch." : $"You attack with your {weapon.Name}.");
        EndInvisibility();
        // Slowed: attacks take twice as long (Pixel Dungeon Slow).
        nextMeleeTime = Time.time + weapon.Cooldown * (GetComponent<PrototypeStatusEffects>()?.ActionDelayMultiplier ?? 1f);
        var facing = movement.FacingDirection;
        facing.y = 0f;
        facing.Normalize();
        var depth = weapon.Range * PrototypeLevelGenerator.TileSize;

        // The Ranger model winds up and strikes; the blow lands at the strike moment.
        // The old box swing is only a fallback when the model is missing.
        var attackAnimation = GetComponent<PrototypeHeroAttackAnimation>();
        if (attackAnimation != null && attackAnimation.PlayAttack(weapon.Id, weapon.Cooldown * 0.85f, MeleeReachDistance(weapon, facing, depth)))
        {
            StartCoroutine(StrikeAfter(attackAnimation.StrikeDelay, weapon, facing));
            return;
        }
        ResolveMeleeHit(weapon, facing);
        StartCoroutine(AnimateSlash(facing, depth));
    }

    /// <summary>
    /// How far ahead the attack animation stretches the fist or weapon tip: to the far edge of the
    /// hit area, or to the nearest enemy in it, never through a wall or closed door. Looks only.
    /// </summary>
    private float MeleeReachDistance(PrototypeMeleeWeapons.WeaponData weapon, Vector3 facing, float depth)
    {
        var reach = depth;
        var side = new Vector3(facing.z, 0f, -facing.x);
        var halfWidth = PrototypeLevelGenerator.TileSize * 0.5f;
        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || !enemy.IsAlive) continue;
            var collider = enemy.GetComponent<Collider>();
            var offset = (collider != null ? collider.ClosestPoint(transform.position) : enemy.transform.position) - transform.position;
            offset.y = 0f;
            var ahead = Vector3.Dot(offset, facing);
            if (ahead < 0f || ahead > depth || Mathf.Abs(Vector3.Dot(offset, side)) > halfWidth) continue;
            reach = Mathf.Min(reach, ahead + 0.3f);
        }

        foreach (var hit in Physics.RaycastAll(transform.position, facing, depth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider.name.StartsWith("Wall_") || hit.collider.name == "Door_Panel")
                reach = Mathf.Min(reach, hit.distance - 0.2f);
        return Mathf.Max(0f, reach);
    }

    /// <summary>Attacking reveals an invisible ranger (Pixel Dungeon rule).</summary>
    private void EndInvisibility() =>
        GetComponent<PrototypeStatusEffects>()?.Cure(PrototypeStatusEffectType.Invisible);

    private IEnumerator StrikeAfter(float delay, PrototypeMeleeWeapons.WeaponData weapon, Vector3 facing)
    {
        yield return new WaitForSeconds(delay);
        var stats = GetComponent<PrototypePlayerStats>();
        if (stats == null || !stats.IsDead) ResolveMeleeHit(weapon, facing);
    }

    /// <summary>Damages enemies in the tile-wide box ahead and plays the impact effects on a hit.</summary>
    private void ResolveMeleeHit(PrototypeMeleeWeapons.WeaponData weapon, Vector3 facing)
    {
        var side = new Vector3(facing.z, 0f, -facing.x);
        // The hit area is a tile-wide box in front of the player, weapon.Range tiles deep.
        var depth = weapon.Range * PrototypeLevelGenerator.TileSize;
        var halfWidth = PrototypeLevelGenerator.TileSize * 0.5f;
        var hits = 0;
        var heavy = weapon.Damage >= 3 || weapon.Id == "spear";

        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            var collider = enemy.GetComponent<Collider>();
            var closest = collider != null ? collider.ClosestPoint(transform.position) : enemy.transform.position;
            var offset = closest - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            var forward = Vector3.Dot(offset, facing);
            if (forward >= 0f && forward <= depth && Mathf.Abs(Vector3.Dot(offset, side)) <= halfWidth && HasClearMeleePath(offset, distance))
            {
                // Followup Strike talent: extra damage right after a bow hit on this enemy.
                var followup = GetComponent<PrototypeTalents>()?.TakeFollowupBonus(enemy) ?? 0;
                enemy.TakeDamage(weapon.Damage + followup, true);
                hits++;
                PrototypeGameFeel.Sparks(new Vector3(closest.x, 1.1f, closest.z), new Color(1f, 0.85f, 0.45f), heavy ? 22 : 14);
            }
        }

        if (hits == 0)
        {
            PrototypeCombatLog.Add("Your attack hits nothing.");
            return;
        }
        // Bigger weapons and multi-hits land harder.
        PrototypeGameFeel.HitStop(heavy ? 0.09f : 0.06f + 0.015f * (hits - 1));
        PrototypeGameFeel.Shake(heavy ? 0.35f : 0.22f, 0.18f);
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
            slash.transform.position = transform.position + Vector3.up * 0.25f + direction * Mathf.Lerp(0.45f, reach * 0.5f, progress);
            slash.transform.localScale = new Vector3(Mathf.Lerp(0.12f, PrototypeLevelGenerator.TileSize * 0.9f, progress), 0.12f, Mathf.Lerp(0.1f, reach * 0.9f, progress));
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
