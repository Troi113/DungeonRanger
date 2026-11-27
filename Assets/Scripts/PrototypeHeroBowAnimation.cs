using UnityEngine;

/// <summary>
/// Presentation only: a quick bow shot for the KayKit Ranger when J fires an arrow (posed in
/// code like PrototypeHeroAttackAnimation; the model has no clips). A primitive bow appears in
/// the left hand only during the shot. Three beats: the left arm snaps out with the bow while
/// the right hand draws the string back (shoulders turned side-on), the string releases, then
/// the arms lower and the bow vanishes. Each J press picks one of three shots at random
/// (looks only; the arrow is the same):
///   Standard: upright bow, full draw to the chest.
///   Snap:     quick hip shot, bow canted sideways, short draw, early release.
///   Power:    bow raised high, deep draw past the shoulder, big body turn, hard flick release.
/// </summary>
[DefaultExecutionOrder(110)]
public sealed class PrototypeHeroBowAnimation : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float bowHeight = 1.6f;
    [SerializeField] private float bowDepth = 0.3f;
    [SerializeField] private float drawLength = 0.8f;
    [SerializeField] private float stanceTwist = 40f;

    private PrototypePlayerMovement movement;
    private Transform upperArm, lowerArm, hand, chest, upperArmL, lowerArmL, handL;
    private Transform bow, upperLimb, lowerLimb, grip, stringTop, stringBottom;
    private float elapsed = -1f;

    private struct Shot
    {
        public float Release;  // fraction of the shot when the string is let go
        public float Draw;     // draw length multiplier
        public float Lift;     // bow arm height (forward + up * Lift)
        public float Cant;     // bow roll, degrees (0 = upright)
        public float Twist;    // chest turn multiplier
        public float Flick;    // how far the string hand flies back on release
        public float Speed;    // duration multiplier
    }

    private static readonly Shot[] Shots =
    {
        new() { Release = 0.3f, Draw = 1f, Lift = 0.08f, Cant = 0f, Twist = 1f, Flick = 1.1f, Speed = 1f },       // standard
        new() { Release = 0.18f, Draw = 0.55f, Lift = -0.25f, Cant = 55f, Twist = 0.5f, Flick = 0.8f, Speed = 0.8f }, // snap
        new() { Release = 0.42f, Draw = 1.35f, Lift = 0.3f, Cant = -8f, Twist = 1.4f, Flick = 1.6f, Speed = 1.2f },  // power
    };

    private Shot shot;

    /// <summary>Called by PrototypeCombat when an arrow is fired.</summary>
    public void Play()
    {
        if (!FindBones()) return;
        shot = Shots[Random.Range(0, Shots.Length)];
        elapsed = 0f;
        bow.gameObject.SetActive(true);
    }

    private void Start() => movement = GetComponent<PrototypePlayerMovement>();

    private void LateUpdate()
    {
        if (elapsed < 0f || bow == null) return;
        elapsed += Time.deltaTime;
        var t = elapsed / (duration * shot.Speed);
        var releaseAt = shot.Release;
        if (t >= 1f)
        {
            elapsed = -1f;
            bow.gameObject.SetActive(false);
            return;
        }

        var forward = movement != null ? movement.FacingDirection : transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        var right = Vector3.Cross(Vector3.up, forward);

        // Raise fast (to 40% of the draw time), hold drawn, release, then lower over the rest.
        float weight, draw;
        if (t < releaseAt)
        {
            weight = Mathf.SmoothStep(0f, 1f, t / (releaseAt * 0.4f));
            draw = Mathf.SmoothStep(0f, 1f, t / releaseAt);
        }
        else
        {
            var r = (t - releaseAt) / (1f - releaseAt);
            weight = 1f - Mathf.SmoothStep(0.3f, 1f, r);
            draw = 0f;
        }

        if (chest != null) chest.rotation = Quaternion.AngleAxis(stanceTwist * shot.Twist * weight, Vector3.up) * chest.rotation;
        // Bow arm straight out ahead, at this shot's height.
        var bowAim = forward + Vector3.up * shot.Lift;
        Aim(upperArmL, lowerArmL.position - upperArmL.position, bowAim, weight);
        Aim(lowerArmL, handL.position - lowerArmL.position, bowAim, weight);

        // Bow in the left hand, standing upright across the line of fire.
        bow.SetPositionAndRotation(handL.position, Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, shot.Cant));

        // String hand: on the string at the bow, pulled back toward the chest by `draw`;
        // after release it flicks back and out a little.
        var nockRest = handL.position - forward * bowDepth * 0.2f;
        var released = t >= releaseAt ? Mathf.Clamp01(1f - (t - releaseAt) / (1f - releaseAt)) : 0f;
        var pull = drawLength * shot.Draw;
        var target = nockRest - forward * pull * Mathf.Max(draw, released * shot.Flick) + right * 0.25f * shot.Flick * released;
        Aim(upperArm, lowerArm.position - upperArm.position, target - upperArm.position, weight);
        Aim(lowerArm, hand.position - lowerArm.position, target - lowerArm.position, weight);

        // The string follows the hand while drawn, then snaps straight.
        var nock = t < releaseAt ? Vector3.Lerp(nockRest, hand.position, draw) : nockRest;
        var top = upperLimb.position + upperLimb.up * upperLimb.lossyScale.y * 0.5f;
        var bottom = lowerLimb.position - lowerLimb.up * lowerLimb.lossyScale.y * 0.5f;
        PlaceString(stringTop, top, nock);
        PlaceString(stringBottom, bottom, nock);
    }

    private static void PlaceString(Transform segment, Vector3 from, Vector3 to)
    {
        var span = to - from;
        if (span.sqrMagnitude < 0.0001f) return;
        segment.SetPositionAndRotation((from + to) * 0.5f, Quaternion.LookRotation(span));
        segment.localScale = new Vector3(0.025f, 0.025f, span.magnitude); // bow has world scale 1
    }

    private static void Aim(Transform bone, Vector3 current, Vector3 target, float weight)
    {
        if (bone == null || current.sqrMagnitude < 0.0001f || target.sqrMagnitude < 0.0001f) return;
        var turn = Quaternion.FromToRotation(current, target);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * bone.rotation;
    }

    private bool FindBones()
    {
        if (bow != null) return true;
        var holder = transform.Find("Hero_Model");
        if (holder == null) return false;
        foreach (var child in holder.GetComponentsInChildren<Transform>(true))
        {
            switch (child.name)
            {
                case "upperarm.r": upperArm = child; break;
                case "lowerarm.r": lowerArm = child; break;
                case "hand.r": hand = child; break;
                case "chest": chest = child; break;
                case "upperarm.l": upperArmL = child; break;
                case "lowerarm.l": lowerArmL = child; break;
                case "hand.l": handL = child; break;
            }
        }
        if (upperArm == null || lowerArm == null || hand == null || upperArmL == null || lowerArmL == null || handL == null) return false;
        BuildBow();
        return true;
    }

    /// <summary>A simple recurve from cubes: grip, two limbs bent back toward the archer, and a string.</summary>
    private void BuildBow()
    {
        bow = new GameObject("Hero_Bow").transform;
        bow.SetParent(transform, false);
        // Sizes are in world units even if the player root is scaled.
        var rootScale = transform.lossyScale.x;
        if (rootScale > 0.0001f) bow.localScale = Vector3.one / rootScale;
        var wood = PrototypeBootstrap.CreateMaterial("Hero Bow Wood", new Color(0.45f, 0.27f, 0.12f));
        var cord = PrototypeBootstrap.CreateMaterial("Hero Bow String", new Color(0.9f, 0.88f, 0.8f));
        var limb = bowHeight * 0.5f;
        grip = Part("Bow_Grip", wood, new Vector3(0.09f, bowHeight * 0.18f, 0.09f), Vector3.zero, 0f);
        // Local space: +Z is the firing direction; the limbs bend back (-Z) toward the tips.
        var bend = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(bowDepth / limb, 0f, 0.9f));
        upperLimb = Part("Bow_Limb_Upper", wood, new Vector3(0.06f, limb, 0.06f), new Vector3(0f, limb * 0.48f, -bowDepth * 0.5f), -bend);
        lowerLimb = Part("Bow_Limb_Lower", wood, new Vector3(0.06f, limb, 0.06f), new Vector3(0f, -limb * 0.48f, -bowDepth * 0.5f), bend);
        stringTop = Part("Bow_String_Top", cord, Vector3.one * 0.025f, Vector3.zero, 0f);
        stringBottom = Part("Bow_String_Bottom", cord, Vector3.one * 0.025f, Vector3.zero, 0f);
        bow.gameObject.SetActive(false);
    }

    private Transform Part(string partName, Material material, Vector3 scale, Vector3 position, float tiltDegrees)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
        part.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        part.transform.SetParent(bow, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
        part.transform.localScale = scale;
        return part.transform;
    }
}
