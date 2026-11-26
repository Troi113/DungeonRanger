using UnityEngine;

/// <summary>A flying caustic glob: hits the ranger (slow + Oozed) or splats on walls.</summary>
public sealed class PrototypeCausticGlob : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float effectSeconds;
    private float life = 4f;
    private PrototypePlayerMovement player;

    public void Launch(Vector3 flightDirection, float flightSpeed, float effectDuration)
    {
        direction = flightDirection;
        speed = flightSpeed;
        effectSeconds = effectDuration;
        player = FindAnyObjectByType<PrototypePlayerMovement>();
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        var step = speed * Time.deltaTime;
        life -= Time.deltaTime;
        foreach (var hit in Physics.SphereCastAll(transform.position, 0.4f, direction, step, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var name = hit.collider.name;
            if (!name.StartsWith("Wall_") && name != "Door_Panel" && !name.StartsWith("Frame_")) continue;
            Splat();
            return;
        }
        transform.position += direction * step;

        if (player != null)
        {
            var offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 1.1f * 1.1f)
            {
                var statuses = player.GetComponent<PrototypeStatusEffects>();
                statuses?.Apply(PrototypeStatusEffectType.Oozed, effectSeconds);
                statuses?.Apply(PrototypeStatusEffectType.Slowed, effectSeconds);
                PrototypeGameUI.ShowNotice("Caustic ooze burns and slows you!");
                PrototypeGameFeel.Shake(0.2f, 0.15f);
                Splat();
                return;
            }
        }
        if (life <= 0f) Splat();
    }

    private void Splat()
    {
        PrototypeGameFeel.Sparks(transform.position, new Color(0.55f, 0.95f, 0.2f), 16);
        Destroy(gameObject);
    }
}
