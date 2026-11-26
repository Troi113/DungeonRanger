using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Goo leaves slime puddles behind as it moves. A ranger standing in slime moves slower and
/// cannot tumble (PrototypePlayerMovement asks IsSlimed). Puddles shrink away after a while.
/// </summary>
public sealed class PrototypeSlimeTrail : MonoBehaviour
{
    [SerializeField] private float spacing = 2.5f;
    [SerializeField] private float puddleRadius = 2.6f;
    [SerializeField] private float lifetime = 9f;
    [SerializeField] private float shrinkSeconds = 1.5f;

    private sealed class Puddle
    {
        public Transform Transform;
        public Vector3 Position;
        public float Age;
        public float Radius;
    }

    // Puddles on the active floor; a short list, checked once per frame by the ranger.
    private static readonly List<Puddle> Active = new();

    private readonly List<Puddle> mine = new();
    private Vector3 lastDrop;
    private Material slime;
    private PrototypeEnemy enemy;

    /// <summary>True when the position is inside any slime puddle.</summary>
    public static bool IsSlimed(Vector3 position)
    {
        foreach (var puddle in Active)
        {
            var offset = position - puddle.Position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= puddle.Radius * puddle.Radius) return true;
        }
        return false;
    }

    private void Start()
    {
        enemy = GetComponent<PrototypeEnemy>();
        slime = PrototypeBootstrap.CreateMaterial("Goo Slime", new Color(0.35f, 0.8f, 0.3f));
        lastDrop = transform.position;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        for (var i = mine.Count - 1; i >= 0; i--)
        {
            var puddle = mine[i];
            puddle.Age += Time.deltaTime;
            var remaining = lifetime - puddle.Age;
            if (remaining <= 0f) { Remove(puddle); continue; }
            var shrink = Mathf.Clamp01(remaining / shrinkSeconds);
            puddle.Radius = puddleRadius * shrink;
            puddle.Transform.localScale = new Vector3(puddleRadius * 2f * shrink, 0.03f, puddleRadius * 2f * shrink);
        }

        if (enemy == null || !enemy.IsAlive) return;
        var moved = transform.position - lastDrop;
        moved.y = 0f;
        if (moved.magnitude < spacing) return;
        lastDrop = transform.position;
        Drop(new Vector3(transform.position.x, 0.04f, transform.position.z));
    }

    private void Drop(Vector3 position)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Goo_Slime";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(transform.parent, true);
        disc.transform.position = position;
        disc.transform.localScale = new Vector3(puddleRadius * 2f, 0.03f, puddleRadius * 2f);
        disc.GetComponent<Renderer>().sharedMaterial = slime;
        var puddle = new Puddle { Transform = disc.transform, Position = position, Radius = puddleRadius };
        mine.Add(puddle);
        Active.Add(puddle);
    }

    private void Remove(Puddle puddle)
    {
        mine.Remove(puddle);
        Active.Remove(puddle);
        if (puddle.Transform != null) Destroy(puddle.Transform.gameObject);
    }

    private void OnDisable()
    {
        // Leaving the floor (or Goo dying with its floor) clears this Goo's slime.
        foreach (var puddle in mine)
        {
            Active.Remove(puddle);
            if (puddle.Transform != null) Destroy(puddle.Transform.gameObject);
        }
        mine.Clear();
    }
}
