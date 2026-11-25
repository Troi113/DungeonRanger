using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal real-time WASD movement for the first playable test.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PrototypePlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        var input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;

        input = Vector2.ClampMagnitude(input, 1f);
        var movement = new Vector3(input.x, 0f, input.y) * moveSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
    }
}
