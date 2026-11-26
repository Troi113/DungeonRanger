using UnityEngine;

/// <summary>
/// Presentation only: melee attack animations for the KayKit Ranger, posed in code because the
/// free KayKit pack has no combat clips. Runs after the Legacy Animation each frame and bends
/// the right arm and chest toward a punch, stab, or slash in three beats: a quick wind-up
/// (arm cocked back, shoulders turned), a snappy strike with a lunge and a weapon trail, and a
/// follow-through. `StrikeDelay` tells combat when the blow lands.
/// Also holds the equipped melee weapon model in the right hand slot.
///   Punch: bare hands.
///   Sword / dagger (knife) / axe: alternate slash and stab.
///   Spear (a long staff with a blade tip): alternate stab and slash, with a longer lunge.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class PrototypeHeroAttackAnimation : MonoBehaviour
{
    private enum Move { Punch, Slash, Stab }



    private PrototypePlayerMovement movement;
    private PrototypeInventory inventory;
    private Transform holder;
    private Transform upperArm;
    private Transform lowerArm;
    private Transform hand;
    private Transform handSlot;
    private Transform chest;
    private TrailRenderer trail;
    private GameObject weaponModel;
    private string shownWeapon;
    private Move move;
    private float elapsed;
    private float duration;
    private float lunge;
    private bool attacking;
    private int swingCount;

    // Fractions of the attack: wind-up ends, strike lands, then follow-through and recovery.
    private const float WindupEnd = 0.28f;
    private const float StrikeEnd = 0.45f;

    /// <summary>Seconds from the start of the attack until the blow lands.</summary>
    public float StrikeDelay => duration * WindupEnd + (StrikeEnd - WindupEnd) * duration * 0.5f;

    private void Start()
    {
        movement = GetComponent<PrototypePlayerMovement>();
        inventory = GetComponent<PrototypeInventory>();
    }

    /// <summary>Called by PrototypeCombat when a melee attack starts.</summary>
    public bool PlayAttack(string weaponId, float seconds)
    {
        if (!FindBones()) return false;
        var isSpear = weaponId == "spear";
        if (weaponId == "punch") move = Move.Punch;
        else move = (swingCount++ % 2 == 0) == isSpear ? Move.Stab : Move.Slash;
        lunge = move == Move.Punch ? 0.25f : isSpear ? 0.55f : 0.35f;
        duration = Mathf.Clamp(seconds, 0.18f, 0.45f);
        elapsed = 0f;
        attacking = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!FindBones()) return;
        UpdateWeaponModel();
        if (!attacking) return;

        elapsed += Time.deltaTime;
        var t = Mathf.Clamp01(elapsed / duration);
        if (t >= 1f)
        {
            attacking = false;
            holder.localPosition = Vector3.zero;
            if (trail != null) trail.emitting = false;
            return;
        }

        var forward = movement.FacingDirection;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        var right = Vector3.Cross(Vector3.up, forward);

        // Three beats: wind-up (0 -> WindupEnd), strike (-> StrikeEnd, eased out), recovery.
        float strike;     // 0 = cocked back, 1 = fully extended
        float weight;     // how strongly the pose overrides the idle/run animation
        float lungeAmount;
        if (t < WindupEnd)
        {
            var w = t / WindupEnd;
            strike = 0f;
            weight = Mathf.SmoothStep(0f, 1f, w);
            lungeAmount = -0.15f * weight;
        }
        else if (t < StrikeEnd)
        {
            var s = (t - WindupEnd) / (StrikeEnd - WindupEnd);
            strike = 1f - (1f - s) * (1f - s) * (1f - s);
            weight = 1f;
            lungeAmount = Mathf.Lerp(-0.15f, 1f, strike);
        }
        else
        {
            var r = (t - StrikeEnd) / (1f - StrikeEnd);
            strike = 1f;
            weight = 1f - Mathf.SmoothStep(0f, 1f, r);
            lungeAmount = weight;
        }

        Vector3 aim;
        float twist;
        switch (move)
        {
            case Move.Slash:
                // Cocked high behind the right shoulder, then a fast sweep across to low left.
                var angle = Mathf.Lerp(135f, -80f, strike) * Mathf.Deg2Rad;
                aim = forward * Mathf.Cos(angle) + right * Mathf.Sin(angle) + Vector3.up * Mathf.Lerp(0.9f, -0.4f, strike);
                twist = Mathf.Lerp(35f, -40f, strike);
                break;
            case Move.Stab:
                aim = Vector3.Lerp(-forward * 0.6f + right * 0.5f + Vector3.up * 0.1f, forward + Vector3.down * 0.05f, strike);
                twist = Mathf.Lerp(30f, -25f, strike);
                break;
            default:
                aim = Vector3.Lerp(-forward * 0.5f + right * 0.6f + Vector3.down * 0.2f, forward + Vector3.down * 0.1f + right * 0.1f, strike);
                twist = Mathf.Lerp(35f, -30f, strike);
                break;
        }

        if (chest != null) chest.rotation = Quaternion.AngleAxis(twist * weight, Vector3.up) * chest.rotation;
        Aim(upperArm, lowerArm.position - upperArm.position, aim, weight);
        Aim(lowerArm, hand.position - lowerArm.position, aim, weight);
        // Point the weapon along the strike (blades extend along the hand slot's up axis).
        if (move != Move.Punch && handSlot != null) Aim(hand, handSlot.up, aim, weight);
        holder.localPosition = holder.parent.InverseTransformVector(forward * lunge * lungeAmount);
        holder.localPosition = new Vector3(holder.localPosition.x, 0f, holder.localPosition.z);
        if (trail != null) trail.emitting = t >= WindupEnd && t < StrikeEnd + 0.1f;
    }

    private static void Aim(Transform bone, Vector3 current, Vector3 target, float weight)
    {
        if (current.sqrMagnitude < 0.0001f || target.sqrMagnitude < 0.0001f) return;
        var turn = Quaternion.FromToRotation(current, target);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * bone.rotation;
    }

    private bool FindBones()
    {
        if (hand != null) return true;
        holder = transform.Find("Hero_Model");
        if (holder == null) return false;
        foreach (var child in holder.GetComponentsInChildren<Transform>(true))
        {
            switch (child.name)
            {
                case "upperarm.r": upperArm = child; break;
                case "lowerarm.r": lowerArm = child; break;
                case "hand.r": hand = child; break;
                case "handslot.r": handSlot = child; break;
                case "chest": chest = child; break;
            }
        }
        if (upperArm == null || lowerArm == null || hand == null) { hand = null; return false; }
        CreateTrail();
        return true;
    }

    private void UpdateWeaponModel()
    {
        var item = inventory != null ? inventory.GetSlot(0) : null;
        var weaponId = item != null && item.EquipmentSlot == PrototypeEquipmentSlot.Weapon ? item.ItemId : null;
        if (weaponId == shownWeapon) return;
        shownWeapon = weaponId;
        if (weaponModel != null) Destroy(weaponModel);
        weaponModel = null;
        var parent = handSlot != null ? handSlot : hand;
        // Trail at the blade tip (local units of the hand slot), or at the fist.
        PlaceTrail(PrototypeItemVisuals.HeldSize(weaponId, out var length, out var grip) ? length * (1f - grip) : 0f);
        if (weaponId != null) weaponModel = PrototypeItemVisuals.CreateHeld(weaponId, parent);
    }

    /// <summary>A short white swoosh that follows the fist or weapon tip during the strike.</summary>
    private void CreateTrail()
    {
        var tip = new GameObject("Swing_Trail");
        tip.transform.SetParent(handSlot != null ? handSlot : hand, false);
        trail = tip.AddComponent<TrailRenderer>();
        trail.time = 0.14f;
        trail.minVertexDistance = 0.02f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0f));
        trail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.5f), 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        };
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        trail.sharedMaterial = new Material(shader) { name = "Swing Trail" };
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.emitting = false;
    }

    /// <summary>Moves the trail to the weapon's tip (or the fist) for the current weapon.</summary>
    private void PlaceTrail(float reach)
    {
        if (trail != null) trail.transform.localPosition = Vector3.up * reach;
    }
}
