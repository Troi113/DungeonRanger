using UnityEngine;

/// <summary>
/// Presentation only: melee attack animations for the KayKit Ranger, posed in code because the
/// free KayKit pack has no combat clips. Runs after the Legacy Animation each frame and bends
/// the right arm and chest toward a punch, stab, or slash in three beats: a quick wind-up
/// (arm cocked back, shoulders turned), a snappy strike with a weapon trail, and a
/// follow-through. `StrikeDelay` tells combat when the blow lands.
/// Also holds the equipped melee weapon model in the right hand slot.
/// Each K press picks one of the weapon's three moves at random (looks only; damage and hit
/// area are the same for every move). The ranger stays in place; at the strike the arm and the
/// weapon stretch ("warp") so the fist or weapon tip reaches the end of the hit area (the reach
/// distance comes from PrototypeCombat). Fists stretch the arm; weapons stretch the arm a
/// little and the weapon the rest.
///   Fists:  jab (left hand), straight, uppercut.
///   Dagger: slash, backhand slash, stab.      Sword: slash, stab, backhand slash.
///   Spear:  two-handed poke, two-handed slash, one-handed slash.
///   Axe:    swing, poke, overhead slam.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class PrototypeHeroAttackAnimation : MonoBehaviour
{
    private enum Move { Jab, Straight, Uppercut, Slash, Backhand, Stab, TwoHandPoke, TwoHandSlash, Swing, Slam }

    private static readonly Move[] FistMoves = { Move.Jab, Move.Straight, Move.Uppercut };
    private static readonly Move[] BladeMoves = { Move.Slash, Move.Backhand, Move.Stab };
    private static readonly Move[] SpearMoves = { Move.TwoHandPoke, Move.TwoHandSlash, Move.Slash };
    private static readonly Move[] AxeMoves = { Move.Swing, Move.Stab, Move.Slam };

    // The warp only covers part of the gap to the end of the hit area, with caps on the arm
    // (world units) and on how long the weapon may stretch (1.3 = 30% longer).
    [SerializeField, Range(0f, 1f)] private float reachFraction = 0.5f;
    [SerializeField] private float maxFistStretch = 0.35f;
    [SerializeField] private float maxArmStretch = 0.2f;
    [SerializeField] private float maxWeaponStretch = 1.3f;

    private PrototypePlayerMovement movement;
    private PrototypeInventory inventory;
    private Transform holder;
    private Transform upperArm;
    private Transform lowerArm;
    private Transform hand;
    private Transform handSlot;
    private Transform chest;
    private Transform upperArmL;
    private Transform lowerArmL;
    private Transform handL;
    private TrailRenderer trail;
    private GameObject weaponModel;
    private string shownWeapon;
    private Move move;
    private float elapsed;
    private float duration;
    private float reach;
    private float weaponTip;
    private bool attacking;

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

    /// <summary>
    /// Called by PrototypeCombat when a melee attack starts. `reachDistance` is how far ahead of
    /// the ranger (world units) the fist or weapon tip should stretch at the strike.
    /// </summary>
    public bool PlayAttack(string weaponId, float seconds, float reachDistance)
    {
        if (!FindBones()) return false;
        var moves = weaponId switch
        {
            "knife" or "sword" => BladeMoves,
            "spear" => SpearMoves,
            "axe" => AxeMoves,
            _ => FistMoves
        };
        move = moves[Random.Range(0, moves.Length)];
        reach = Mathf.Max(0f, reachDistance);
        weaponTip = PrototypeItemVisuals.HeldSize(weaponId, out var length, out var grip) ? length * (1f - grip) : 0f;
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
            if (weaponModel != null) weaponModel.transform.localScale = Vector3.one;
            if (trail != null) { trail.emitting = false; PlaceTrail(weaponTip); }
            return;
        }

        var forward = movement.FacingDirection;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        var right = Vector3.Cross(Vector3.up, forward);

        // Three beats: wind-up (0 -> WindupEnd), strike (-> StrikeEnd, eased out), recovery.
        float strike;     // 0 = cocked back, 1 = fully extended
        float weight;     // how strongly the pose overrides the idle/run animation
        float extend;     // 0..1 how far the arm/weapon stretch toward the reach distance
        if (t < WindupEnd)
        {
            var w = t / WindupEnd;
            strike = 0f;
            weight = Mathf.SmoothStep(0f, 1f, w);
            extend = 0f;
        }
        else if (t < StrikeEnd)
        {
            var s = (t - WindupEnd) / (StrikeEnd - WindupEnd);
            strike = 1f - (1f - s) * (1f - s) * (1f - s);
            weight = 1f;
            extend = strike;
        }
        else
        {
            var r = (t - StrikeEnd) / (1f - StrikeEnd);
            strike = 1f;
            weight = 1f - Mathf.SmoothStep(0f, 1f, r);
            extend = weight;
        }

        Pose(move, strike, forward, right, out var aim, out var twist);
        var leftLead = move == Move.Jab;
        var twoHanded = move == Move.TwoHandPoke || move == Move.TwoHandSlash || move == Move.Slam;

        if (chest != null) chest.rotation = Quaternion.AngleAxis(twist * weight, Vector3.up) * chest.rotation;
        if (leftLead && upperArmL != null)
        {
            // The jab is thrown with the lead (left) hand.
            Aim(upperArmL, lowerArmL.position - upperArmL.position, aim, weight);
            Aim(lowerArmL, handL.position - lowerArmL.position, aim, weight);
        }
        else
        {
            Aim(upperArm, lowerArm.position - upperArm.position, aim, weight);
            Aim(lowerArm, hand.position - lowerArm.position, aim, weight);
            // Point the weapon along the strike (blades extend along the hand slot's up axis).
            if (!IsFist(move) && handSlot != null) Aim(hand, handSlot.up, aim, weight);
            // Two-handed moves: the left hand grips the shaft a little behind the right hand.
            if (twoHanded && upperArmL != null)
            {
                var grip = hand.position - aim.normalized * 0.45f;
                Aim(upperArmL, lowerArmL.position - upperArmL.position, grip - upperArmL.position, weight);
                Aim(lowerArmL, handL.position - lowerArmL.position, grip - lowerArmL.position, weight);
            }
        }
        Stretch(leftLead && handL != null ? handL : hand, aim, forward, extend);
        if (trail != null) trail.emitting = t >= WindupEnd && t < StrikeEnd + 0.1f;
    }

    /// <summary>
    /// Warps the striking hand (the skinned forearm stretches with it) and the weapon so the tip
    /// reaches `reach` ahead of the ranger, scaled by `extend`.
    /// </summary>
    private void Stretch(Transform strikingHand, Vector3 aim, Vector3 forward, float extend)
    {
        var direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : forward;
        var hasWeapon = weaponModel != null && strikingHand == hand && !IsFist(move);
        if (weaponModel != null) weaponModel.transform.localScale = Vector3.one;
        // The weapon's tip length in world units (the hero model is scaled).
        var tipLength = hasWeapon ? weaponTip * weaponModel.transform.lossyScale.y : 0f;
        var tip = hasWeapon ? weaponModel.transform.position + weaponModel.transform.up * tipLength : strikingHand.position;
        var along = Vector3.Dot(direction, forward);
        // How much farther the tip must go, measured along the facing direction.
        var missing = reach - Vector3.Dot(tip - transform.position, forward);
        if (missing <= 0f || along < 0.2f || extend <= 0f) return;
        var travel = missing * reachFraction / along;
        var armTravel = Mathf.Min(travel, hasWeapon ? maxArmStretch : maxFistStretch);
        strikingHand.position += direction * armTravel * extend;
        var stretch = 1f;
        if (hasWeapon && tipLength > 0.01f)
        {
            stretch = Mathf.Min(1f + (travel - armTravel) / tipLength, maxWeaponStretch);
            stretch = 1f + (stretch - 1f) * extend;
            weaponModel.transform.localScale = new Vector3(1f, stretch, 1f);
        }
        if (trail != null)
            trail.transform.position = hasWeapon ? weaponModel.transform.position + weaponModel.transform.up * tipLength * stretch : strikingHand.position;
    }

    private static bool IsFist(Move m) => m == Move.Jab || m == Move.Straight || m == Move.Uppercut;

    /// <summary>Arm direction and chest twist for a move; strike goes 0 (cocked) to 1 (extended).</summary>
    private static void Pose(Move m, float strike, Vector3 forward, Vector3 right, out Vector3 aim, out float twist)
    {
        var up = Vector3.up;
        switch (m)
        {
            case Move.Jab:
                // Short and quick from the guard, little body turn.
                aim = Vector3.Lerp(forward * 0.2f - right * 0.3f + up * 0.1f, forward + up * 0.05f, strike);
                twist = Mathf.Lerp(-10f, 15f, strike);
                break;
            case Move.Straight:
                aim = Vector3.Lerp(-forward * 0.5f + right * 0.6f + up * 0.05f, forward + up * 0.05f, strike);
                twist = Mathf.Lerp(40f, -35f, strike);
                break;
            case Move.Uppercut:
                aim = Vector3.Lerp(forward * 0.1f + right * 0.35f - up * 0.9f, forward * 0.55f + up, strike);
                twist = Mathf.Lerp(25f, -20f, strike);
                break;
            case Move.Slash:
                // Cocked high behind the right shoulder, then a fast sweep across to low left.
                aim = Arc(forward, right, Mathf.Lerp(135f, -80f, strike), Mathf.Lerp(0.9f, -0.4f, strike));
                twist = Mathf.Lerp(35f, -40f, strike);
                break;
            case Move.Backhand:
                // Wound across the body on the left, then swept out to the right.
                aim = Arc(forward, right, Mathf.Lerp(-120f, 95f, strike), Mathf.Lerp(-0.1f, 0.35f, strike));
                twist = Mathf.Lerp(-40f, 40f, strike);
                break;
            case Move.TwoHandPoke:
                aim = Vector3.Lerp(-forward * 0.3f + right * 0.25f - up * 0.2f, forward - up * 0.05f, strike);
                twist = Mathf.Lerp(20f, -10f, strike);
                break;
            case Move.TwoHandSlash:
                // A flat sweep at waist height, both hands on the shaft.
                aim = Arc(forward, right, Mathf.Lerp(110f, -95f, strike), Mathf.Lerp(0.3f, 0f, strike));
                twist = Mathf.Lerp(40f, -45f, strike);
                break;
            case Move.Swing:
                // A wide, heavy horizontal swing with a big body turn.
                aim = Arc(forward, right, Mathf.Lerp(155f, -105f, strike), Mathf.Lerp(0.45f, -0.15f, strike));
                twist = Mathf.Lerp(55f, -55f, strike);
                break;
            case Move.Slam:
                // Raised overhead, then chopped straight down in front.
                aim = Vector3.Lerp(-forward * 0.45f + up, forward * 0.75f - up * 0.8f, strike);
                twist = Mathf.Lerp(10f, -5f, strike);
                break;
            default: // Stab
                aim = Vector3.Lerp(-forward * 0.6f + right * 0.5f + up * 0.1f, forward - up * 0.05f, strike);
                twist = Mathf.Lerp(30f, -25f, strike);
                break;
        }
    }

    /// <summary>A direction at `degrees` around the ranger (0 = forward, + = right), lifted by `height`.</summary>
    private static Vector3 Arc(Vector3 forward, Vector3 right, float degrees, float height)
    {
        var angle = degrees * Mathf.Deg2Rad;
        return forward * Mathf.Cos(angle) + right * Mathf.Sin(angle) + Vector3.up * height;
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
                case "upperarm.l": upperArmL = child; break;
                case "lowerarm.l": lowerArmL = child; break;
                case "hand.l": handL = child; break;
            }
        }
        if (upperArm == null || lowerArm == null || hand == null) { hand = null; return false; }
        if (lowerArmL == null || handL == null) upperArmL = null; // left-arm moves need all three bones
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
