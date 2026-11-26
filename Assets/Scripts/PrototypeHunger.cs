using UnityEngine;

public enum PrototypeHungerState
{
    Satiated,
    Hungry,
    Famished
}

/// <summary>
/// Hunger advances only while the ranger is moving. Famished hunger damages
/// the player repeatedly until health reaches zero.
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeHunger : MonoBehaviour
{
    [SerializeField] private float movingSecondsToHungry = 60f;
    [SerializeField] private float movingSecondsToFamished = 120f;
    [SerializeField] private float famishedDamageInterval = 5f;

    private PrototypePlayerStats stats;
    private PrototypePlayerMovement movement;
    private float movingTime;
    private float nextFamishedDamageTime;

    public PrototypeHungerState State { get; private set; } = PrototypeHungerState.Satiated;
    public float MovingTime => movingTime;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
    }

    private void Update()
    {
        if (movement == null)
        {
            movement = GetComponent<PrototypePlayerMovement>();
        }

        if (stats.IsDead)
        {
            return;
        }

        if (movement != null && movement.IsMoving)
        {
            movingTime += Time.deltaTime;
        }

        if (movingTime >= movingSecondsToFamished)
        {
            State = PrototypeHungerState.Famished;
            if (Time.time >= nextFamishedDamageTime)
            {
                nextFamishedDamageTime = Time.time + famishedDamageInterval;
                stats.TakeDamage(1);
            }
        }
        else if (movingTime >= movingSecondsToHungry)
        {
            State = PrototypeHungerState.Hungry;
        }
        else
        {
            State = PrototypeHungerState.Satiated;
        }
    }

    public void RestoreMovingTime(float value) => movingTime = Mathf.Max(0f, value);

    public void Eat(float movingSecondsRestored)
    {
        movingTime = Mathf.Max(0f, movingTime - Mathf.Max(0f, movingSecondsRestored));
        nextFamishedDamageTime = Time.time + famishedDamageInterval;
    }
}
