using UnityEngine;

/// <summary>
/// Presentation only: shows the KayKit Ranger in place of the player capsule and plays
/// Rig_Medium clips (idle, run, death) through a Legacy Animation component. The model turns
/// to the ranger's facing direction. Falls back to the capsule if the pack is missing.
/// </summary>
[RequireComponent(typeof(PrototypePlayerMovement))]
public sealed class PrototypeHeroModel : MonoBehaviour
{
    private const string CharacterPath = "KayKit/Characters/Ranger";
    private const string TexturePath = "KayKit/Characters/ranger_texture";
    private static readonly string[] ClipSources = { "KayKit/Characters/Rig_Medium_MovementBasic", "KayKit/Characters/Rig_Medium_General" };

    [SerializeField] private float modelHeight = 2.3f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private string idleClip = "Idle_A";
    [SerializeField] private string moveClip = "Running_A";
    [SerializeField] private string deathClip = "Death_A";

    private PrototypePlayerMovement movement;
    private PrototypePlayerStats stats;
    private Transform model;
    private Animation animator;
    private string playing;

    private void Start()
    {
        movement = GetComponent<PrototypePlayerMovement>();
        stats = GetComponent<PrototypePlayerStats>();
        var prefab = PrototypeKayKit.Prefab(CharacterPath);
        if (prefab == null) return;

        var holder = new GameObject("Hero_Model").transform;
        holder.SetParent(transform, false);
        var instance = PrototypeKayKit.Spawn(prefab, holder, PrototypeKayKit.TextureMaterial(TexturePath, Color.white));
        var bounds = PrototypeKayKit.LocalBounds(CharacterPath, prefab);
        var scale = bounds.size.y > 0.001f ? modelHeight / bounds.size.y : 1f;
        instance.transform.localScale = Vector3.one * scale;
        // The controller is centered on the player; its feet are half its height below.
        var controller = GetComponent<CharacterController>();
        var feet = controller != null ? controller.center.y - controller.height * 0.5f : -1f;
        instance.transform.localPosition = new Vector3(0f, feet - bounds.min.y * scale, 0f);
        model = holder;

        var capsule = GetComponent<MeshRenderer>();
        if (capsule != null) capsule.enabled = false;

        animator = instance.GetComponent<Animation>();
        if (animator == null) animator = instance.AddComponent<Animation>();
        foreach (var source in ClipSources)
            foreach (var clip in Resources.LoadAll<AnimationClip>(source))
                if (!clip.name.StartsWith("__preview__") && animator.GetClip(clip.name) == null)
                {
                    clip.legacy = true;
                    animator.AddClip(clip, clip.name);
                }
        Play(idleClip, WrapMode.Loop);
    }

    private void Update()
    {
        if (model == null) return;
        if (stats != null && stats.IsDead)
        {
            Play(deathClip, WrapMode.ClampForever);
            return;
        }

        var facing = movement.FacingDirection;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
            model.localRotation = Quaternion.RotateTowards(model.localRotation, Quaternion.LookRotation(facing), turnSpeed * Time.deltaTime);
        Play(movement.IsMoving ? moveClip : idleClip, WrapMode.Loop);
    }

    private void Play(string clipName, WrapMode wrap)
    {
        if (animator == null || playing == clipName || animator.GetClip(clipName) == null) return;
        playing = clipName;
        animator[clipName].wrapMode = wrap;
        animator.CrossFade(clipName, 0.15f);
    }
}
