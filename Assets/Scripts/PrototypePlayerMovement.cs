using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal real-time WASD movement for the first playable test.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PrototypePlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float tumbleDistance = 4f;
    [SerializeField] private float tumbleDuration = 0.2f;
    [SerializeField] private float tumbleCooldown = 0.65f;
    [SerializeField, Range(0f, 1f)] private float slimeSpeedMultiplier = 0.45f;

    private CharacterController characterController;
    private float verticalVelocity;
    private Vector3 tumbleDirection;
    private float tumbleElapsed;
    private float nextTumbleTime;
    private Quaternion tumbleStartRotation;
    private Vector3 tumbleRotationAxis;
    private Vector3 knockbackVelocity;
    private float knockbackRemaining;

    public Vector3 FacingDirection { get; private set; } = Vector3.forward;
    public bool IsTumbling { get; private set; }
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            IsMoving = false;
            return;
        }

        if (PrototypeGameUI.IsGamePaused)
        {
            IsMoving = false;
            return;
        }

        var stats = GetComponent<PrototypePlayerStats>();
        if (stats != null && stats.IsDead)
        {
            IsMoving = false;
            return;
        }

        if (knockbackRemaining > 0f)
        {
            // Knockback overrides input; the controller still collides with walls.
            knockbackRemaining -= Time.deltaTime;
            IsMoving = false;
            characterController.Move(knockbackVelocity * Time.deltaTime);
            return;
        }

        // Goo's slime slows the ranger and prevents tumbling.
        var slimed = PrototypeSlimeTrail.IsSlimed(transform.position);
        if (!slimed && !IsTumbling && Time.time >= nextTumbleTime && IsShiftPressedThisFrame())
        {
            BeginTumble();
        }

        if (IsTumbling)
        {
            IsMoving = true;
            UpdateTumble();
            return;
        }

        var input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;

        input = Vector2.ClampMagnitude(input, 1f);
        var statusEffects = GetComponent<PrototypeStatusEffects>();
        if (statusEffects != null && statusEffects.IsMovementBlocked)
        {
            input = Vector2.zero;
        }

        IsMoving = input.sqrMagnitude > 0.01f;
        var movement = new Vector3(input.x, 0f, input.y) * moveSpeed * (slimed ? slimeSpeedMultiplier : 1f) * ArmorSpeedMultiplier();

        if (input.sqrMagnitude > 0.01f)
        {
            FacingDirection = new Vector3(input.x, 0f, input.y).normalized;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
    }

    /// <summary>Pushes the ranger a set distance over a short time, interrupting a tumble.</summary>
    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f || distance <= 0f) return;
        duration = Mathf.Max(0.05f, duration);
        knockbackVelocity = direction.normalized * (distance / duration);
        knockbackRemaining = duration;
        if (IsTumbling)
        {
            IsTumbling = false;
            transform.rotation = tumbleStartRotation;
        }
    }

    private void BeginTumble()
    {
        nextTumbleTime = Time.time + tumbleCooldown;
        tumbleDirection = FacingDirection.sqrMagnitude > 0.01f
            ? FacingDirection.normalized
            : Vector3.forward;
        tumbleElapsed = 0f;
        tumbleStartRotation = transform.rotation;
        tumbleRotationAxis = Vector3.Cross(Vector3.up, tumbleDirection).normalized;
        IsTumbling = true;
    }

    private void UpdateTumble()
    {
        tumbleElapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(tumbleElapsed / Mathf.Max(0.01f, tumbleDuration));
        var tumbleSpeed = tumbleDistance / Mathf.Max(0.01f, tumbleDuration);
        characterController.Move(tumbleDirection * tumbleSpeed * Time.deltaTime);

        if (tumbleRotationAxis.sqrMagnitude > 0.01f)
        {
            transform.rotation = tumbleStartRotation * Quaternion.AngleAxis(progress * 360f, tumbleRotationAxis);
        }

        if (progress >= 1f)
        {
            IsTumbling = false;
            transform.rotation = tumbleStartRotation;
        }
    }

    private PrototypeInventory inventory;
    private PrototypePlayerStats playerStats;

    /// <summary>Armor the ranger is too weak for slows them down (Pixel Dungeon Strength rule).</summary>
    private float ArmorSpeedMultiplier()
    {
        if (inventory == null) inventory = GetComponent<PrototypeInventory>();
        if (playerStats == null) playerStats = GetComponent<PrototypePlayerStats>();
        if (inventory == null || playerStats == null) return 1f;
        return PrototypeStrength.ArmorSpeedMultiplier(PrototypeStrength.Shortfall(inventory.GetSlot(1), playerStats.Strength));
    }

    private static bool IsShiftPressedThisFrame()
    {
        return Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
    }
}
