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

    private CharacterController characterController;
    private float verticalVelocity;
    private Vector3 tumbleDirection;
    private float tumbleElapsed;
    private float nextTumbleTime;
    private Quaternion tumbleStartRotation;
    private Vector3 tumbleRotationAxis;

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

        if (!IsTumbling && Time.time >= nextTumbleTime && IsShiftPressedThisFrame())
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
        var movement = new Vector3(input.x, 0f, input.y) * moveSpeed;

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

    private static bool IsShiftPressedThisFrame()
    {
        return Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
    }
}
